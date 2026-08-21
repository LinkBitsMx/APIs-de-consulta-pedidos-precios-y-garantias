namespace ApisConsulta.Application.Payments.Response;

/// <summary>
/// Page of payments plus the breakdown by status of everything the filter matched, so a
/// caller asking for rejected/validated/in-process payments gets both the records and
/// how many (and how much) each status adds up to.
/// </summary>
public class PagedPaymentsResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }

    /// <summary>Sum of <c>payments[].amount</c> over the whole filter, not just this page.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// One entry per status present in the filtered set, ordered by status id. It covers
    /// every matching payment, not only the ones in this page.
    /// </summary>
    public List<PaymentStatusSummaryResponse> Summary { get; set; } = [];

    /// <summary>Payments of this page, newest first, each with its full record.</summary>
    public List<PaymentResponse> Payments { get; set; } = [];
}

public class PaymentStatusSummaryResponse
{
    public int StatusId { get; set; }

    /// <summary>Status as stored in BambooERP (<c>catEstatus</c>, in Spanish).</summary>
    public string? StatusRaw { get; set; }

    /// <summary>VALID, REJECTED, PENDING, IN_PROCESS, CANCELLED or UNKNOWN.</summary>
    public string? Status { get; set; }

    public int Count { get; set; }

    /// <summary>Sum of the amounts of the payments in this status.</summary>
    public decimal Amount { get; set; }
}
