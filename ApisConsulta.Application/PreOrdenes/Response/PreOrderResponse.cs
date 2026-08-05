namespace ApisConsulta.Application.PreOrdenes.Response;

/// <summary>
/// English-facing view of a pre-order with per-item stock breakdown.
/// Used only by <c>GET /api/preorders/detail</c> (reviewed by the China team).
/// The Spanish endpoints keep using <see cref="PreOrdenResponse"/>.
/// </summary>
public class PreOrderResponse
{
    public int Id { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    /// <summary>PENDING, TAKEN, CONVERTED or CANCELLED.</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PreOrderItemResponse> Items { get; set; } = [];
}

public class PreOrderItemResponse
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Total deliverable stock across all sales warehouses.</summary>
    public int AvailableStock { get; set; }

    /// <summary>How much of the requested quantity can be fulfilled with the available stock.</summary>
    public int CoveredQuantity { get; set; }

    /// <summary>How much cannot be fulfilled (shortage / out of stock).</summary>
    public int ShortageQuantity { get; set; }

    /// <summary>
    /// COVERED (a single warehouse covers it all), DISTRIBUTE (enough stock but spread
    /// across several warehouses), PARTIALLY_COVERED (stock is short) or OUT_OF_STOCK
    /// (no warehouse has any stock).
    /// </summary>
    public string FulfillmentStatus { get; set; } = string.Empty;

    /// <summary>Stock breakdown per warehouse (only warehouses that hold stock).</summary>
    public List<WarehouseStockResponse> Warehouses { get; set; } = [];
}

/// <summary>Stock of a product in a sales warehouse.</summary>
public class WarehouseStockResponse
{
    public int WarehouseId { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    /// <summary>Deliverable units in this warehouse.</summary>
    public int AvailableStock { get; set; }
    /// <summary>Units suggested to fulfill from this warehouse (greedy allocation).</summary>
    public int QuantityToFulfill { get; set; }
}
