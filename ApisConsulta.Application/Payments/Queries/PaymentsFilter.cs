namespace ApisConsulta.Application.Payments.Queries;

/// <summary>
/// Filter of the payments list. English-facing endpoint (reviewed by the China team):
/// the status is filtered and published in English, never with the Spanish name
/// BambooERP stores in <c>catEstatus</c>.
/// </summary>
public class PaymentsFilter
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Statuses a payment can be filtered by. They are the English values published in
    /// <c>status</c>; their BambooERP counterparts are resolved by
    /// <c>PaymentStatusMapper</c>.
    /// </summary>
    public static readonly string[] Statuses =
        ["VALID", "REJECTED", "PENDING", "IN_PROCESS", "CANCELLED"];

    /// <summary>
    /// One or more statuses, comma separated. Example: <c>REJECTED,IN_PROCESS</c> for the
    /// payments that were rejected plus the ones still being validated. All of them when
    /// omitted.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Status as stored in BambooERP (<c>catEstatus.idEstatus</c>), for the cases where
    /// the caller works with the internal id: 29 (Valido), 30 (Rechazado), 4 (PENDIENTE),
    /// 17 (EN PROCESO). Combines with <see cref="Status"/>.
    /// </summary>
    public int? StatusId { get; set; }

    public string? CustomerCode { get; set; }

    /// <summary>Payment folio (<c>Payments.Folio</c>), matched partially. Example: <c>PAY-0826</c>.</summary>
    public string? Folio { get; set; }

    /// <summary>Folio of the sale the payment was applied to (<c>quotation.billCode</c>).</summary>
    public string? SaleFolio { get; set; }

    /// <summary>Payments made on or after this date (<c>Payments.PaymentDate</c>).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Payments made on or before this date. The whole day is included.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Payments of sales registered in BambooERP on or after this date
    /// (<c>quotation.created_at</c>, the sale published as <see cref="SaleFolio"/>).
    /// Leaves out the payments that are not related to a sale.
    /// </summary>
    public DateTime? SaleStartDate { get; set; }

    /// <summary>Same range, upper bound. The whole day is included.</summary>
    public DateTime? SaleEndDate { get; set; }

    /// <summary>
    /// Folio of the sale as it was generated in Kingdee:
    /// <c>kingdee_sales_invoices.bill_code</c> (example: <c>XSCKD100949</c>) or
    /// <c>KingDeeSalesPOS.Folio</c> for a POS ticket (example:
    /// <c>10040002604109633</c>). Exact match, same as <see cref="SaleFolio"/>.
    /// </summary>
    public string? KingdeeSaleFolio { get; set; }

    /// <summary>
    /// Payments applied to a sale generated in Kingdee on or after this date:
    /// <c>kingdee_sales_invoices.bill_date</c>, or <c>KingDeeSalesPOS.BillDate</c> when
    /// the sale is a POS ticket. Keeps the payment when at least one of its applications
    /// falls in the range.
    /// </summary>
    public DateTime? KingdeeSaleStartDate { get; set; }

    /// <summary>Same range, upper bound. The whole day is included.</summary>
    public DateTime? KingdeeSaleEndDate { get; set; }

    /// <summary>
    /// Branch of the payment (<c>starnet_branches.code</c>), reached through its
    /// department: <c>Payments.DepartmentId</c> → <c>departments.branchId</c> →
    /// <c>starnet_branches.id</c>. It is the branch published as <c>kingdee.Fbranch</c>.
    /// Example: <c>801.01.01</c>
    /// </summary>
    public string? BranchCode { get; set; }

    public int? PaymentFormId { get; set; }
    public int? BankId { get; set; }
    public int? DepartmentId { get; set; }
    public int? SellerId { get; set; }

    /// <summary><c>payment</c>, <c>credit</c> or <c>advance</c>.</summary>
    public string? PaymentType { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// <see cref="Status"/> split, trimmed and upper cased. Empty when none was sent,
    /// which means every status.
    /// </summary>
    public string[] StatusValues()
        => string.IsNullOrWhiteSpace(Status)
            ? []
            : Status
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .Distinct()
                .ToArray();

    public PaymentsFilter Normalized() => new()
    {
        Status = Status,
        StatusId = StatusId,
        CustomerCode = string.IsNullOrWhiteSpace(CustomerCode) ? null : CustomerCode.Trim(),
        Folio = string.IsNullOrWhiteSpace(Folio) ? null : Folio.Trim(),
        SaleFolio = string.IsNullOrWhiteSpace(SaleFolio) ? null : SaleFolio.Trim(),
        StartDate = StartDate?.Date,
        EndDate = EndOfDay(EndDate),
        SaleStartDate = SaleStartDate?.Date,
        SaleEndDate = EndOfDay(SaleEndDate),
        KingdeeSaleFolio = string.IsNullOrWhiteSpace(KingdeeSaleFolio)
            ? null
            : KingdeeSaleFolio.Trim(),
        KingdeeSaleStartDate = KingdeeSaleStartDate?.Date,
        KingdeeSaleEndDate = EndOfDay(KingdeeSaleEndDate),
        BranchCode = string.IsNullOrWhiteSpace(BranchCode) ? null : BranchCode.Trim(),
        PaymentFormId = PaymentFormId,
        BankId = BankId,
        DepartmentId = DepartmentId,
        SellerId = SellerId,
        PaymentType = string.IsNullOrWhiteSpace(PaymentType) ? null : PaymentType.Trim().ToLower(),
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => 50,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        }
    };

    /// <summary>
    /// Upper bounds are received as a day and compared against a datetime: take the whole
    /// day when no time part was supplied.
    /// </summary>
    private static DateTime? EndOfDay(DateTime? date)
        => date.HasValue && date.Value.TimeOfDay == TimeSpan.Zero
            ? date.Value.AddDays(1).AddTicks(-1)
            : date;
}
