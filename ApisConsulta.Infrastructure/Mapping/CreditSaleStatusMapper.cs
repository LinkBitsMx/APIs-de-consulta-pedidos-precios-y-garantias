namespace ApisConsulta.Infrastructure.Mapping;

/// <summary>
/// Maps the status the ERP computes for a credit sale (the <c>ordersCredits</c> view, in
/// Spanish) to the English values published by the credit sales API, and back.
/// </summary>
public static class CreditSaleStatusMapper
{
    public static string Map(string? name)
    {
        return name?.ToLower() switch
        {
            "pagada" => "PAID",
            "pago vencido" => "OVERDUE",
            "pendiente de pago" => "PENDING",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Inverse of <see cref="Map"/>: the name the ERP computes behind an English status,
    /// ready to filter with. An unknown value maps to nothing, so a filter built from it
    /// matches no sale instead of every one — the request is rejected earlier anyway, in
    /// <c>GetCreditSalesQueryHandler</c>.
    /// </summary>
    public static string[] ToInternalNames(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "PAID" => ["Pagada"],
            "OVERDUE" => ["Pago vencido"],
            "PENDING" => ["Pendiente de pago"],
            _ => []
        };
    }
}
