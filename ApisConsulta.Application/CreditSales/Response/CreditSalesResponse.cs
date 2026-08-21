namespace ApisConsulta.Application.CreditSales.Response;

/// <summary>
/// Page of credit sales, each with the payments applied against it, plus the metrics of
/// the whole filtered set: how many credit sales there are and how long the customer took
/// to pay them.
/// </summary>
public class PagedCreditSalesResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }

    /// <summary>Metrics over every credit sale the filter matched, not just this page.</summary>
    public CreditSalesSummaryResponse Summary { get; set; } = new();

    /// <summary>
    /// One entry per customer in the filtered set, ordered by outstanding balance
    /// descending. Empty when the request asked for
    /// <c>includeCustomerSummary=false</c>.
    /// </summary>
    public List<CreditSalesCustomerResponse> ByCustomer { get; set; } = [];

    /// <summary>Credit sales of this page, newest first.</summary>
    public List<CreditSaleResponse> Sales { get; set; } = [];
}

/// <summary>
/// How many credit sales exist and how long they take to be paid. Every figure covers the
/// whole filtered set.
/// </summary>
public class CreditSalesSummaryResponse
{
    /// <summary>Credit sales the filter matched.</summary>
    public int TotalSales { get; set; }

    /// <summary>Distinct customers behind those sales.</summary>
    public int TotalCustomers { get; set; }

    /// <summary>Sum of <c>total</c>: everything that was sold on credit.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Sum of <c>paid</c>: how much of it has already been collected.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>Sum of <c>balance</c>: how much is still owed.</summary>
    public decimal OutstandingBalance { get; set; }

    /// <summary>Sales already settled (<c>status = PAID</c>).</summary>
    public int PaidCount { get; set; }

    /// <summary>Sales still owed and past their due date (<c>status = OVERDUE</c>).</summary>
    public int OverdueCount { get; set; }

    /// <summary>Sales still owed but within their credit days (<c>status = PENDING</c>).</summary>
    public int PendingCount { get; set; }

    /// <summary>Balance still owed by the sales in <c>OVERDUE</c>.</summary>
    public decimal OverdueBalance { get; set; }

    /// <summary>
    /// Average <c>daysToSettle</c> of the sales already settled: the plain answer to how
    /// long a customer takes to pay. Null when none of them is settled yet.
    /// </summary>
    public decimal? AvgDaysToSettle { get; set; }

    /// <summary>
    /// Same average weighted by the amount of each sale, so a large invoice paid late
    /// weighs more than a small one. This is the figure to read as days sales outstanding.
    /// </summary>
    public decimal? WeightedAvgDaysToSettle { get; set; }

    /// <summary>Longest <c>daysToSettle</c> among the settled sales.</summary>
    public int? MaxDaysToSettle { get; set; }

    /// <summary>
    /// Average days from the sale to its first payment, over the sales that have at least
    /// one. Measured on when the customer paid (<c>payments[].paymentDate</c>), not on
    /// when the ERP applied it.
    /// </summary>
    public decimal? AvgDaysToFirstPayment { get; set; }

    /// <summary>Same average, measured to the last payment of each sale.</summary>
    public decimal? AvgDaysToLastPayment { get; set; }

    /// <summary>
    /// Average days the sales still owed have been open, counted to today. Null when
    /// every matching sale is settled.
    /// </summary>
    public decimal? AvgDaysOutstanding { get; set; }

    /// <summary>Payments applied to the matched sales, and how much they add up to.</summary>
    public int PaymentsCount { get; set; }

    public decimal PaymentsTotal { get; set; }
}

/// <summary>The same metrics, per customer.</summary>
public class CreditSalesCustomerResponse
{
    public int CustomerId { get; set; }
    public string? CustomerCode { get; set; }
    public string? Customer { get; set; }

    /// <summary>Credit line of the customer (<c>CreditLines</c>), null when they have none.</summary>
    public decimal? CreditLimit { get; set; }

    public decimal? CreditUsed { get; set; }

    /// <summary>Days the customer is given to pay (<c>CreditLines.creditDays</c>).</summary>
    public int? CreditDays { get; set; }

    public int Sales { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int PendingCount { get; set; }
    public decimal OverdueBalance { get; set; }

    /// <summary>Average <c>daysToSettle</c> of the settled sales of this customer.</summary>
    public decimal? AvgDaysToSettle { get; set; }

    /// <summary>Same average weighted by the amount of each sale.</summary>
    public decimal? WeightedAvgDaysToSettle { get; set; }

    public int? MaxDaysToSettle { get; set; }

    /// <summary>Average days from each sale to its last payment.</summary>
    public decimal? AvgDaysToLastPayment { get; set; }

    /// <summary>Average days the unsettled sales of this customer have been open.</summary>
    public decimal? AvgDaysOutstanding { get; set; }
}

/// <summary>
/// A sale that went on credit. It is the document generated in Kingdee
/// (<c>kingdee_sales_invoices</c> with <c>isCredit = 1</c>), which is where the balance,
/// the settlement date and the payments applied against it live.
/// </summary>
public class CreditSaleResponse
{
    /// <summary>Id of the sale in Kingdee (<c>kingdee_sales_invoices.id</c>).</summary>
    public int InvoiceId { get; set; }

    /// <summary>Folio of the sale in Kingdee. Example: <c>XSCKD166491</c>.</summary>
    public string? Folio { get; set; }

    /// <summary>Folio of the same sale in BambooERP (<c>quotation.billCode</c>). Example: <c>2604-00328</c>.</summary>
    public string? SaleFolio { get; set; }

    /// <summary>Id of the sale in BambooERP (<c>quotation.id</c>), the one <c>/api/sales</c> works with.</summary>
    public int? SaleId { get; set; }

    /// <summary>Fiscal invoice folio, when the sale was invoiced (<c>FiscalInvoiceFolio</c>).</summary>
    public string? InvoiceCode { get; set; }

    public DateTime? BillDate { get; set; }

    public int CustomerId { get; set; }
    public string? CustomerCode { get; set; }
    public string? Customer { get; set; }

    public string? BranchCode { get; set; }
    public string? Branch { get; set; }
    public string? Warehouse { get; set; }

    public int? SellerId { get; set; }
    public string? Seller { get; set; }

    /// <summary>Amount the sale was billed for (<c>bill_total_amount</c>).</summary>
    public decimal Total { get; set; }

    /// <summary>Already collected: <c>total - balance</c>.</summary>
    public decimal Paid { get; set; }

    /// <summary>Still owed (<c>kingdee_sales_invoices.balance</c>).</summary>
    public decimal Balance { get; set; }

    /// <summary>Days of credit granted to the customer (<c>CreditLines.creditDays</c>).</summary>
    public int? CreditDays { get; set; }

    /// <summary>
    /// Date the sale had to be paid by. Computed the same way the ERP does it: for a
    /// <c>Proceso CheckPlus</c> customer it is <c>billDate + creditDays</c>; for everyone
    /// else <c>creditDays + 3</c>, skipping Sundays.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Date the sale was fully paid (<c>conclusion_date</c>). Null while it is owed.</summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>
    /// Days from the sale to its settlement. While the sale is still owed it counts to
    /// today, so it reads as how long it has been open.
    /// </summary>
    public int DaysToSettle { get; set; }

    /// <summary>The sale is settled, so <see cref="DaysToSettle"/> is final.</summary>
    public bool IsSettled { get; set; }

    /// <summary>Days left before the due date. Negative once the sale is overdue.</summary>
    public int? DaysRemaining { get; set; }

    /// <summary>Days past the due date. Zero while the sale is still within its term.</summary>
    public int DaysOverdue { get; set; }

    /// <summary>PAID, OVERDUE or PENDING.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Status exactly as the ERP shows it (in Spanish): <c>Pagada</c>, <c>Pago vencido</c>
    /// or <c>Pendiente de pago</c>.
    /// </summary>
    public string? StatusRaw { get; set; }

    /// <summary>Date of the first payment the customer made against this sale.</summary>
    public DateTime? FirstPaymentDate { get; set; }

    /// <summary>Date of the last one.</summary>
    public DateTime? LastPaymentDate { get; set; }

    /// <summary>Days from the sale to <see cref="FirstPaymentDate"/>. Negative when the customer paid in advance.</summary>
    public int? DaysToFirstPayment { get; set; }

    /// <summary>Days from the sale to <see cref="LastPaymentDate"/>.</summary>
    public int? DaysToLastPayment { get; set; }

    public int PaymentsCount { get; set; }

    /// <summary>Sum of <c>payments[].amount</c>: what was applied to this sale.</summary>
    public decimal PaymentsTotal { get; set; }

    /// <summary>
    /// Payments applied against the sale, oldest first. Empty when the request asked for
    /// <c>includePayments=false</c> or the sale has not been paid yet.
    /// </summary>
    public List<CreditSalePaymentResponse> Payments { get; set; } = [];
}

/// <summary>
/// A payment applied to a credit sale (<c>PaymentApplications</c>): the abono. One payment
/// can be split across several sales, so <see cref="Amount"/> is the part applied here,
/// not the whole payment.
/// </summary>
public class CreditSalePaymentResponse
{
    public int PaymentId { get; set; }

    /// <summary>Payment folio (<c>Payments.Folio</c>). Example: <c>PAY-0626-002061</c>.</summary>
    public string? Folio { get; set; }

    /// <summary>Amount of the payment applied to this sale (<c>AmountApplied</c>).</summary>
    public decimal Amount { get; set; }

    /// <summary>Amount of the whole payment, which may cover other sales too.</summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>When the customer paid (<c>Payments.PaymentDate</c>).</summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// When the ERP applied the payment to this sale (<c>AppliedDate</c>). It can be days
    /// after <see cref="PaymentDate"/>: that lag is the ERP's, not the customer's.
    /// </summary>
    public DateTime? AppliedDate { get; set; }

    /// <summary>Days from the sale to <see cref="PaymentDate"/>. Negative when paid in advance.</summary>
    public int? DaysFromSale { get; set; }

    /// <summary>SAT code of the payment form (<c>sat_FormaPago.vchCode</c>). Example: <c>03</c>.</summary>
    public string? PaymentFormCode { get; set; }

    public string? PaymentForm { get; set; }

    public string? Bank { get; set; }

    public string? Reference { get; set; }

    /// <summary>Payment type as stored (<c>Payments.PaymentType</c>): <c>payment</c>, <c>credit</c> or <c>advance</c>.</summary>
    public string? PaymentType { get; set; }

    public int StatusId { get; set; }

    /// <summary>Payment status as stored in BambooERP (<c>catEstatus</c>, in Spanish).</summary>
    public string? StatusRaw { get; set; }

    /// <summary>VALID, REJECTED, PENDING, IN_PROCESS, CANCELLED or UNKNOWN.</summary>
    public string? Status { get; set; }
}
