using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Response;
using MediatR;

namespace ApisConsulta.Application.PreOrdenes.Queries;

/// <summary>
/// Lists every pre-order with the full per-item stock breakdown (available stock per
/// warehouse, covered/shortage quantity, fulfillment status and suggested allocation).
/// English-facing endpoint reviewed by the China team.
/// </summary>
public class GetPreOrdersDetailQuery : IRequest<IReadOnlyList<PreOrderResponse>>
{
    /// <summary>Optional status filter in English: PENDING, TAKEN, CONVERTED, CANCELLED.</summary>
    public string? Status { get; set; }
}

public class GetPreOrdersDetailQueryHandler
    : IRequestHandler<GetPreOrdersDetailQuery, IReadOnlyList<PreOrderResponse>>
{
    private readonly IPreOrdenRepository _repository;

    public GetPreOrdersDetailQueryHandler(IPreOrdenRepository repository)
        => _repository = repository;

    // The pre-order status is stored in Spanish in the DB; expose it in English.
    private static readonly Dictionary<string, string> StatusEsToEn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PENDIENTE"] = "PENDING",
        ["TOMADA"] = "TAKEN",
        ["CONVERTIDA"] = "CONVERTED",
        ["CANCELADA"] = "CANCELLED",
    };

    private static readonly Dictionary<string, string> StatusEnToEs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PENDING"] = "PENDIENTE",
        ["TAKEN"] = "TOMADA",
        ["CONVERTED"] = "CONVERTIDA",
        ["CANCELLED"] = "CANCELADA",
    };

    private static readonly Dictionary<string, string> FulfillmentEsToEn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CUBIERTA"] = "COVERED",
        ["DISTRIBUIR"] = "DISTRIBUTE",
        ["AGOTADO_PARCIAL"] = "PARTIALLY_COVERED",
        ["SIN_STOCK"] = "OUT_OF_STOCK",
    };

    public async Task<IReadOnlyList<PreOrderResponse>> Handle(
        GetPreOrdersDetailQuery request, CancellationToken cancellationToken)
    {
        // The consumer filters in English; the repository queries the Spanish column.
        string? estatusEs = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
            estatusEs = StatusEnToEs.TryGetValue(request.Status.Trim(), out var es)
                ? es
                : request.Status.Trim();

        var preordenes = await _repository.GetAllDetalladoAsync(estatusEs);

        return preordenes.Select(MapToEnglish).ToList();
    }

    private static PreOrderResponse MapToEnglish(PreOrdenResponse p) => new()
    {
        Id = p.Id,
        Folio = p.Folio,
        CustomerCode = p.CustomerCode,
        Email = p.Email,
        Phone = p.Phone,
        Notes = p.Notes,
        Status = StatusEsToEn.TryGetValue(p.Status, out var st) ? st : p.Status,
        IsApproved = p.IsApproved,
        Total = p.Total,
        CreatedAt = p.CreatedAt,
        Items = p.Items.Select(MapItem).ToList(),
    };

    private static PreOrderItemResponse MapItem(PreOrdenItemResponse i) => new()
    {
        Id = i.Id,
        ProductCode = i.ProductCode,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        Amount = i.Amount,
        AvailableStock = i.StockDisponible,
        CoveredQuantity = i.CantidadCubierta,
        ShortageQuantity = i.CantidadAgotada,
        FulfillmentStatus = FulfillmentEsToEn.TryGetValue(i.EstadoSurtido, out var fs) ? fs : i.EstadoSurtido,
        Warehouses = i.Almacenes.Select(a => new WarehouseStockResponse
        {
            WarehouseId = a.AlmacenId,
            Warehouse = a.Almacen,
            AvailableStock = a.StockDisponible,
            QuantityToFulfill = a.CantidadSurtir,
        }).ToList(),
    };
}
