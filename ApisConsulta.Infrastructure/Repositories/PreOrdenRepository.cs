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

        var ids = await _context.Database
            .SqlQuery<int>($@"
                INSERT INTO request_quotation (customer_code, email, telefono, notas, total, estatus, created_at)
                OUTPUT INSERTED.id AS [Value]
                VALUES ({request.CustomerCode}, {request.Email}, {request.Phone}, {request.Notes},
                        {total}, 'PENDIENTE', GETDATE())")
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

        var items = await _context.Database
            .SqlQuery<PreOrdenItemResponse>($@"
                SELECT
                    id              AS Id,
                    product_code    AS ProductCode,
                    cantidad        AS Quantity,
                    precio_unitario AS UnitPrice,
                    importe         AS Amount
                FROM request_quotation_items
                WHERE request_id = {id}
                ORDER BY id")
            .ToListAsync();

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
