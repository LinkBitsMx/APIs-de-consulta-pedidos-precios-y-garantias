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

    /// <summary>Stock total entregable sumando todos los almacenes de venta.</summary>
    public int StockDisponible { get; set; }

    /// <summary>Cantidad de la solicitud que sí se puede cubrir con el stock disponible.</summary>
    public int CantidadCubierta { get; set; }

    /// <summary>Cantidad que no alcanza a cubrirse (agotado/faltante).</summary>
    public int CantidadAgotada { get; set; }

    /// <summary>
    /// Estado del surtido:
    /// CUBIERTA (un solo almacén cubre todo), DISTRIBUIR (hay stock suficiente
    /// pero repartido en varios almacenes), AGOTADO_PARCIAL (falta stock) o
    /// SIN_STOCK (ningún almacén tiene existencia).
    /// </summary>
    public string EstadoSurtido { get; set; } = string.Empty;

    /// <summary>Desglose de existencias por almacén (solo almacenes con stock).</summary>
    public List<StockAlmacenResponse> Almacenes { get; set; } = [];
}

/// <summary>Existencia de un producto en un almacén de venta.</summary>
public class StockAlmacenResponse
{
    public int AlmacenId { get; set; }
    public string Almacen { get; set; } = string.Empty;
    /// <summary>Piezas entregables en este almacén.</summary>
    public int StockDisponible { get; set; }
    /// <summary>Piezas sugeridas a surtir desde este almacén (reparto greedy).</summary>
    public int CantidadSurtir { get; set; }
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

/// <summary>Vista agregada para métricas de cada pre-orden.</summary>
public class PreOrdenMetricasResponse
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public int RequestedItemsCount { get; set; }
    public int RequestedQuantity { get; set; }
    public decimal RequestedAmount { get; set; }
    public int ConfirmedQuantity { get; set; }
    public decimal ConfirmedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PreOrdenMetricItemResponse> Items { get; set; } = [];
}

public class PreOrdenMetricItemResponse
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public decimal RequestedAmount { get; set; }
    public int ConfirmedQuantity { get; set; }
    public decimal ConfirmedAmount { get; set; }
}
