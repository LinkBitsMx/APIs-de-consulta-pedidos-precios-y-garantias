using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Requests;
using ApisConsulta.Application.PreOrdenes.Response;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

public class PreOrdenRepository : IPreOrdenRepository
{
    private readonly ApplicationDbContext _context;
    public PreOrdenRepository(ApplicationDbContext context) => _context = context;

    public async Task<bool> ExisteCustomerAsync(string customerCode)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            return false;

        var total = await _context.Database
            .SqlQuery<int>($@"
                SELECT COUNT(1) AS [Value]
                FROM customers
                WHERE customer_code = {customerCode}")
            .FirstOrDefaultAsync();

        return total > 0;
    }

    public async Task<PreOrdenResponse> CrearAsync(CrearPreOrdenRequest request)
    {
        var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // OUTPUT ... INTO una variable de tabla: necesario porque en producción la tabla
        // request_quotation tiene triggers habilitados, y SQL Server no permite OUTPUT
        // sin INTO cuando la tabla destino tiene triggers (Error 334).
        var ids = await _context.Database
            .SqlQuery<int>($@"
                SET NOCOUNT ON;
                DECLARE @IdsInsertados TABLE (Value INT);
                INSERT INTO request_quotation (customer_code, email, telefono, notas, total, estatus, created_at)
                OUTPUT INSERTED.id INTO @IdsInsertados(Value)
                VALUES ({request.CustomerCode}, {request.Email}, {request.Phone}, {request.Notes},
                        {total}, 'PENDIENTE', GETDATE());
                SELECT Value FROM @IdsInsertados;")
            .ToListAsync();

        var preOrdenId = ids.First();

        // Folio legible: código de cliente + id secuencial (único), p. ej. C00123-00012.
        var folio = $"{request.CustomerCode}-{preOrdenId:D5}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE request_quotation SET folio = {folio} WHERE id = {preOrdenId}");

        foreach (var item in request.Items)
        {
            var amount = item.Quantity * item.UnitPrice;

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO request_quotation_items
                    (request_id, product_code, cantidad, precio_unitario, importe)
                VALUES ({preOrdenId}, {item.ProductCode},
                        {item.Quantity}, {item.UnitPrice}, {amount})");
        }

        await transaction.CommitAsync();

        // Re-lectura para devolver el registro tal cual quedó persistido (id, created_at, etc.).
        return (await GetByIdAsync(preOrdenId))!;
    }

    public async Task<IReadOnlyList<PreOrdenResumenResponse>> GetAllAsync(string? estatus)
    {
        var filtro = string.IsNullOrWhiteSpace(estatus) ? null : estatus.Trim().ToUpper();

        return await _context.Database
            .SqlQuery<PreOrdenResumenResponse>($@"
                SELECT
                    p.id                       AS Id,
                    p.folio                    AS Folio,
                    p.customer_code            AS CustomerCode,
                    p.estatus                  AS Status,
                    p.is_approved              AS IsApproved,
                    p.total                    AS Total,
                    ISNULL(COUNT(pi.id), 0)    AS TotalItems,
                    p.created_at               AS CreatedAt
                FROM request_quotation p
                LEFT JOIN request_quotation_items pi ON pi.request_id = p.id
                WHERE ({filtro} IS NULL OR p.estatus = {filtro})
                GROUP BY p.id, p.folio, p.customer_code, p.estatus, p.is_approved, p.total, p.created_at
                ORDER BY p.created_at DESC")
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PreOrdenMetricasResponse>> GetMetricasAsync(string? estatus)
    {
        var filtro = string.IsNullOrWhiteSpace(estatus) ? null : estatus.Trim().ToUpper();

        var cabeceras = await _context.Database
            .SqlQuery<PreOrdenMetricasRow>($@"
                SELECT
                    p.id                            AS Id,
                    p.folio                         AS Folio,
                    p.customer_code                 AS CustomerCode,
                    ISNULL(c.name, '')              AS CustomerName,
                    p.estatus                       AS Status,
                    p.is_approved                   AS IsApproved,
                    ISNULL(COUNT(pi.id), 0)         AS RequestedItemsCount,
                    ISNULL(SUM(pi.cantidad), 0)     AS RequestedQuantity,
                    ISNULL(SUM(pi.importe), 0)      AS RequestedAmount,
                    CASE WHEN p.is_approved = 1 THEN ISNULL(SUM(pi.cantidad), 0) ELSE 0 END AS ConfirmedQuantity,
                    CASE WHEN p.is_approved = 1 THEN ISNULL(SUM(pi.importe), 0) ELSE 0 END AS ConfirmedAmount,
                    p.created_at                    AS CreatedAt
                FROM request_quotation p
                LEFT JOIN customers c ON c.customer_code = p.customer_code
                LEFT JOIN request_quotation_items pi ON pi.request_id = p.id
                WHERE ({filtro} IS NULL OR p.estatus = {filtro})
                GROUP BY p.id, p.folio, p.customer_code, c.name, p.estatus, p.is_approved, p.created_at
                ORDER BY p.created_at DESC")
            .ToListAsync();

        if (cabeceras.Count == 0)
            return [];

        var requestIds = cabeceras.Select(c => c.Id).ToList();

        var items = await _context.Database
            .SqlQuery<PreOrdenMetricItemRow>($@"
                SELECT
                    pi.id                           AS Id,
                    p.id                            AS RequestId,
                    pi.product_code                 AS ProductCode,
                    pi.cantidad                     AS RequestedQuantity,
                    pi.importe                      AS RequestedAmount,
                    CASE WHEN p.is_approved = 1 THEN pi.cantidad ELSE 0 END AS ConfirmedQuantity,
                    CASE WHEN p.is_approved = 1 THEN pi.importe ELSE 0 END AS ConfirmedAmount
                FROM request_quotation_items pi
                INNER JOIN request_quotation p ON p.id = pi.request_id
                WHERE pi.request_id IN ({requestIds})
                ORDER BY pi.request_id, pi.id")
            .ToListAsync();

        var itemsPorRequest = items
            .GroupBy(i => i.RequestId)
            .ToDictionary(g => g.Key, g => g.Select(i => new PreOrdenMetricItemResponse
            {
                Id = i.Id,
                ProductCode = i.ProductCode,
                RequestedQuantity = i.RequestedQuantity,
                RequestedAmount = i.RequestedAmount,
                ConfirmedQuantity = i.ConfirmedQuantity,
                ConfirmedAmount = i.ConfirmedAmount
            }).ToList());

        return cabeceras
            .Select(c => new PreOrdenMetricasResponse
            {
                Id = c.Id,
                Folio = c.Folio,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Status = c.Status,
                IsApproved = c.IsApproved,
                RequestedItemsCount = c.RequestedItemsCount,
                RequestedQuantity = c.RequestedQuantity,
                RequestedAmount = c.RequestedAmount,
                ConfirmedQuantity = c.ConfirmedQuantity,
                ConfirmedAmount = c.ConfirmedAmount,
                CreatedAt = c.CreatedAt,
                Items = itemsPorRequest.TryGetValue(c.Id, out var metricItems)
                    ? metricItems
                    : []
            })
            .ToList();
    }

    public async Task<PreOrdenResponse?> GetByIdAsync(int id)
    {
        var cabecera = await _context.Database
            .SqlQuery<PreOrdenCabeceraRow>($@"
                SELECT
                    id            AS Id,
                    folio         AS Folio,
                    customer_code AS CustomerCode,
                    email         AS Email,
                    telefono    AS Phone,
                    notas       AS Notes,
                    estatus     AS Status,
                    is_approved AS IsApproved,
                    total       AS Total,
                    created_at  AS CreatedAt
                FROM request_quotation
                WHERE id = {id}")
            .FirstOrDefaultAsync();

        if (cabecera == null)
            return null;

        var itemRows = await _context.Database
            .SqlQuery<PreOrdenItemRow>($@"
                SELECT
                    request_id      AS RequestId,
                    id              AS Id,
                    product_code    AS ProductCode,
                    cantidad        AS Quantity,
                    precio_unitario AS UnitPrice,
                    importe         AS Amount
                FROM request_quotation_items
                WHERE request_id = {id}
                ORDER BY id")
            .ToListAsync();

        var items = itemRows
            .Select(r => new PreOrdenItemResponse
            {
                Id = r.Id,
                ProductCode = r.ProductCode,
                Quantity = r.Quantity,
                UnitPrice = r.UnitPrice,
                Amount = r.Amount
            })
            .ToList();

        var stockRows = await ConsultarStockAsync(requestId: id, estatus: null);
        EnriquecerItems(id, items, stockRows);

        return new PreOrdenResponse
        {
            Id = cabecera.Id,
            Folio = cabecera.Folio,
            CustomerCode = cabecera.CustomerCode,
            Email = cabecera.Email,
            Phone = cabecera.Phone,
            Notes = cabecera.Notes,
            Status = cabecera.Status,
            IsApproved = cabecera.IsApproved,
            Total = cabecera.Total,
            CreatedAt = cabecera.CreatedAt,
            Items = items
        };
    }

    public async Task<IReadOnlyList<PreOrdenResponse>> GetAllDetalladoAsync(string? estatus)
    {
        var filtro = string.IsNullOrWhiteSpace(estatus) ? null : estatus.Trim().ToUpper();

        var cabeceras = await _context.Database
            .SqlQuery<PreOrdenCabeceraRow>($@"
                SELECT
                    id            AS Id,
                    folio         AS Folio,
                    customer_code AS CustomerCode,
                    email         AS Email,
                    telefono    AS Phone,
                    notas       AS Notes,
                    estatus     AS Status,
                    is_approved AS IsApproved,
                    total       AS Total,
                    created_at  AS CreatedAt
                FROM request_quotation
                WHERE ({filtro} IS NULL OR estatus = {filtro})
                ORDER BY created_at DESC")
            .ToListAsync();

        if (cabeceras.Count == 0)
            return [];

        var itemRows = await _context.Database
            .SqlQuery<PreOrdenItemRow>($@"
                SELECT
                    ri.request_id     AS RequestId,
                    ri.id             AS Id,
                    ri.product_code   AS ProductCode,
                    ri.cantidad       AS Quantity,
                    ri.precio_unitario AS UnitPrice,
                    ri.importe        AS Amount
                FROM request_quotation_items ri
                JOIN request_quotation rq ON rq.id = ri.request_id
                WHERE ({filtro} IS NULL OR rq.estatus = {filtro})
                ORDER BY ri.request_id, ri.id")
            .ToListAsync();

        // Un solo golpe de stock para todos los items de todas las pre-órdenes.
        var stockRows = await ConsultarStockAsync(requestId: null, estatus: filtro);

        var itemsPorRequest = itemRows
            .GroupBy(r => r.RequestId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => new PreOrdenItemResponse
                {
                    Id = r.Id,
                    ProductCode = r.ProductCode,
                    Quantity = r.Quantity,
                    UnitPrice = r.UnitPrice,
                    Amount = r.Amount
                }).ToList());

        var resultado = new List<PreOrdenResponse>(cabeceras.Count);
        foreach (var cabecera in cabeceras)
        {
            var items = itemsPorRequest.TryGetValue(cabecera.Id, out var lista) ? lista : [];
            EnriquecerItems(cabecera.Id, items, stockRows);

            resultado.Add(new PreOrdenResponse
            {
                Id = cabecera.Id,
                Folio = cabecera.Folio,
                CustomerCode = cabecera.CustomerCode,
                Email = cabecera.Email,
                Phone = cabecera.Phone,
                Notes = cabecera.Notes,
                Status = cabecera.Status,
                IsApproved = cabecera.IsApproved,
                Total = cabecera.Total,
                CreatedAt = cabecera.CreatedAt,
                Items = items
            });
        }

        return resultado;
    }

    /// <summary>
    /// Stock entregable por almacén de venta (sales_enabled = 1) para los items de una
    /// pre-orden (<paramref name="requestId"/>) o de todas las de un estatus. Se une
    /// directamente contra los items (matcheando por código o SKU) para no depender de
    /// la expansión de listas en IN (...), que EF no realiza aquí.
    /// </summary>
    private async Task<List<StockAlmacenRow>> ConsultarStockAsync(int? requestId, string? estatus)
    {
        return await _context.Database
            .SqlQuery<StockAlmacenRow>($@"
                SELECT
                    ri.request_id   AS RequestId,
                    ri.product_code AS ProductCode,
                    w.id            AS AlmacenId,
                    w.name          AS Almacen,
                    SUM(i.deliverable_qty) AS StockDisponible
                FROM request_quotation_items ri
                JOIN request_quotation   rq ON rq.id = ri.request_id
                JOIN starnet_products    p ON (p.code = ri.product_code OR p.spec = ri.product_code)
                JOIN starnet_inventories i ON i.product_id = p.id
                JOIN starnet_warehouses  w ON w.id = i.warehouse_id
                WHERE w.sales_enabled = 1
                  AND ({requestId} IS NULL OR ri.request_id = {requestId})
                  AND ({estatus} IS NULL OR rq.estatus = {estatus})
                GROUP BY ri.request_id, ri.product_code, w.id, w.name
                HAVING SUM(i.deliverable_qty) > 0")
            .ToListAsync();
    }

    /// <summary>
    /// Completa cada item con el desglose de existencias por almacén, la cantidad
    /// cubierta, la faltante (agotada) y una sugerencia de reparto (greedy: del almacén
    /// con más stock al que menos).
    /// </summary>
    private static void EnriquecerItems(
        int requestId, List<PreOrdenItemResponse> items, List<StockAlmacenRow> stockRows)
    {
        foreach (var item in items)
        {
            var almacenes = stockRows
                .Where(r => r.RequestId == requestId && r.ProductCode == item.ProductCode)
                .GroupBy(r => new { r.AlmacenId, r.Almacen })
                .Select(g => new StockAlmacenResponse
                {
                    AlmacenId = g.Key.AlmacenId,
                    Almacen = g.Key.Almacen,
                    StockDisponible = g.Sum(x => x.StockDisponible)
                })
                .OrderByDescending(a => a.StockDisponible)
                .ToList();

            var stockTotal = almacenes.Sum(a => a.StockDisponible);
            var cubierta = Math.Min(item.Quantity, stockTotal);

            // Reparto greedy: llena desde el almacén con más stock.
            var restante = cubierta;
            foreach (var almacen in almacenes)
            {
                if (restante <= 0)
                    break;

                var surtir = Math.Min(restante, almacen.StockDisponible);
                almacen.CantidadSurtir = surtir;
                restante -= surtir;
            }

            item.StockDisponible = stockTotal;
            item.CantidadCubierta = cubierta;
            item.CantidadAgotada = item.Quantity - cubierta;
            item.Almacenes = almacenes;
            item.EstadoSurtido = CalcularEstadoSurtido(item.Quantity, stockTotal, almacenes);
        }
    }

    private static string CalcularEstadoSurtido(
        int solicitado, int stockTotal, List<StockAlmacenResponse> almacenes)
    {
        if (stockTotal <= 0)
            return "SIN_STOCK";

        if (stockTotal < solicitado)
            return "AGOTADO_PARCIAL";

        // Hay stock suficiente: ¿lo cubre un solo almacén o hay que distribuir?
        return almacenes.Any(a => a.StockDisponible >= solicitado)
            ? "CUBIERTA"
            : "DISTRIBUIR";
    }

    private sealed class PreOrdenItemRow
    {
        public int RequestId { get; set; }
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class StockAlmacenRow
    {
        public int RequestId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public int AlmacenId { get; set; }
        public string Almacen { get; set; } = string.Empty;
        public int StockDisponible { get; set; }
    }

    private sealed class PreOrdenCabeceraRow
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
    }

    private sealed class PreOrdenMetricasRow
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
    }

    private sealed class PreOrdenMetricItemRow
    {
        public int Id { get; set; }
        public int RequestId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public decimal RequestedAmount { get; set; }
        public int ConfirmedQuantity { get; set; }
        public decimal ConfirmedAmount { get; set; }
    }
}
