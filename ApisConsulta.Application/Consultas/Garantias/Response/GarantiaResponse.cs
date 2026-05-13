namespace ApisConsulta.Application.Consultas.Garantias.Response;

public class GarantiaResponse
{
    public string? FolioTicket { get; set; }
    public List<GarantiaProductoResponse> Productos { get; set; } = [];
}

public class GarantiaProductoResponse
{
    public string? Producto { get; set; }
    public DateTime? FechaIngreso { get; set; }
    public string? Resultado { get; set; }
    public string? Estatus { get; set; }
}
