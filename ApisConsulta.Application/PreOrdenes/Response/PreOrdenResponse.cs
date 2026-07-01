namespace ApisConsulta.Application.PreOrdenes.Response;

public class PreOrdenResponse
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PreOrdenItemResponse> Items { get; set; } = [];
}

public class PreOrdenItemResponse
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Vista resumida para el listado que consulta el vendedor.</summary>
public class PreOrdenResumenResponse
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int TotalItems { get; set; }
    public DateTime CreatedAt { get; set; }
}
