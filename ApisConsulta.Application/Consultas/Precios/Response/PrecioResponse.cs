namespace ApisConsulta.Application.Consultas.Precios.Response;

public class PrecioResponse
{
    public string ProductoId { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Nombre { get; set; }
    public string? Sku { get; set; }
    public List<PrecioSucursalResponse> PreciosPorSucursal { get; set; } = [];
}

public class PrecioSucursalResponse
{
    public string? Sucursal { get; set; }
    public decimal PrecioMayoreo { get; set; }
    public decimal PrecioCaja { get; set; }
    public string Moneda { get; set; } = "MXN";
    public bool IncluyeIva { get; set; } = true;
}
