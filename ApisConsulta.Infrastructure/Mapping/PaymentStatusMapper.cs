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
}
