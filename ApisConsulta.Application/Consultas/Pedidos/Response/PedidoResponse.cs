namespace ApisConsulta.Application.Consultas.Pedidos.Response;

public class PedidoResponse
{
    public int PedidoId { get; set; }
    public string? Folio { get; set; }
    public string? Cliente { get; set; }
    public DateTime? Fecha { get; set; }
    public decimal Total { get; set; }
    public string? Estatus { get; set; }
}
