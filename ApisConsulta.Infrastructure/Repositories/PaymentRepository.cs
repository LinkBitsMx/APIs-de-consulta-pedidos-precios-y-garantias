using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.Payments.Queries;
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
                     DepartmentId, CreatedAt,
                     KingdeeBillNo, BizOrgId, BizOrgCode, SettleOrgId, SettleOrgCode,
                     CashierId, CashierCode, KingdeeAccountId, KingdeeAccountCode,
                     ReceiveTypeId, ReceiveTypeCode, SettleCurrencyId, SettleCurrencyCode,
                     ReceiveCurrencyId, ReceiveCurrencyCode, ExchangeRate,
                     CardId, CardNumber, MemberId, MemberCardNumber, RechargeAmount)
                OUTPUT INSERTED.Id INTO @IdsInsertados(Value)
                VALUES ({data.CustomerId}, {data.PaymentDate}, {data.AccountId}, {data.BankId},
                        {data.PaymentFormId}, {data.StatusId}, {data.Amount}, {data.PaymentType},
                        {data.Reference}, {data.Comentary}, {data.Observations},
                        {data.PaymentFilePath}, {data.SaleId}, {relatedTo}, {data.UploadedById},
                        {data.SellerId}, {data.DepartmentId}, GETDATE(),
                        {data.KingdeeBillNo}, {data.BizOrgId}, {data.BizOrgCode},
                        {data.SettleOrgId}, {data.SettleOrgCode},
                        {data.CashierId}, {data.CashierCode},
                        {data.KingdeeAccountId}, {data.KingdeeAccountCode},
                        {data.ReceiveTypeId}, {data.ReceiveTypeCode},
                        {data.SettleCurrencyId}, {data.SettleCurrencyCode},
                        {data.ReceiveCurrencyId}, {data.ReceiveCurrencyCode}, {data.ExchangeRate},
                        {data.CardId}, {data.CardNumber},
                        {data.MemberId}, {data.MemberCardNumber}, {data.RechargeAmount});
                SELECT Value FROM @IdsInsertados;")
            .ToListAsync();

        var paymentId = ids.First();

        // Re-read so the response carries the folio the trigger generated.
        return (await GetByIdAsync(paymentId))!;
    }

    public async Task<PaymentResponse?> GetByIdAsync(int paymentId)
    {
        var rows = await QueryPaymentsAsync(new PaymentsFilter { PageSize = 1 }, paymentId);

        if (rows.Count == 0)
            return null;

        var payment = MapPayment(rows[0]);
        var sales = await GetKingdeeSalesAsync([payment.PaymentId]);

        if (sales.TryGetValue(payment.PaymentId, out var own))
            payment.KingdeeSales = own;

        return payment;
    }

    public async Task<PagedPaymentsResponse> GetPaymentsAsync(PaymentsFilter filter)
    {
        // The breakdown by status is what carries the total: it already counts every
        // matching payment, so no separate COUNT query is needed.
        var summary = await GetStatusSummaryAsync(filter);
        var total = summary.Sum(s => s.Count);

        if (total == 0)
        {
            return new PagedPaymentsResponse
            {
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        var rows = await QueryPaymentsAsync(filter, paymentId: null);
        var payments = rows.Select(MapPayment).ToList();

        // One query for the whole page: the applications of every payment at once.
        var sales = await GetKingdeeSalesAsync(payments.Select(p => p.PaymentId));

        foreach (var payment in payments)
        {
            if (sales.TryGetValue(payment.PaymentId, out var own))
                payment.KingdeeSales = own;
        }

        return new PagedPaymentsResponse
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize),
            TotalAmount = summary.Sum(s => s.Amount),
            Summary = summary,
            Payments = payments
        };
    }

    /// <summary>
    /// Sales each payment was applied to (<c>PaymentApplications</c>), with the folio of
    /// the document generated in Kingdee. The sale lives in one of two tables, picked by
    /// <c>isPOS</c>: <c>kingdee_sales_invoices</c> for an invoice and
    /// <c>KingDeeSalesPOS</c> for a point-of-sale ticket, both keyed by <c>SaleId</c>.
    ///
    /// Applications with <c>SaleId = 0</c> are kept: the payment is already applied but
    /// the sale has not been generated in Kingdee yet, so they come back with a null
    /// folio instead of disappearing.
    /// </summary>
    private async Task<Dictionary<int, List<PaymentSaleResponse>>> GetKingdeeSalesAsync(
        IEnumerable<int> paymentIds)
    {
        var ids = string.Join(",", paymentIds.Distinct());
        if (ids.Length == 0)
            return [];

        var rows = await _context.Database
            .SqlQuery<PaymentSaleRow>($@"
                SELECT
                    pa.PaymentId                AS PaymentId,
                    pa.SaleId                   AS SaleId,
                    pa.isPOS                    AS IsPos,
                    CASE WHEN pa.isPOS = 1 THEN pos.Folio ELSE inv.bill_code END AS Folio,
                    ISNULL(pa.AmountApplied, 0) AS AmountApplied,
                    pa.AppliedDate              AS AppliedDate
                FROM PaymentApplications pa
                LEFT JOIN kingdee_sales_invoices inv ON pa.isPOS = 0 AND inv.id = pa.SaleId
                LEFT JOIN KingDeeSalesPOS pos ON pa.isPOS = 1 AND pos.Id = pa.SaleId
                JOIN STRING_SPLIT({ids}, ',') sp ON sp.value = pa.PaymentId
                ORDER BY pa.PaymentId, pa.Id")
            .ToListAsync();

        return rows
            .GroupBy(r => r.PaymentId)
            .ToDictionary(g => g.Key, g => g.Select(r => new PaymentSaleResponse
            {
                SaleId = r.SaleId,
                IsPos = r.IsPos,
                Folio = string.IsNullOrWhiteSpace(r.Folio) ? null : r.Folio.Trim(),
                AmountApplied = r.AmountApplied,
                AppliedDate = r.AppliedDate
            }).ToList());
    }

    /// <summary>
    /// How many payments — and how much — the filter matches in each status. It covers the
    /// whole filtered set, not the page.
    /// </summary>
    private async Task<List<PaymentStatusSummaryResponse>> GetStatusSummaryAsync(PaymentsFilter filter)
    {
        var statusNames = InternalStatusNames(filter);
        var statusId = filter.StatusId;
        var customerCode = filter.CustomerCode;
        var folio = filter.Folio;
        var saleFolio = filter.SaleFolio;
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;
        var saleStartDate = filter.SaleStartDate;
        var saleEndDate = filter.SaleEndDate;
        var kingdeeSaleFolio = filter.KingdeeSaleFolio;
        var kingdeeStartDate = filter.KingdeeSaleStartDate;
        var kingdeeEndDate = filter.KingdeeSaleEndDate;
        var branchCode = filter.BranchCode;
        var paymentFormId = filter.PaymentFormId;
        var bankId = filter.BankId;
        var departmentId = filter.DepartmentId;
        var sellerId = filter.SellerId;
        var paymentType = filter.PaymentType;

        var rows = await _context.Database
            .SqlQuery<PaymentStatusSummaryRow>($@"
                SELECT
                    ISNULL(p.StatusId, 0)   AS StatusId,
                    e.vchNombre             AS StatusRaw,
                    COUNT(1)                AS [Count],
                    ISNULL(SUM(p.Amount), 0) AS Amount
                FROM Payments p
                LEFT JOIN customers c ON c.customer_id = p.CustomerId
                LEFT JOIN catEstatus e ON e.idEstatus = p.StatusId
                LEFT JOIN departments d ON d.id = p.DepartmentId
                LEFT JOIN starnet_branches sb ON sb.id = d.branchId
                LEFT JOIN quotation q ON q.id = p.RelatedId AND p.RelatedTo = 'QUOTATION'
                WHERE ({statusNames} IS NULL OR LOWER(e.vchNombre) IN (
                        SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT({statusNames}, ',')))
                  AND ({statusId} IS NULL OR p.StatusId = {statusId})
                  AND ({customerCode} IS NULL OR c.customer_code = {customerCode})
                  AND ({folio} IS NULL OR p.Folio LIKE '%' + {folio} + '%')
                  AND ({saleFolio} IS NULL OR q.billCode = {saleFolio})
                  AND ({branchCode} IS NULL OR sb.code = {branchCode})
                  AND ({startDate} IS NULL OR p.PaymentDate >= {startDate})
                  AND ({endDate} IS NULL OR p.PaymentDate <= {endDate})
                  AND ({saleStartDate} IS NULL OR q.created_at >= {saleStartDate})
                  AND ({saleEndDate} IS NULL OR q.created_at <= {saleEndDate})
                  AND (({kingdeeSaleFolio} IS NULL AND {kingdeeStartDate} IS NULL
                            AND {kingdeeEndDate} IS NULL) OR EXISTS (
                        SELECT 1
                        FROM PaymentApplications pa
                        LEFT JOIN kingdee_sales_invoices inv ON pa.isPOS = 0 AND inv.id = pa.SaleId
                        LEFT JOIN KingDeeSalesPOS pos ON pa.isPOS = 1 AND pos.Id = pa.SaleId
                        WHERE pa.PaymentId = p.Id
                          AND ({kingdeeSaleFolio} IS NULL
                                OR CASE WHEN pa.isPOS = 1 THEN pos.Folio ELSE inv.bill_code END
                                   = {kingdeeSaleFolio})
                          AND ({kingdeeStartDate} IS NULL
                                OR COALESCE(inv.bill_date, pos.BillDate) >= {kingdeeStartDate})
                          AND ({kingdeeEndDate} IS NULL
                                OR COALESCE(inv.bill_date, pos.BillDate) <= {kingdeeEndDate})))
                  AND ({paymentFormId} IS NULL OR p.PaymentFormId = {paymentFormId})
                  AND ({bankId} IS NULL OR p.BankId = {bankId})
                  AND ({departmentId} IS NULL OR p.DepartmentId = {departmentId})
                  AND ({sellerId} IS NULL OR p.SellerId = {sellerId})
                  AND ({paymentType} IS NULL OR LOWER(p.PaymentType) = {paymentType})
                GROUP BY ISNULL(p.StatusId, 0), e.vchNombre
                ORDER BY ISNULL(p.StatusId, 0)")
            .ToListAsync();

        return rows
            .Select(r => new PaymentStatusSummaryResponse
            {
                StatusId = r.StatusId,
                StatusRaw = r.StatusRaw,
                Status = PaymentStatusMapper.Map(r.StatusRaw),
                Count = r.Count,
                Amount = r.Amount
            })
            .ToList();
    }

    /// <summary>
    /// The single read of <c>Payments</c>: it serves both the detail (<paramref name="paymentId"/>)
    /// and the paged list, so the payment is published with the same shape either way.
    /// </summary>
    private async Task<List<PaymentRow>> QueryPaymentsAsync(PaymentsFilter filter, int? paymentId)
    {
        var statusNames = InternalStatusNames(filter);
        var statusId = filter.StatusId;
        var customerCode = filter.CustomerCode;
        var folio = filter.Folio;
        var saleFolio = filter.SaleFolio;
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;
        var saleStartDate = filter.SaleStartDate;
        var saleEndDate = filter.SaleEndDate;
        var kingdeeSaleFolio = filter.KingdeeSaleFolio;
        var kingdeeStartDate = filter.KingdeeSaleStartDate;
        var kingdeeEndDate = filter.KingdeeSaleEndDate;
        var branchCode = filter.BranchCode;
        var paymentFormId = filter.PaymentFormId;
        var bankId = filter.BankId;
        var departmentId = filter.DepartmentId;
        var sellerId = filter.SellerId;
        var paymentType = filter.PaymentType;
        var offset = (filter.Page - 1) * filter.PageSize;
        var pageSize = filter.PageSize;

        return await _context.Database
            .SqlQuery<PaymentRow>($@"
                SELECT
                    p.Id                            AS PaymentId,
                    p.Folio                         AS Folio,
                    p.CustomerId                    AS CustomerId,
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
                    p.CreatedAt                     AS CreatedAt,
                    p.KingdeeBillNo                 AS KingdeeBillNo,
                    p.BizOrgId                      AS BizOrgId,
                    p.BizOrgCode                    AS BizOrgCode,
                    p.SettleOrgId                   AS SettleOrgId,
                    p.SettleOrgCode                 AS SettleOrgCode,
                    p.CashierId                     AS CashierId,
                    p.CashierCode                   AS CashierCode,
                    p.KingdeeAccountId              AS KingdeeAccountId,
                    p.KingdeeAccountCode            AS KingdeeAccountCode,
                    p.ReceiveTypeId                 AS ReceiveTypeId,
                    p.ReceiveTypeCode               AS ReceiveTypeCode,
                    p.SettleCurrencyId              AS SettleCurrencyId,
                    p.SettleCurrencyCode            AS SettleCurrencyCode,
                    p.ReceiveCurrencyId             AS ReceiveCurrencyId,
                    p.ReceiveCurrencyCode           AS ReceiveCurrencyCode,
                    p.ExchangeRate                  AS ExchangeRate,
                    p.CardId                        AS CardId,
                    p.CardNumber                    AS CardNumber,
                    p.MemberId                      AS MemberId,
                    p.MemberCardNumber              AS MemberCardNumber,
                    p.RechargeAmount                AS RechargeAmount,
                    -- 充值门店: the Kingdee branch behind the department of the payment.
                    sb.id                           AS KingdeeBranchId,
                    sb.code                         AS KingdeeBranchCode,
                    -- 业务员: 'kingdeeId_kingdeeCode_branchId', split on the way out.
                    su.code_seller                  AS SellerCodeRaw
                FROM Payments p
                LEFT JOIN customers c ON c.customer_id = p.CustomerId
                LEFT JOIN sat_FormaPago f ON f.ID = p.PaymentFormId
                LEFT JOIN bancos b ON b.id = p.BankId
                LEFT JOIN origen_cuenta o ON o.id = p.AccountId
                LEFT JOIN catEstatus e ON e.idEstatus = p.StatusId
                LEFT JOIN departments d ON d.id = p.DepartmentId
                LEFT JOIN starnet_branches sb ON sb.id = d.branchId
                LEFT JOIN catUsers su ON su.idUsuario = p.SellerId
                LEFT JOIN quotation q ON q.id = p.RelatedId AND p.RelatedTo = 'QUOTATION'
                WHERE ({paymentId} IS NULL OR p.Id = {paymentId})
                  AND ({statusNames} IS NULL OR LOWER(e.vchNombre) IN (
                        SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT({statusNames}, ',')))
                  AND ({statusId} IS NULL OR p.StatusId = {statusId})
                  AND ({customerCode} IS NULL OR c.customer_code = {customerCode})
                  AND ({folio} IS NULL OR p.Folio LIKE '%' + {folio} + '%')
                  AND ({saleFolio} IS NULL OR q.billCode = {saleFolio})
                  AND ({branchCode} IS NULL OR sb.code = {branchCode})
                  AND ({startDate} IS NULL OR p.PaymentDate >= {startDate})
                  AND ({endDate} IS NULL OR p.PaymentDate <= {endDate})
                  AND ({saleStartDate} IS NULL OR q.created_at >= {saleStartDate})
                  AND ({saleEndDate} IS NULL OR q.created_at <= {saleEndDate})
                  AND (({kingdeeSaleFolio} IS NULL AND {kingdeeStartDate} IS NULL
                            AND {kingdeeEndDate} IS NULL) OR EXISTS (
                        SELECT 1
                        FROM PaymentApplications pa
                        LEFT JOIN kingdee_sales_invoices inv ON pa.isPOS = 0 AND inv.id = pa.SaleId
                        LEFT JOIN KingDeeSalesPOS pos ON pa.isPOS = 1 AND pos.Id = pa.SaleId
                        WHERE pa.PaymentId = p.Id
                          AND ({kingdeeSaleFolio} IS NULL
                                OR CASE WHEN pa.isPOS = 1 THEN pos.Folio ELSE inv.bill_code END
                                   = {kingdeeSaleFolio})
                          AND ({kingdeeStartDate} IS NULL
                                OR COALESCE(inv.bill_date, pos.BillDate) >= {kingdeeStartDate})
                          AND ({kingdeeEndDate} IS NULL
                                OR COALESCE(inv.bill_date, pos.BillDate) <= {kingdeeEndDate})))
                  AND ({paymentFormId} IS NULL OR p.PaymentFormId = {paymentFormId})
                  AND ({bankId} IS NULL OR p.BankId = {bankId})
                  AND ({departmentId} IS NULL OR p.DepartmentId = {departmentId})
                  AND ({sellerId} IS NULL OR p.SellerId = {sellerId})
                  AND ({paymentType} IS NULL OR LOWER(p.PaymentType) = {paymentType})
                ORDER BY p.Id DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY")
            .ToListAsync();
    }

    /// <summary>
    /// English statuses of the filter turned into the BambooERP names they are stored
    /// with. Null when the filter did not ask for any, which means every status.
    /// </summary>
    private static string? InternalStatusNames(PaymentsFilter filter)
    {
        var statuses = filter.StatusValues();

        return statuses.Length == 0
            ? null
            : string.Join(",", statuses
                .SelectMany(PaymentStatusMapper.ToInternalNames)
                .Distinct());
    }

    private static PaymentResponse MapPayment(PaymentRow row)
    {
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
            CreatedAt = row.CreatedAt,
            Kingdee = MapKingdee(row)
        };
    }

    private static KingdeePaymentFields MapKingdee(PaymentRow row)
    {
        var (sellerId, sellerCode) = SplitSellerCode(row.SellerCodeRaw);

        return new KingdeePaymentFields
        {
            FBillNo = row.KingdeeBillNo,
            FDate = row.PaymentDate,
            FBizOrgId = row.BizOrgId,
            FBizOrg = row.BizOrgCode,
            FSETTLEORGID = row.SettleOrgId,
            FSETTLEORG = row.SettleOrgCode,
            FBranchID = row.KingdeeBranchId,
            Fbranch = row.KingdeeBranchCode?.Trim(),
            FSalerID = sellerId,
            FSaler = sellerCode,
            FCashierID = row.CashierId,
            FCashier = row.CashierCode,
            FCustomerID = row.CustomerId,
            FCustomer = row.CustomerCode,
            FSETTLECURRENCYID = row.SettleCurrencyId,
            FSETTLECURRENCY = row.SettleCurrencyCode,
            FNote = row.Comentary,
            FCardID = row.CardId,
            FCard = row.CardNumber,
            FMemberID = row.MemberId,
            FMember = row.MemberCardNumber,
            FAccountID = row.KingdeeAccountId,
            FAccount = row.KingdeeAccountCode,
            FRechargeAmount = row.RechargeAmount,
            FReceiveTypeID = row.ReceiveTypeId,
            FReceiveType = row.ReceiveTypeCode,
            FReceiveCurrencyID = row.ReceiveCurrencyId,
            FReceiveCurrency = row.ReceiveCurrencyCode,
            FReceiveAmt = row.Amount,
            FExchangeRate = row.ExchangeRate
        };
    }

    /// <summary>
    /// <c>catUsers.code_seller</c> holds the salesperson as
    /// <c>kingdeeId_kingdeeCode_branchId</c> (e.g. <c>1772_GW000041_1148515</c>). Older
    /// rows carry free text instead, so anything that does not match is ignored.
    /// </summary>
    private static (int? Id, string? Code) SplitSellerCode(string? codeSeller)
    {
        if (string.IsNullOrWhiteSpace(codeSeller))
            return (null, null);

        var parts = codeSeller.Trim().Split('_');

        if (parts.Length < 2 || !int.TryParse(parts[0], out var kingdeeId))
            return (null, null);

        return (kingdeeId, string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1]);
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

    private sealed class PaymentSaleRow
    {
        public int PaymentId { get; set; }
        public int SaleId { get; set; }
        public bool IsPos { get; set; }
        public string? Folio { get; set; }

        [Precision(18, 2)]
        public decimal AmountApplied { get; set; }

        public DateTime? AppliedDate { get; set; }
    }

    private sealed class PaymentStatusSummaryRow
    {
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public int Count { get; set; }

        /// <summary>
        /// Sum of a whole status, so it goes well above a single payment. Without the
        /// precision EF falls back to its default and truncates the total silently.
        /// </summary>
        [Precision(18, 2)]
        public decimal Amount { get; set; }
    }

    private sealed class PaymentRow
    {
        public int PaymentId { get; set; }
        public string? Folio { get; set; }
        public int CustomerId { get; set; }
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

        public string? KingdeeBillNo { get; set; }
        public int? BizOrgId { get; set; }
        public string? BizOrgCode { get; set; }
        public int? SettleOrgId { get; set; }
        public string? SettleOrgCode { get; set; }
        public int? CashierId { get; set; }
        public string? CashierCode { get; set; }
        public int? KingdeeAccountId { get; set; }
        public string? KingdeeAccountCode { get; set; }
        public int? ReceiveTypeId { get; set; }
        public string? ReceiveTypeCode { get; set; }
        public int? SettleCurrencyId { get; set; }
        public string? SettleCurrencyCode { get; set; }
        public int? ReceiveCurrencyId { get; set; }
        public string? ReceiveCurrencyCode { get; set; }
        public decimal? ExchangeRate { get; set; }
        public int? CardId { get; set; }
        public string? CardNumber { get; set; }
        public int? MemberId { get; set; }
        public string? MemberCardNumber { get; set; }
        public decimal? RechargeAmount { get; set; }

        public int? KingdeeBranchId { get; set; }
        public string? KingdeeBranchCode { get; set; }
        public string? SellerCodeRaw { get; set; }
    }
}
