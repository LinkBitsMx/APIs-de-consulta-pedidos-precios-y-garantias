namespace ApisConsulta.Application.Consultas.Envios.Response;

public class EnvioResponse
{
    public string PedidoId { get; set; } = string.Empty;
    public string Paqueteria { get; set; } = string.Empty;
    public IReadOnlyList<EnvioGuiaResponse> Guias { get; set; } = Array.Empty<EnvioGuiaResponse>();
    public string EstatusEnvio { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
}

public class EnvioGuiaResponse
{
    public string Guia { get; set; } = string.Empty;
    public string? TrackingUrl { get; set; }
}
