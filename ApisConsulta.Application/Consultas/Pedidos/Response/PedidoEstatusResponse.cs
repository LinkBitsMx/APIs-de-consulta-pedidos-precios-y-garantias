namespace ApisConsulta.Application.Consultas.Pedidos.Response;

public class PedidoEstatusResponse
{
    public int PedidoId { get; set; }
    public string? Estatus { get; set; }
    public DateTime? FechaEstatus { get; set; }
}
