using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.Payments.Requests;
using ApisConsulta.Application.Payments.Response;
using ApisConsulta.Infrastructure.Mapping;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

/// <summary>
/// Registers payments in BambooERP (<c>Payments</c>), the same table the ERP writes when
/// a voucher is uploaded.
///
/// Two triggers on the table do part of the work, so the insert must not duplicate it:
/// <c>trg_GenerarFolioPayments</c> generates the folio (<c>PAY-MMYY-NNNNNN</c>) right
/// after the insert, and <c>trg_AfterInsert_Payments_InsertRelation</c> relates the
/// payment to the sale in <c>rel_quotes_to_payments</c> — and moves that sale to status
/// 29 — whenever <c>RelatedTo = 'QUOTATION'</c> and <c>RelatedId &gt; 0</c>.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;
    public PaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<PaymentReferences> ResolveReferencesAsync(CreatePaymentRequest request)
    {
        var customerCode = request.CustomerCode;
        var bankId = request.BankId;
        var paymentFormId = request.PaymentFormId;
        var uploadedById = request.UploadedById;
        var sellerId = request.SellerId;
        var departmentId = request.DepartmentId;
        var statusId = request.StatusId;
        var saleFolio = string.IsNullOrWhiteSpace(request.SaleFolio) ? null : request.SaleFolio.Trim();

        var row = await _context.Database
            .SqlQuery<PaymentReferencesRow>($@"
                SELECT
                    (SELECT TOP 1 c.customer_id FROM customers c
                      WHERE c.customer_code = {customerCode})            AS CustomerId,
                    (SELECT TOP 1 b.id_origen FROM bancos b
                      WHERE b.id = {bankId})                             AS AccountId,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM bancos b
                      WHERE b.id = {bankId}) THEN 1 ELSE 0 END AS BIT)   AS BankExists,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM bancos b
                      WHERE b.id = {bankId} AND b.isDisabled = 1)
                      THEN 1 ELSE 0 END AS BIT)                          AS BankDisabled,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM sat_FormaPago f
                      WHERE f.ID = {paymentFormId}) THEN 1 ELSE 0 END AS BIT) AS PaymentFormExists,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM catUsers u
                      WHERE u.idUsuario = {uploadedById}) THEN 1 ELSE 0 END AS BIT) AS UploaderExists,
                    (SELECT TOP 1 u.BranchID FROM catUsers u
                      WHERE u.idUsuario = {uploadedById})                AS UploaderDepartmentId,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM catUsers u
                      WHERE u.idUsuario = {sellerId}) THEN 1 ELSE 0 END AS BIT) AS SellerExists,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM departments d
                      WHERE d.id = {departmentId}) THEN 1 ELSE 0 END AS BIT) AS DepartmentExists,
                    CAST(CASE WHEN EXISTS (SELECT 1 FROM catEstatus e
                      WHERE e.idEstatus = {statusId}) THEN 1 ELSE 0 END AS BIT) AS StatusExists,
                    (SELECT TOP 1 q.id FROM quotation q
                      WHERE q.billCode = {saleFolio}
                        AND (q.is_hide = 0 OR q.is_hide IS NULL))        AS SaleId")
            .FirstAsync();

        return new PaymentReferences
        {
            CustomerId = row.CustomerId,
            AccountId = row.AccountId,
            BankExists = row.BankExists,
            BankDisabled = row.BankDisabled,
            PaymentFormExists = row.PaymentFormExists,
            UploaderExists = row.UploaderExists,
            UploaderDepartmentId = row.UploaderDepartmentId,
            SellerExists = row.SellerExists,
            DepartmentExists = row.DepartmentExists,
            StatusExists = row.StatusExists,
            SaleId = row.SaleId
        };
    }

    public async Task<PaymentResponse> CreateAsync(CreatePaymentData data)
    {
        // Folio is left out on purpose: trg_GenerarFolioPayments assigns it after the
        // insert. RelatedTo/RelatedId drive trg_AfterInsert_Payments_InsertRelation, which
        // is what creates the row in rel_quotes_to_payments.
        var relatedTo = data.SaleId > 0 ? "QUOTATION" : null;

        // OUTPUT ... INTO a table variable: the table has triggers and SQL Server does not
        // allow a bare OUTPUT clause on a table with triggers (Error 334).
        var ids = await _context.Database
            .SqlQuery<int>($@"
                SET NOCOUNT ON;
                DECLARE @IdsInsertados TABLE (Value INT);
                INSERT INTO Payments
                    (CustomerId, PaymentDate, AccountId, BankId, PaymentFormId, StatusId,
                     Amount, PaymentType, Reference, Comentary, Observations,
                     PaymentFilePath, RelatedId, RelatedTo, UploadedById, SellerId,
                     DepartmentId, CreatedAt)
                OUTPUT INSERTED.Id INTO @IdsInsertados(Value)
                VALUES ({data.CustomerId}, {data.PaymentDate}, {data.AccountId}, {data.BankId},
                        {data.PaymentFormId}, {data.StatusId}, {data.Amount}, {data.PaymentType},
                        {data.Reference}, {data.Comentary}, {data.Observations},
                        {data.PaymentFilePath}, {data.SaleId}, {relatedTo}, {data.UploadedById},
                        {data.SellerId}, {data.DepartmentId}, GETDATE());
                SELECT Value FROM @IdsInsertados;")
            .ToListAsync();

        var paymentId = ids.First();

        // Re-read so the response carries the folio the trigger generated.
        return (await GetByIdAsync(paymentId))!;
    }

    public async Task<PaymentResponse?> GetByIdAsync(int paymentId)
    {
        var row = await _context.Database
            .SqlQuery<PaymentRow>($@"
                SELECT TOP 1
                    p.Id                            AS PaymentId,
                    p.Folio                         AS Folio,
                    c.customer_code                 AS CustomerCode,
                    c.name                          AS Customer,
                    p.PaymentDate                   AS PaymentDate,
                    p.Amount                        AS Amount,
                    p.PaymentFormId                 AS PaymentFormId,
                    f.vchCode                       AS PaymentFormCode,
                    ISNULL(f.UILabel, f.vchFPago)   AS PaymentForm,
                    p.AccountId                     AS AccountId,
                    o.name                          AS Account,
                    p.BankId                        AS BankId,
                    b.banco                         AS Bank,
                    b.numero_cuenta                 AS BankAccountNumber,
                    p.StatusId                      AS StatusId,
                    e.vchNombre                     AS StatusRaw,
                    p.Reference                     AS Reference,
                    p.PaymentType                   AS PaymentType,
                    NULLIF(p.RelatedId, 0)          AS SaleId,
                    q.billCode                      AS SaleFolio,
                    p.DepartmentId                  AS DepartmentId,
                    d.name                          AS Department,
                    p.UploadedById                  AS UploadedById,
                    p.SellerId                      AS SellerId,
                    p.PaymentFilePath               AS PaymentFilePath,
                    p.Comentary                     AS Comentary,
                    p.Observations                  AS Observations,
                    p.CreatedAt                     AS CreatedAt
                FROM Payments p
                LEFT JOIN customers c ON c.customer_id = p.CustomerId
                LEFT JOIN sat_FormaPago f ON f.ID = p.PaymentFormId
                LEFT JOIN bancos b ON b.id = p.BankId
                LEFT JOIN origen_cuenta o ON o.id = p.AccountId
                LEFT JOIN catEstatus e ON e.idEstatus = p.StatusId
                LEFT JOIN departments d ON d.id = p.DepartmentId
                LEFT JOIN quotation q ON q.id = p.RelatedId AND p.RelatedTo = 'QUOTATION'
                WHERE p.Id = {paymentId}")
            .FirstOrDefaultAsync();

        if (row == null)
            return null;

        return new PaymentResponse
        {
            PaymentId = row.PaymentId,
            Folio = row.Folio,
            CustomerCode = row.CustomerCode,
            Customer = row.Customer,
            PaymentDate = row.PaymentDate,
            Amount = row.Amount,
            PaymentFormId = row.PaymentFormId,
            PaymentFormCode = row.PaymentFormCode?.Trim(),
            // vchFPago carries a trailing line break in the catalog.
            PaymentForm = row.PaymentForm?.Trim(),
            AccountId = row.AccountId,
            Account = row.Account,
            BankId = row.BankId,
            Bank = row.Bank?.Trim(),
            BankAccountNumber = row.BankAccountNumber?.Trim(),
            StatusId = row.StatusId,
            StatusRaw = row.StatusRaw,
            Status = PaymentStatusMapper.Map(row.StatusRaw),
            Reference = row.Reference?.Trim(),
            PaymentType = row.PaymentType,
            SaleId = row.SaleId,
            SaleFolio = row.SaleFolio,
            DepartmentId = row.DepartmentId,
            Department = row.Department,
            UploadedById = row.UploadedById,
            SellerId = row.SellerId,
            PaymentFilePath = row.PaymentFilePath,
            Comentary = row.Comentary,
            Observations = row.Observations,
            CreatedAt = row.CreatedAt
        };
    }

    private sealed class PaymentReferencesRow
    {
        public int? CustomerId { get; set; }
        public int? AccountId { get; set; }
        public bool BankExists { get; set; }
        public bool BankDisabled { get; set; }
        public bool PaymentFormExists { get; set; }
        public bool UploaderExists { get; set; }
        public int? UploaderDepartmentId { get; set; }
        public bool SellerExists { get; set; }
        public bool DepartmentExists { get; set; }
        public bool StatusExists { get; set; }
        public int? SaleId { get; set; }
    }

    private sealed class PaymentRow
    {
        public int PaymentId { get; set; }
        public string? Folio { get; set; }
        public string? CustomerCode { get; set; }
        public string? Customer { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal? Amount { get; set; }
        public int PaymentFormId { get; set; }
        public string? PaymentFormCode { get; set; }
        public string? PaymentForm { get; set; }
        public int AccountId { get; set; }
        public string? Account { get; set; }
        public int BankId { get; set; }
        public string? Bank { get; set; }
        public string? BankAccountNumber { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public string? Reference { get; set; }
        public string? PaymentType { get; set; }
        public int? SaleId { get; set; }
        public string? SaleFolio { get; set; }
        public int DepartmentId { get; set; }
        public string? Department { get; set; }
        public int UploadedById { get; set; }
        public int? SellerId { get; set; }
        public string? PaymentFilePath { get; set; }
        public string? Comentary { get; set; }
        public string? Observations { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
