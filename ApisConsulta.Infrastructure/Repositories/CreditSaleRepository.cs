using ApisConsulta.Application.CreditSales.Queries;
using ApisConsulta.Application.CreditSales.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Mapping;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

/// <summary>
/// Credit sales queries against BambooERP.
///
/// A sale that went on credit is the document generated in Kingdee
/// (<c>kingdee_sales_invoices</c> with <c>isCredit = 1</c> and not cancelled) — the same
/// definition the ERP uses in its <c>ordersCredits</c> view, and the only place where the
/// balance and the settlement date of the sale live. The sale in BambooERP
/// (<c>quotation</c>, the one <c>/api/sales</c> publishes) is reached through
/// <c>quoteId</c> and is published as <c>saleFolio</c>.
///
/// The payments — the abonos — are the applications of a payment to that document
/// (<c>PaymentApplications</c> with <c>StatusId = 1</c> and <c>isPOS = 0</c>, joined to
/// <c>Payments</c>). A payment can be split across several sales, so what is published per
/// sale is <c>AmountApplied</c>, not the whole payment. The legacy pair
/// <c>applyPaymentsCredits</c>/<c>paymentsCredits</c> is not read: its rows were migrated
/// into <c>PaymentApplications</c>, which is what the ERP writes today.
///
/// The due date and the days left are computed exactly as <c>ordersCredits</c> does, so
/// the API and the ERP screen never disagree: for a customer on <c>Proceso CheckPlus</c>
/// the term is <c>creditDays</c> flat, for everyone else <c>creditDays + 3</c> skipping
/// Sundays.
/// </summary>
public class CreditSaleRepository : ICreditSaleRepository
{
    private readonly ApplicationDbContext _context;
    public CreditSaleRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedCreditSalesResponse> GetCreditSalesAsync(CreditSalesFilter filter)
    {
        // One read of the whole filtered set, projected down to the columns the metrics
        // need. It is a credit portfolio — thousands of rows at most — and resolving it in
        // one go is what keeps `summary`, `byCustomer` and the page from disagreeing with
        // each other, and the filter from being written three times.
        var metrics = await QueryMetricsAsync(filter);

        var response = new PagedCreditSalesResponse
        {
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalRecords = metrics.Count,
            TotalPages = (int)Math.Ceiling(metrics.Count / (double)filter.PageSize),
            Summary = BuildSummary(metrics)
        };

        if (metrics.Count == 0)
            return response;

        if (filter.IncludeCustomerSummary)
            response.ByCustomer = BuildCustomerSummary(metrics);

        var page = metrics
            .OrderByDescending(m => m.InvoiceId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        if (page.Count == 0)
            return response;

        // The page is fetched by id, so the filter does not have to be repeated: the rows
        // it adds are only the ones the list shows and the metrics do not need.
        var details = await QueryPageAsync(page.Select(m => m.InvoiceId));

        var payments = filter.IncludePayments
            ? await QueryPaymentsAsync(page.Select(m => m.InvoiceId))
            : [];

        response.Sales = page
            .Select(m =>
            {
                var detail = details.GetValueOrDefault(m.InvoiceId);
                var own = payments.GetValueOrDefault(m.InvoiceId) ?? [];

                foreach (var payment in own)
                {
                    payment.DaysFromSale = m.BillDate.HasValue && payment.PaymentDate.HasValue
                        ? (int)(payment.PaymentDate.Value.Date - m.BillDate.Value.Date).TotalDays
                        : null;
                }

                return new CreditSaleResponse
                {
                    InvoiceId = m.InvoiceId,
                    Folio = detail?.Folio?.Trim(),
                    SaleFolio = detail?.SaleFolio?.Trim(),
                    SaleId = detail?.SaleId > 0 ? detail.SaleId : null,
                    InvoiceCode = string.IsNullOrWhiteSpace(detail?.InvoiceCode)
                        ? null
                        : detail.InvoiceCode.Trim(),
                    BillDate = m.BillDate,
                    CustomerId = m.CustomerId,
                    CustomerCode = m.CustomerCode?.Trim(),
                    Customer = m.Customer?.Trim(),
                    BranchCode = detail?.BranchCode?.Trim(),
                    Branch = detail?.Branch?.Trim(),
                    Warehouse = detail?.Warehouse?.Trim(),
                    SellerId = detail?.SellerId,
                    Seller = string.IsNullOrWhiteSpace(detail?.Seller) ? null : detail.Seller.Trim(),
                    Total = m.Total,
                    Paid = m.Total - m.Balance,
                    Balance = m.Balance,
                    CreditDays = m.CreditDays,
                    DueDate = m.DueDate,
                    SettledAt = m.SettledAt,
                    DaysToSettle = m.DaysToSettle,
                    IsSettled = m.SettledAt.HasValue,
                    DaysRemaining = m.DaysRemaining,
                    DaysOverdue = m.DaysRemaining < 0 ? -m.DaysRemaining.Value : 0,
                    StatusRaw = m.StatusRaw,
                    Status = CreditSaleStatusMapper.Map(m.StatusRaw),
                    FirstPaymentDate = m.FirstPaymentDate,
                    LastPaymentDate = m.LastPaymentDate,
                    DaysToFirstPayment = DaysBetween(m.BillDate, m.FirstPaymentDate),
                    DaysToLastPayment = DaysBetween(m.BillDate, m.LastPaymentDate),
                    PaymentsCount = m.PaymentsCount,
                    PaymentsTotal = m.PaymentsTotal,
                    Payments = own
                };
            })
            .ToList();

        return response;
    }

    /// <summary>
    /// Every credit sale the filter matched, reduced to the columns the metrics are built
    /// from. It is the only place the filter is written.
    /// </summary>
    private async Task<List<CreditSaleMetricRow>> QueryMetricsAsync(CreditSalesFilter filter)
    {
        var statusNames = InternalStatusNames(filter);
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;
        var customerCode = filter.CustomerCode;
        var folio = filter.Folio;
        var saleFolio = filter.SaleFolio;
        var branchCode = filter.BranchCode;
        var sellerId = filter.SellerId;
        var minDays = filter.MinDaysToSettle;
        var maxDays = filter.MaxDaysToSettle;

        return await _context.Database
            .SqlQuery<CreditSaleMetricRow>($@"
                SELECT
                    cs.InvoiceId,
                    cs.BillDate,
                    cs.CustomerId,
                    cs.CustomerCode,
                    cs.Customer,
                    cs.CreditLimit,
                    cs.CreditUsed,
                    cs.CreditDays,
                    cs.Total,
                    cs.Balance,
                    cs.SettledAt,
                    cs.DueDate,
                    cs.DaysToSettle,
                    cs.DaysRemaining,
                    cs.StatusRaw,
                    cs.FirstPaymentDate,
                    cs.LastPaymentDate,
                    cs.PaymentsCount,
                    cs.PaymentsTotal
                FROM (
                    SELECT
                        k.id                                    AS InvoiceId,
                        k.bill_date                             AS BillDate,
                        k.customer_id                           AS CustomerId,
                        c.customer_code                         AS CustomerCode,
                        ISNULL(c.name, k.customer_name)         AS Customer,
                        cl.creditLimit                          AS CreditLimit,
                        cl.creditUsed                           AS CreditUsed,
                        cl.creditDays                           AS CreditDays,
                        ISNULL(k.bill_total_amount, 0)          AS Total,
                        ROUND(ISNULL(k.balance, 0), 2)          AS Balance,
                        k.conclusion_date                       AS SettledAt,
                        venc.dueDate                            AS DueDate,
                        d.elapsedDays                           AS DaysToSettle,
                        dr.daysRemaining                        AS DaysRemaining,
                        CASE
                            WHEN ROUND(ISNULL(k.balance, 0), 2) = 0 THEN 'Pagada'
                            WHEN dr.daysRemaining < 0 THEN 'Pago vencido'
                            ELSE 'Pendiente de pago'
                        END                                     AS StatusRaw,
                        pay.FirstPaymentDate                    AS FirstPaymentDate,
                        pay.LastPaymentDate                     AS LastPaymentDate,
                        ISNULL(pay.PaymentsCount, 0)            AS PaymentsCount,
                        ISNULL(pay.PaymentsTotal, 0)            AS PaymentsTotal
                    FROM kingdee_sales_invoices k
                    LEFT JOIN customers c ON c.customer_id = k.customer_id
                    LEFT JOIN quotation q ON q.id = k.quoteId
                    LEFT JOIN starnet_branches br ON br.id = k.branch_id
                    LEFT JOIN CreditLines cl ON cl.customerId = k.customer_id
                    -- The credit request carries the process the term is computed with. A
                    -- customer can have more than one, so it is picked with TOP 1 instead
                    -- of a join that would duplicate the sale.
                    OUTER APPLY (
                        SELECT TOP 1 cr.processType
                        FROM credits cr
                        WHERE cr.customerid = k.customer_id
                          AND cr.requestType = 'Cliente Nuevo'
                          AND cr.statusId = 51
                        ORDER BY cr.id DESC
                    ) cred
                    OUTER APPLY (
                        SELECT
                            DATEDIFF(DAY, CAST(k.bill_date AS date),
                                     CAST(ISNULL(k.conclusion_date, GETDATE()) AS date)) AS elapsedDays,
                            CAST(k.bill_date AS date) AS startDate,
                            CAST(ISNULL(k.conclusion_date, GETDATE()) AS date) AS endDate
                    ) d
                    -- Days left before the term runs out. Outside CheckPlus the ERP does
                    -- not count Sundays and grants three extra days; the Sunday test is
                    -- written on DATEPART so it does not depend on the language of the
                    -- login.
                    OUTER APPLY (
                        SELECT
                            CASE
                                WHEN cred.processType = 'Proceso CheckPlus' THEN
                                    cl.creditDays - d.elapsedDays
                                ELSE
                                    cl.creditDays
                                    - (
                                        (d.elapsedDays + 1)
                                        - (
                                            (DATEDIFF(WEEK, d.startDate, d.endDate) + 1)
                                            - CASE WHEN (DATEPART(WEEKDAY, d.startDate) + @@DATEFIRST - 1) % 7 = 0
                                                   THEN 0 ELSE 1 END
                                            - CASE WHEN (DATEPART(WEEKDAY, d.endDate) + @@DATEFIRST - 1) % 7 = 0
                                                   THEN 0 ELSE 1 END
                                        )
                                    )
                                    + 3
                            END AS daysRemaining
                    ) dr
                    OUTER APPLY (
                        SELECT
                            CASE
                                WHEN cred.processType = 'Proceso CheckPlus' THEN
                                    DATEADD(DAY, cl.creditDays, k.bill_date)
                                ELSE
                                    DATEADD(DAY,
                                        (cl.creditDays + 3)
                                        + DATEDIFF(WEEK, CAST(k.bill_date AS date),
                                                   DATEADD(DAY, cl.creditDays + 3, CAST(k.bill_date AS date)))
                                        + CASE WHEN (DATEPART(WEEKDAY, CAST(k.bill_date AS date)) + @@DATEFIRST - 1) % 7 = 0
                                               THEN 1 ELSE 0 END,
                                        CAST(k.bill_date AS date))
                            END AS dueDate
                    ) venc
                    -- The abonos of the sale, collapsed. `StatusId = 1` leaves out the
                    -- applications that were undone, `isPOS = 0` the ones pointing at a
                    -- point-of-sale ticket instead of this invoice.
                    OUTER APPLY (
                        SELECT
                            MIN(p.PaymentDate)              AS FirstPaymentDate,
                            MAX(p.PaymentDate)              AS LastPaymentDate,
                            COUNT(1)                        AS PaymentsCount,
                            ISNULL(SUM(pa.AmountApplied), 0) AS PaymentsTotal
                        FROM PaymentApplications pa
                        JOIN Payments p ON p.Id = pa.PaymentId
                        WHERE pa.SaleId = k.id
                          AND ISNULL(pa.isPOS, 0) = 0
                          AND pa.StatusId = 1
                    ) pay
                    WHERE k.isCredit = 1
                      AND ISNULL(k.is_cancelled, 0) = 0
                      AND ({startDate} IS NULL OR k.bill_date >= {startDate})
                      AND ({endDate} IS NULL OR k.bill_date <= {endDate})
                      AND ({customerCode} IS NULL OR c.customer_code = {customerCode})
                      AND ({folio} IS NULL OR k.bill_code LIKE '%' + {folio} + '%')
                      AND ({saleFolio} IS NULL OR q.billCode = {saleFolio})
                      AND ({branchCode} IS NULL OR br.code = {branchCode})
                      AND ({sellerId} IS NULL OR q.usuarioId = {sellerId})
                ) cs
                WHERE ({statusNames} IS NULL OR LOWER(cs.StatusRaw) IN (
                        SELECT LOWER(LTRIM(RTRIM(value))) FROM STRING_SPLIT({statusNames}, ',')))
                  AND ({minDays} IS NULL OR cs.DaysToSettle >= {minDays})
                  AND ({maxDays} IS NULL OR cs.DaysToSettle <= {maxDays})")
            .ToListAsync();
    }

    /// <summary>
    /// The columns the list shows and the metrics do not need: the folios, the branch and
    /// the salesperson of the sale behind the invoice.
    /// </summary>
    private async Task<Dictionary<int, CreditSaleDetailRow>> QueryPageAsync(IEnumerable<int> invoiceIds)
    {
        var ids = string.Join(",", invoiceIds.Distinct());
        if (ids.Length == 0)
            return [];

        var rows = await _context.Database
            .SqlQuery<CreditSaleDetailRow>($@"
                SELECT
                    k.id                        AS InvoiceId,
                    k.bill_code                 AS Folio,
                    q.billCode                  AS SaleFolio,
                    k.quoteId                   AS SaleId,
                    k.FiscalInvoiceFolio        AS InvoiceCode,
                    br.code                     AS BranchCode,
                    ISNULL(br.name, k.branch_name) AS Branch,
                    k.warehouse_name            AS Warehouse,
                    q.usuarioId                 AS SellerId,
                    LTRIM(RTRIM(ISNULL(u.vchNombre, '') + ' ' + ISNULL(u.vchApellidos, ''))) AS Seller
                FROM kingdee_sales_invoices k
                LEFT JOIN quotation q ON q.id = k.quoteId
                LEFT JOIN catUsers u ON u.idUsuario = q.usuarioId
                LEFT JOIN starnet_branches br ON br.id = k.branch_id
                JOIN STRING_SPLIT({ids}, ',') sp ON sp.value = k.id")
            .ToListAsync();

        return rows.ToDictionary(r => r.InvoiceId);
    }

    /// <summary>
    /// The abonos of the page, resolved in one query and keyed by the sale they were
    /// applied to.
    /// </summary>
    private async Task<Dictionary<int, List<CreditSalePaymentResponse>>> QueryPaymentsAsync(
        IEnumerable<int> invoiceIds)
    {
        var ids = string.Join(",", invoiceIds.Distinct());
        if (ids.Length == 0)
            return [];

        var rows = await _context.Database
            .SqlQuery<CreditSalePaymentRow>($@"
                SELECT
                    pa.SaleId                       AS InvoiceId,
                    p.Id                            AS PaymentId,
                    p.Folio                         AS Folio,
                    ISNULL(pa.AmountApplied, 0)     AS Amount,
                    ISNULL(p.Amount, 0)             AS PaymentAmount,
                    p.PaymentDate                   AS PaymentDate,
                    pa.AppliedDate                  AS AppliedDate,
                    f.vchCode                       AS PaymentFormCode,
                    ISNULL(f.UILabel, f.vchFPago)   AS PaymentForm,
                    b.banco                         AS Bank,
                    p.Reference                     AS Reference,
                    p.PaymentType                   AS PaymentType,
                    ISNULL(p.StatusId, 0)           AS StatusId,
                    e.vchNombre                     AS StatusRaw
                FROM PaymentApplications pa
                JOIN Payments p ON p.Id = pa.PaymentId
                LEFT JOIN sat_FormaPago f ON f.ID = p.PaymentFormId
                LEFT JOIN bancos b ON b.id = p.BankId
                LEFT JOIN catEstatus e ON e.idEstatus = p.StatusId
                JOIN STRING_SPLIT({ids}, ',') sp ON sp.value = pa.SaleId
                WHERE ISNULL(pa.isPOS, 0) = 0
                  AND pa.StatusId = 1
                ORDER BY pa.SaleId, p.PaymentDate, pa.Id")
            .ToListAsync();

        return rows
            .GroupBy(r => r.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Select(r => new CreditSalePaymentResponse
            {
                PaymentId = r.PaymentId,
                Folio = r.Folio?.Trim(),
                Amount = r.Amount,
                PaymentAmount = r.PaymentAmount,
                PaymentDate = r.PaymentDate,
                AppliedDate = r.AppliedDate,
                PaymentFormCode = r.PaymentFormCode?.Trim(),
                // vchFPago carries a trailing line break in the catalog.
                PaymentForm = r.PaymentForm?.Trim(),
                Bank = string.IsNullOrWhiteSpace(r.Bank) ? null : r.Bank.Trim(),
                Reference = string.IsNullOrWhiteSpace(r.Reference) ? null : r.Reference.Trim(),
                PaymentType = r.PaymentType?.Trim(),
                StatusId = r.StatusId,
                StatusRaw = r.StatusRaw,
                Status = PaymentStatusMapper.Map(r.StatusRaw)
            }).ToList());
    }

    private static CreditSalesSummaryResponse BuildSummary(List<CreditSaleMetricRow> metrics)
    {
        var settled = metrics.Where(m => m.SettledAt.HasValue).ToList();
        var open = metrics.Where(m => !m.SettledAt.HasValue).ToList();
        var overdue = metrics.Where(m => m.StatusRaw == "Pago vencido").ToList();

        return new CreditSalesSummaryResponse
        {
            TotalSales = metrics.Count,
            TotalCustomers = metrics.Select(m => m.CustomerId).Distinct().Count(),
            TotalAmount = metrics.Sum(m => m.Total),
            PaidAmount = metrics.Sum(m => m.Total - m.Balance),
            OutstandingBalance = metrics.Sum(m => m.Balance),
            PaidCount = metrics.Count(m => m.StatusRaw == "Pagada"),
            OverdueCount = overdue.Count,
            PendingCount = metrics.Count(m => m.StatusRaw == "Pendiente de pago"),
            OverdueBalance = overdue.Sum(m => m.Balance),
            AvgDaysToSettle = Average(settled.Select(m => (decimal)m.DaysToSettle)),
            WeightedAvgDaysToSettle = WeightedAverage(settled),
            MaxDaysToSettle = settled.Count == 0 ? null : settled.Max(m => m.DaysToSettle),
            AvgDaysToFirstPayment = Average(metrics
                .Select(m => DaysBetween(m.BillDate, m.FirstPaymentDate))
                .Where(d => d.HasValue)
                .Select(d => (decimal)d!.Value)),
            AvgDaysToLastPayment = Average(metrics
                .Select(m => DaysBetween(m.BillDate, m.LastPaymentDate))
                .Where(d => d.HasValue)
                .Select(d => (decimal)d!.Value)),
            AvgDaysOutstanding = Average(open.Select(m => (decimal)m.DaysToSettle)),
            PaymentsCount = metrics.Sum(m => m.PaymentsCount),
            PaymentsTotal = metrics.Sum(m => m.PaymentsTotal)
        };
    }

    private static List<CreditSalesCustomerResponse> BuildCustomerSummary(
        List<CreditSaleMetricRow> metrics)
    {
        return metrics
            .GroupBy(m => m.CustomerId)
            .Select(g =>
            {
                var first = g.First();
                var settled = g.Where(m => m.SettledAt.HasValue).ToList();
                var open = g.Where(m => !m.SettledAt.HasValue).ToList();
                var overdue = g.Where(m => m.StatusRaw == "Pago vencido").ToList();

                return new CreditSalesCustomerResponse
                {
                    CustomerId = g.Key,
                    CustomerCode = first.CustomerCode?.Trim(),
                    Customer = first.Customer?.Trim(),
                    CreditLimit = first.CreditLimit,
                    CreditUsed = first.CreditUsed,
                    CreditDays = first.CreditDays,
                    Sales = g.Count(),
                    TotalAmount = g.Sum(m => m.Total),
                    PaidAmount = g.Sum(m => m.Total - m.Balance),
                    OutstandingBalance = g.Sum(m => m.Balance),
                    PaidCount = g.Count(m => m.StatusRaw == "Pagada"),
                    OverdueCount = overdue.Count,
                    PendingCount = g.Count(m => m.StatusRaw == "Pendiente de pago"),
                    OverdueBalance = overdue.Sum(m => m.Balance),
                    AvgDaysToSettle = Average(settled.Select(m => (decimal)m.DaysToSettle)),
                    WeightedAvgDaysToSettle = WeightedAverage(settled),
                    MaxDaysToSettle = settled.Count == 0 ? null : settled.Max(m => m.DaysToSettle),
                    AvgDaysToLastPayment = Average(g
                        .Select(m => DaysBetween(m.BillDate, m.LastPaymentDate))
                        .Where(d => d.HasValue)
                        .Select(d => (decimal)d!.Value)),
                    AvgDaysOutstanding = Average(open.Select(m => (decimal)m.DaysToSettle))
                };
            })
            .OrderByDescending(c => c.OutstandingBalance)
            .ThenByDescending(c => c.TotalAmount)
            .ToList();
    }

    /// <summary>
    /// Days to settle weighted by the amount of each sale: a large invoice paid late
    /// weighs more than a small one. Falls back to the plain average when every sale in
    /// the set was billed at zero.
    /// </summary>
    private static decimal? WeightedAverage(List<CreditSaleMetricRow> settled)
    {
        if (settled.Count == 0)
            return null;

        var weight = settled.Sum(m => m.Total);

        return weight == 0
            ? Average(settled.Select(m => (decimal)m.DaysToSettle))
            : Math.Round(settled.Sum(m => m.Total * m.DaysToSettle) / weight, 2);
    }

    private static decimal? Average(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    /// <summary>
    /// Whole days between two dates, negative when the second one came first — a customer
    /// paying before the sale was billed.
    /// </summary>
    private static int? DaysBetween(DateTime? from, DateTime? to)
        => from.HasValue && to.HasValue
            ? (int)(to.Value.Date - from.Value.Date).TotalDays
            : null;

    /// <summary>
    /// English statuses of the filter turned into the names the ERP computes them with.
    /// Null when the filter did not ask for any, which means every status.
    /// </summary>
    private static string? InternalStatusNames(CreditSalesFilter filter)
    {
        var statuses = filter.StatusValues();

        return statuses.Length == 0
            ? null
            : string.Join(",", statuses
                .SelectMany(CreditSaleStatusMapper.ToInternalNames)
                .Distinct());
    }

    private sealed class CreditSaleMetricRow
    {
        public int InvoiceId { get; set; }
        public DateTime? BillDate { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? Customer { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? CreditUsed { get; set; }
        public int? CreditDays { get; set; }
        public decimal Total { get; set; }
        public decimal Balance { get; set; }
        public DateTime? SettledAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int DaysToSettle { get; set; }
        public int? DaysRemaining { get; set; }
        public string? StatusRaw { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int PaymentsCount { get; set; }
        public decimal PaymentsTotal { get; set; }
    }

    private sealed class CreditSaleDetailRow
    {
        public int InvoiceId { get; set; }
        public string? Folio { get; set; }
        public string? SaleFolio { get; set; }
        public int? SaleId { get; set; }
        public string? InvoiceCode { get; set; }
        public string? BranchCode { get; set; }
        public string? Branch { get; set; }
        public string? Warehouse { get; set; }
        public int? SellerId { get; set; }
        public string? Seller { get; set; }
    }

    private sealed class CreditSalePaymentRow
    {
        public int InvoiceId { get; set; }
        public int PaymentId { get; set; }
        public string? Folio { get; set; }
        public decimal Amount { get; set; }
        public decimal PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string? PaymentFormCode { get; set; }
        public string? PaymentForm { get; set; }
        public string? Bank { get; set; }
        public string? Reference { get; set; }
        public string? PaymentType { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
    }
}
