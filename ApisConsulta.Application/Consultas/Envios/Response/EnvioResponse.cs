namespace ApisConsulta.Application.Consultas.Envios.Response;

public class EnvioResponse
{
    public string PedidoId { get; set; } = string.Empty;
    public string Paqueteria { get; set; } = string.Empty;
    public string Guia { get; set; } = string.Empty;
    public string? TrackingUrl { get; set; }
    public string EstatusEnvio { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
}
