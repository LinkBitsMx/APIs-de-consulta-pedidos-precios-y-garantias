namespace ApisConsulta.Infrastructure.Mapping;

/// <summary>
/// Maps the payment statuses of BambooERP (<c>catEstatus</c>, stored in Spanish and
/// shared with other modules) to the English values published by the sales API.
/// </summary>
public static class PaymentStatusMapper
{
    public static string Map(string? name)
    {
        return name?.ToLower() switch
        {
            "valido"     => "VALID",
            "rechazado"  => "REJECTED",
            "pendiente"  => "PENDING",
            "en proceso" => "IN_PROCESS",
            "cancelado"  => "CANCELLED",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Inverse of <see cref="Map"/>: the BambooERP names (<c>catEstatus.vchNombre</c>,
    /// lower cased) behind an English status, ready to filter with. An unknown value maps
    /// to nothing, so a filter built from it matches no payment instead of every one —
    /// the request is rejected earlier anyway, in <c>GetPaymentsQueryHandler</c>.
    /// </summary>
    public static string[] ToInternalNames(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            "VALID"      => ["valido"],
            "REJECTED"   => ["rechazado"],
            "PENDING"    => ["pendiente"],
            "IN_PROCESS" => ["en proceso"],
            "CANCELLED"  => ["cancelado"],
            _ => []
        };
    }
}
