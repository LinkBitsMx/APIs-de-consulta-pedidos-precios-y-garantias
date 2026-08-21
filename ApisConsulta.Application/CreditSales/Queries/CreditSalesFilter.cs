namespace ApisConsulta.Application.CreditSales.Queries;

/// <summary>
/// Filter of the credit sales list. English-facing endpoint (reviewed by the China team):
/// the status is filtered and published in English, never with the Spanish name the ERP
/// shows on screen.
/// </summary>
public class CreditSalesFilter
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Statuses a credit sale can be filtered by. They are the English values published in
    /// <c>status</c>; their BambooERP counterparts are resolved by
    /// <c>CreditSaleStatusMapper</c>.
    /// </summary>
    public static readonly string[] Statuses = ["PAID", "OVERDUE", "PENDING"];

    /// <summary>
    /// One or more statuses, comma separated. Example: <c>OVERDUE,PENDING</c> for
    /// everything still owed. All of them when omitted.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>Credit sales billed on or after this date (<c>kingdee_sales_invoices.bill_date</c>).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Same range, upper bound. The whole day is included.</summary>
    public DateTime? EndDate { get; set; }

    public string? CustomerCode { get; set; }

    /// <summary>
    /// Folio of the sale as it was generated in Kingdee
    /// (<c>kingdee_sales_invoices.bill_code</c>), matched partially. Example: <c>XSCKD16</c>.
    /// </summary>
    public string? Folio { get; set; }

    /// <summary>Folio of the sale in BambooERP (<c>quotation.billCode</c>). Exact match.</summary>
    public string? SaleFolio { get; set; }

    /// <summary>
    /// Branch of the sale in Kingdee (<c>starnet_branches.code</c> reached through
    /// <c>kingdee_sales_invoices.branch_id</c>). It is the top level branch — example:
    /// <c>801</c> — not the department-derived code the sales API filters by.
    /// </summary>
    public string? BranchCode { get; set; }

    /// <summary>Salesperson of the underlying sale (<c>quotation.usuarioId</c>).</summary>
    public int? SellerId { get; set; }

    /// <summary>
    /// Credit sales that took at least this many days to be settled. Applied over
    /// <c>daysToSettle</c>, so an unsettled sale counts the days it has been open so far.
    /// </summary>
    public int? MinDaysToSettle { get; set; }

    /// <summary>Same bound, upper end.</summary>
    public int? MaxDaysToSettle { get; set; }

    /// <summary>
    /// Include the payments of every credit sale. On by default: they are what the
    /// endpoint exists for. Turn it off for a lighter payload when only the metrics matter.
    /// </summary>
    public bool IncludePayments { get; set; } = true;

    /// <summary>
    /// Include the per-customer breakdown in <c>byCustomer</c>. On by default; it costs
    /// one extra query over the whole filtered set, not just the page.
    /// </summary>
    public bool IncludeCustomerSummary { get; set; } = true;

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

    public CreditSalesFilter Normalized() => new()
    {
        Status = Status,
        StartDate = StartDate?.Date,
        EndDate = EndOfDay(EndDate),
        CustomerCode = string.IsNullOrWhiteSpace(CustomerCode) ? null : CustomerCode.Trim(),
        Folio = string.IsNullOrWhiteSpace(Folio) ? null : Folio.Trim(),
        SaleFolio = string.IsNullOrWhiteSpace(SaleFolio) ? null : SaleFolio.Trim(),
        BranchCode = string.IsNullOrWhiteSpace(BranchCode) ? null : BranchCode.Trim(),
        SellerId = SellerId,
        MinDaysToSettle = MinDaysToSettle,
        MaxDaysToSettle = MaxDaysToSettle,
        IncludePayments = IncludePayments,
        IncludeCustomerSummary = IncludeCustomerSummary,
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
