using ApisConsulta.Application.Consultas.Sales.Queries;
using ApisConsulta.Application.Consultas.Sales.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Mapping;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

/// <summary>
/// Sales queries against BambooERP.
///
/// The actual model is: <c>quotation</c> is the sale (header and overall status) and
/// <c>quotation_detail</c> its detail, where every line holds the warehouse fulfilling
/// it. While the sale is a quotation that overall status is the only one there is; once
/// it is validated the order is split and each warehouse keeps its own status in
/// <c>startnet_sales_orders_picking_assignment</c> (one row per order and warehouse).
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly ApplicationDbContext _context;
    public SaleRepository(ApplicationDbContext context) => _context = context;

    public async Task<PagedSalesResponse> GetSalesAsync(SalesFilter filter)
    {
        var startDate = filter.StartDate;
        var endDate = filter.EndDate;
        var customerCode = filter.CustomerCode;
        var folio = filter.Folio;
        var statusId = filter.StatusId;
        var warehouseId = filter.WarehouseId;
        var branchCode = filter.BranchCode;
        var warehouseStatusId = filter.WarehouseStatusId;
        var onlyQuotations = filter.OnlyQuotations;
        var offset = (filter.Page - 1) * filter.PageSize;
        var pageSize = filter.PageSize;

        var total = await _context.Database
            .SqlQuery<int>($@"
                SELECT COUNT(1) AS [Value]
                FROM quotation q
                LEFT JOIN customers c ON c.customer_id = q.customer_id
                LEFT JOIN departments dep ON dep.id = q.DepartamentoId
                LEFT JOIN starnet_branches br ON br.id = dep.branchId
                WHERE (q.is_hide = 0 OR q.is_hide IS NULL)
                  AND ({startDate} IS NULL OR q.created_at >= {startDate})
                  AND ({endDate} IS NULL OR q.created_at <= {endDate})
                  AND ({customerCode} IS NULL OR c.customer_code = {customerCode})
                  AND ({folio} IS NULL OR q.billCode LIKE '%' + {folio} + '%')
                  AND ({statusId} IS NULL OR q.status_id = {statusId})
                  AND ({warehouseId} IS NULL OR EXISTS (
                        SELECT 1 FROM quotation_detail d
                        WHERE d.quote_id = q.id AND d.warehouse_id = {warehouseId}))
                  AND ({branchCode} IS NULL OR br.code = {branchCode})
                  AND ({warehouseStatusId} IS NULL OR EXISTS (
                        SELECT 1 FROM (
                            SELECT pa.status_id,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY pa.order_id, pa.warehouseId
                                       ORDER BY pa.id DESC) AS rn
                            FROM startnet_sales_orders_picking_assignment pa
                            WHERE pa.order_id = q.id
                        ) cur
                        WHERE cur.rn = 1 AND cur.status_id = {warehouseStatusId}))
                  AND ({onlyQuotations} IS NULL OR {onlyQuotations} = 0 OR NOT EXISTS (
                        SELECT 1 FROM startnet_sales_orders_picking_assignment pa
                        WHERE pa.order_id = q.id))")
            .FirstOrDefaultAsync();

        if (total == 0)
        {
            return new PagedSalesResponse
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = 0,
                TotalPages = 0
            };
        }

        var rows = await _context.Database
            .SqlQuery<SaleSummaryRow>($@"
                SELECT
                    q.id                        AS SaleId,
                    q.billCode                  AS Folio,
                    q.created_at                AS Date,
                    c.customer_code             AS CustomerCode,
                    c.name                      AS Customer,
                    br.code                     AS BranchCode,
                    br.name                     AS Branch,
                    q.usuarioId                 AS SellerId,
                    LTRIM(RTRIM(ISNULL(u.vchNombre, '') + ' ' + ISNULL(u.vchApellidos, ''))) AS Seller,
                    ISNULL(q.status_id, 0)      AS StatusId,
                    ISNULL(s.name, '')          AS StatusRaw,
                    ISNULL(q.quantity, 0)       AS Units,
                    ISNULL(q.total, 0)          AS Total,
                    (SELECT COUNT(1) FROM quotation_detail d WHERE d.quote_id = q.id) AS TotalLines
                FROM quotation q
                LEFT JOIN customers c ON c.customer_id = q.customer_id
                LEFT JOIN departments dep ON dep.id = q.DepartamentoId
                LEFT JOIN starnet_branches br ON br.id = dep.branchId
                LEFT JOIN catUsers u ON u.idUsuario = q.usuarioId
                LEFT JOIN startnet_sales_orders_status s ON s.id = q.status_id
                WHERE (q.is_hide = 0 OR q.is_hide IS NULL)
                  AND ({startDate} IS NULL OR q.created_at >= {startDate})
                  AND ({endDate} IS NULL OR q.created_at <= {endDate})
                  AND ({customerCode} IS NULL OR c.customer_code = {customerCode})
                  AND ({folio} IS NULL OR q.billCode LIKE '%' + {folio} + '%')
                  AND ({statusId} IS NULL OR q.status_id = {statusId})
                  AND ({warehouseId} IS NULL OR EXISTS (
                        SELECT 1 FROM quotation_detail d
                        WHERE d.quote_id = q.id AND d.warehouse_id = {warehouseId}))
                  AND ({branchCode} IS NULL OR br.code = {branchCode})
                  AND ({warehouseStatusId} IS NULL OR EXISTS (
                        SELECT 1 FROM (
                            SELECT pa.status_id,
                                   ROW_NUMBER() OVER (
                                       PARTITION BY pa.order_id, pa.warehouseId
                                       ORDER BY pa.id DESC) AS rn
                            FROM startnet_sales_orders_picking_assignment pa
                            WHERE pa.order_id = q.id
                        ) cur
                        WHERE cur.rn = 1 AND cur.status_id = {warehouseStatusId}))
                  AND ({onlyQuotations} IS NULL OR {onlyQuotations} = 0 OR NOT EXISTS (
                        SELECT 1 FROM startnet_sales_orders_picking_assignment pa
                        WHERE pa.order_id = q.id))
                ORDER BY q.id DESC
                OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY")
            .ToListAsync();

        var warehouses = await GetWarehouseStatusesAsync(rows.Select(r => r.SaleId));
        var payments = filter.IncludePayments
            ? await GetPaymentsAsync(rows.Select(r => r.SaleId))
            : [];

        var sales = rows
            .Select(r =>
            {
                var own = warehouses.Where(w => w.SaleId == r.SaleId).ToList();

                return new SaleSummaryResponse
                {
                    SaleId = r.SaleId,
                    Folio = r.Folio,
                    Date = r.Date,
                    CustomerCode = r.CustomerCode,
                    Customer = r.Customer,
                    BranchCode = r.BranchCode,
                    Branch = r.Branch,
                    SellerId = r.SellerId,
                    Seller = string.IsNullOrWhiteSpace(r.Seller) ? null : r.Seller,
                    StatusId = r.StatusId,
                    StatusRaw = r.StatusRaw,
                    Status = SaleStatusMapper.Map(r.StatusRaw),
                    IsQuotation = own.Count == 0,
                    Units = r.Units,
                    TotalLines = r.TotalLines,
                    Total = r.Total,
                    Payments = payments.TryGetValue(r.SaleId, out var own_payments)
                        ? own_payments
                        : [],
                    PaymentsTotal = payments.TryGetValue(r.SaleId, out var sale_payments)
                        ? sale_payments.Sum(x => x.Amount)
                        : 0,
                    Warehouses = own
                        .OrderBy(w => w.Warehouse)
                        .Select(w => new SaleWarehouseSummaryResponse
                        {
                            WarehouseId = w.WarehouseId,
                            Warehouse = w.Warehouse,
                            StatusId = w.StatusId,
                            StatusRaw = w.StatusRaw,
                            Status = SaleStatusMapper.Map(w.StatusRaw)
                        })
                        .ToList()
                };
            })
            .ToList();

        return new PagedSalesResponse
        {
            Page = filter.Page,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Sales = sales
        };
    }

    public async Task<SaleDetailResponse?> GetDetailByFolioAsync(string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return null;

        var header = await _context.Database
            .SqlQuery<SaleHeaderRow>($@"
                SELECT TOP 1
                    q.id                                AS SaleId,
                    q.billCode                          AS Folio,
                    q.created_at                        AS Date,
                    q.updated_at                        AS StatusDate,
                    c.customer_code                     AS CustomerCode,
                    c.name                              AS Customer,
                    c.email                             AS Email,
                    c.phone                             AS Phone,
                    c.branchName                        AS Branch,
                    q.DepartamentoId                    AS DepartmentId,
                    dep.code                            AS DepartmentCode,
                    dep.name                            AS DepartmentName,
                    dep.departmentZone                  AS DepartmentZone,
                    dep.branchId                        AS BranchId,
                    br.code                             AS BranchCode,
                    br.name                             AS BranchName,
                    q.usuarioId                         AS SellerId,
                    LTRIM(RTRIM(ISNULL(u.vchNombre, '') + ' ' + ISNULL(u.vchApellidos, ''))) AS SellerName,
                    u.code_seller                       AS SellerCode,
                    u.vchCorreo                         AS SellerEmail,
                    u.vchUsuario                        AS SellerUsername,
                    ISNULL(q.status_id, 0)              AS StatusId,
                    ISNULL(s.name, '')                  AS StatusRaw,
                    ISNULL(q.quantity, 0)               AS Units,
                    ISNULL(q.total, 0)                  AS Total,
                    ISNULL(q.total_deliver, 0)          AS DeliveryTotal,
                    ISNULL(q.total_of_assured, 0)       AS AssuredTotal,
                    ISNULL(q.total_fletera, 0)          AS FreightCarrierTotal,
                    q.total_initial                     AS InitialTotal,
                    ISNULL(q.IsDiscount, 0)             AS HasDiscount,
                    ISNULL(q.requires_invoice, 0)       AS RequiresInvoice,
                    ISNULL(q.invoiced, 0)               AS Invoiced,
                    ISNULL(q.isCredit, 0)               AS IsCredit,
                    ISNULL(q.is_direct_sale, 0)         AS IsDirectSale
                FROM quotation q
                LEFT JOIN customers c ON c.customer_id = q.customer_id
                LEFT JOIN departments dep ON dep.id = q.DepartamentoId
                LEFT JOIN starnet_branches br ON br.id = dep.branchId
                LEFT JOIN catUsers u ON u.idUsuario = q.usuarioId
                LEFT JOIN startnet_sales_orders_status s ON s.id = q.status_id
                WHERE q.billCode = {folio}
                  AND (q.is_hide = 0 OR q.is_hide IS NULL)")
            .FirstOrDefaultAsync();

        if (header == null)
            return null;

        var saleId = header.SaleId;

        // starnet_products holds one row per branch for the same code, hence the OUTER
        // APPLY with TOP 1: a LEFT JOIN would multiply the detail lines.
        var itemRows = await _context.Database
            .SqlQuery<SaleItemRow>($@"
                SELECT
                    qd.id                           AS ItemId,
                    qd.CodeProduct                  AS ProductCode,
                    prod.spec                       AS Sku,
                    prod.name                       AS Product,
                    ISNULL(qd.quantity, 0)          AS Quantity,
                    ISNULL(qd.price, 0)             AS UnitPrice,
                    ISNULL(qd.Discount, 0)          AS Discount,
                    ISNULL(qd.quantity, 0) * ISNULL(qd.price, 0) AS Amount,
                    CAST(ISNULL(qd.is_service, 0) AS BIT)        AS IsService,
                    NULLIF(qd.warehouse_id, 0)      AS WarehouseId,
                    w.name                          AS Warehouse,
                    qd.Notes                        AS Notes
                FROM quotation_detail qd
                OUTER APPLY (
                    SELECT TOP 1 p.name, p.spec
                    FROM starnet_products p
                    WHERE p.code = qd.CodeProduct
                ) prod
                LEFT JOIN starnet_warehouses w ON w.id = qd.warehouse_id
                WHERE qd.quote_id = {saleId}
                ORDER BY qd.id")
            .ToListAsync();

        var warehouseStatuses = await GetWarehouseStatusesAsync([saleId]);
        var payments = await GetPaymentsAsync(saleId);

        var items = itemRows
            .Select(r => new SaleItemResponse
            {
                ItemId = r.ItemId,
                ProductCode = r.ProductCode,
                Sku = r.Sku,
                Product = r.Product,
                Quantity = r.Quantity,
                UnitPrice = r.UnitPrice,
                Discount = r.Discount,
                Amount = r.Amount,
                IsService = r.IsService,
                WarehouseId = r.WarehouseId,
                Warehouse = r.Warehouse,
                Notes = r.Notes
            })
            .ToList();

        var warehouses = BuildWarehouses(items, warehouseStatuses);
        var statusByWarehouse = warehouses.ToDictionary(w => w.WarehouseId, w => w.Status);

        foreach (var item in items.Where(i => i.WarehouseId.HasValue))
        {
            if (statusByWarehouse.TryGetValue(item.WarehouseId!.Value, out var status))
                item.WarehouseStatus = status;
        }

        var products = items.Where(i => !i.IsService).ToList();

        return new SaleDetailResponse
        {
            SaleId = header.SaleId,
            Folio = header.Folio,
            Date = header.Date,
            Customer = new SaleCustomerResponse
            {
                CustomerCode = header.CustomerCode,
                Name = header.Customer,
                Email = header.Email,
                Phone = header.Phone,
                Branch = header.Branch
            },
            Seller = header.SellerId.HasValue
                ? new SaleSellerResponse
                {
                    SellerId = header.SellerId.Value,
                    Name = string.IsNullOrWhiteSpace(header.SellerName) ? null : header.SellerName,
                    Code = header.SellerCode,
                    Email = header.SellerEmail,
                    Username = header.SellerUsername
                }
                : null,
            Branch = header.BranchId.HasValue && header.BranchName != null
                ? new SaleBranchResponse
                {
                    BranchId = header.BranchId.Value,
                    Code = header.BranchCode,
                    Name = header.BranchName
                }
                : null,
            // A handful of sales point to a department id that no longer exists in
            // `departments`, so the id is published only when the join resolved.
            Department = header.DepartmentId.HasValue && header.DepartmentName != null
                ? new SaleDepartmentResponse
                {
                    DepartmentId = header.DepartmentId.Value,
                    Code = header.DepartmentCode,
                    Name = header.DepartmentName,
                    Zone = header.DepartmentZone
                }
                : null,
            Status = new SaleStatusResponse
            {
                StatusId = header.StatusId,
                StatusRaw = header.StatusRaw,
                Status = SaleStatusMapper.Map(header.StatusRaw),
                IsQuotation = warehouseStatuses.Count == 0,
                StatusDate = header.StatusDate
            },
            Totals = new SaleTotalsResponse
            {
                Units = products.Sum(i => i.Quantity),
                TotalLines = items.Count,
                ProductsSubtotal = products.Sum(i => i.Amount),
                ServicesTotal = items.Where(i => i.IsService).Sum(i => i.Amount),
                LineDiscount = items.Sum(i => i.Discount),
                PaymentsTotal = payments.Sum(p => p.Amount),
                DeliveryTotal = header.DeliveryTotal,
                AssuredTotal = header.AssuredTotal,
                FreightCarrierTotal = header.FreightCarrierTotal,
                Total = header.Total,
                InitialTotal = header.InitialTotal,
                HasDiscount = header.HasDiscount
            },
            Invoicing = new SaleInvoicingResponse
            {
                RequiresInvoice = header.RequiresInvoice,
                Invoiced = header.Invoiced,
                IsCredit = header.IsCredit,
                IsDirectSale = header.IsDirectSale
            },
            Warehouses = warehouses,
            Items = items,
            Payments = payments
        };
    }

    /// <summary>
    /// Payments registered against the sale. The relation carries the payment in
    /// <c>VoucherId</c> (not in <c>payment_id</c>), so that is the column joined against
    /// <c>Payments.Id</c>; rows with <c>VoucherId = 0</c> are placeholders without a
    /// payment and are skipped.
    /// </summary>
    private async Task<List<SalePaymentResponse>> GetPaymentsAsync(int saleId)
    {
        var bySale = await GetPaymentsAsync([saleId]);
        return bySale.TryGetValue(saleId, out var payments) ? payments : [];
    }

    /// <summary>
    /// Same as the single-sale overload, but resolves the payments of a whole page in one
    /// query, keyed by sale id.
    /// </summary>
    private async Task<Dictionary<int, List<SalePaymentResponse>>> GetPaymentsAsync(
        IEnumerable<int> saleIds)
    {
        var ids = string.Join(",", saleIds.Distinct());
        if (ids.Length == 0)
            return [];

        var rows = await _context.Database
            .SqlQuery<SalePaymentRow>($@"
                SELECT
                    r.Quote_id              AS SaleId,
                    p.Id                    AS PaymentId,
                    p.Folio                 AS Folio,
                    p.PaymentDate           AS PaymentDate,
                    ISNULL(p.Amount, 0)     AS Amount,
                    f.vchCode               AS PaymentFormCode,
                    ISNULL(f.UILabel, f.vchFPago) AS PaymentForm,
                    ISNULL(p.StatusId, 0)   AS StatusId,
                    e.vchNombre             AS StatusRaw,
                    p.Reference             AS Reference,
                    p.PaymentType           AS PaymentType,
                    ISNULL(r.CreatedAt, p.CreatedAt) AS CreatedAt
                FROM rel_quotes_to_payments r
                JOIN Payments p ON p.Id = r.VoucherId
                LEFT JOIN sat_FormaPago f ON f.ID = p.PaymentFormId
                LEFT JOIN catEstatus e ON e.idEstatus = p.StatusId
                JOIN STRING_SPLIT({ids}, ',') sp ON sp.value = r.Quote_id
                WHERE r.VoucherId > 0
                ORDER BY r.Quote_id, p.PaymentDate, p.Id")
            .ToListAsync();

        return rows
            .GroupBy(r => r.SaleId)
            .ToDictionary(g => g.Key, g => g.Select(r => new SalePaymentResponse
            {
                PaymentId = r.PaymentId,
                Folio = r.Folio,
                PaymentDate = r.PaymentDate,
                Amount = r.Amount,
                PaymentFormCode = r.PaymentFormCode?.Trim(),
                // vchFPago carries a trailing line break in the catalog.
                PaymentForm = r.PaymentForm?.Trim(),
                StatusId = r.StatusId,
                StatusRaw = r.StatusRaw,
                Status = PaymentStatusMapper.Map(r.StatusRaw),
                Reference = string.IsNullOrWhiteSpace(r.Reference) ? null : r.Reference.Trim(),
                PaymentType = r.PaymentType,
                CreatedAt = r.CreatedAt
            }).ToList());
    }

    /// <summary>
    /// Current status of every per-warehouse order. It keeps the latest
    /// <c>startnet_sales_orders_picking_assignment</c> row per (sale, warehouse), which
    /// is the one reflecting the current fulfillment stage.
    /// </summary>
    private async Task<List<SaleWarehouseRow>> GetWarehouseStatusesAsync(IEnumerable<int> saleIds)
    {
        var ids = string.Join(",", saleIds.Distinct());
        if (ids.Length == 0)
            return [];

        return await _context.Database
            .SqlQuery<SaleWarehouseRow>($@"
                SELECT
                    last.order_id            AS SaleId,
                    last.warehouseId         AS WarehouseId,
                    ISNULL(w.name, '')       AS Warehouse,
                    ISNULL(last.status_id, 0) AS StatusId,
                    ISNULL(s.name, '')       AS StatusRaw,
                    last.created_asig        AS AssignedAt
                FROM (
                    SELECT
                        pa.order_id, pa.warehouseId, pa.status_id, pa.created_asig,
                        ROW_NUMBER() OVER (
                            PARTITION BY pa.order_id, pa.warehouseId
                            ORDER BY pa.id DESC) AS rn
                    FROM startnet_sales_orders_picking_assignment pa
                    JOIN STRING_SPLIT({ids}, ',') sp ON sp.value = pa.order_id
                ) last
                LEFT JOIN starnet_warehouses w ON w.id = last.warehouseId
                LEFT JOIN startnet_sales_orders_status s ON s.id = last.status_id
                WHERE last.rn = 1")
            .ToListAsync();
    }

    /// <summary>
    /// Combines the status of each per-warehouse order with the amounts of the lines it
    /// fulfills. Both sides are included: a warehouse may have an order without lines of
    /// its own (or lines without an order yet).
    /// </summary>
    private static List<SaleWarehouseResponse> BuildWarehouses(
        List<SaleItemResponse> items, List<SaleWarehouseRow> warehouseStatuses)
    {
        var byWarehouse = items
            .Where(i => i.WarehouseId.HasValue)
            .GroupBy(i => i.WarehouseId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var warehouseIds = byWarehouse.Keys
            .Union(warehouseStatuses.Select(w => w.WarehouseId))
            .Where(id => id > 0)
            .Distinct();

        return warehouseIds
            .Select(id =>
            {
                var status = warehouseStatuses.FirstOrDefault(w => w.WarehouseId == id);
                var lines = byWarehouse.TryGetValue(id, out var list) ? list : [];

                return new SaleWarehouseResponse
                {
                    WarehouseId = id,
                    Warehouse = status?.Warehouse is { Length: > 0 } name
                        ? name
                        : lines.FirstOrDefault()?.Warehouse,
                    StatusId = status?.StatusId ?? 0,
                    StatusRaw = status?.StatusRaw,
                    Status = status == null ? null : SaleStatusMapper.Map(status.StatusRaw),
                    AssignedAt = status?.AssignedAt,
                    Units = lines.Sum(i => i.Quantity),
                    TotalLines = lines.Count,
                    Amount = lines.Sum(i => i.Amount)
                };
            })
            .OrderBy(w => w.Warehouse)
            .ToList();
    }

    private sealed class SaleSummaryRow
    {
        public int SaleId { get; set; }
        public string? Folio { get; set; }
        public DateTime? Date { get; set; }
        public string? CustomerCode { get; set; }
        public string? Customer { get; set; }
        public string? BranchCode { get; set; }
        public string? Branch { get; set; }
        public int? SellerId { get; set; }
        public string? Seller { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public int Units { get; set; }
        public int TotalLines { get; set; }
        public decimal Total { get; set; }
    }

    private sealed class SaleHeaderRow
    {
        public int SaleId { get; set; }
        public string? Folio { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? StatusDate { get; set; }
        public string? CustomerCode { get; set; }
        public string? Customer { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Branch { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentZone { get; set; }
        public int? BranchId { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public int? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? SellerCode { get; set; }
        public string? SellerEmail { get; set; }
        public string? SellerUsername { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public int Units { get; set; }
        public decimal Total { get; set; }
        public decimal DeliveryTotal { get; set; }
        public decimal AssuredTotal { get; set; }
        public decimal FreightCarrierTotal { get; set; }
        public decimal? InitialTotal { get; set; }
        public bool HasDiscount { get; set; }
        public bool RequiresInvoice { get; set; }
        public bool Invoiced { get; set; }
        public bool IsCredit { get; set; }
        public bool IsDirectSale { get; set; }
    }

    private sealed class SaleItemRow
    {
        public int ItemId { get; set; }
        public string? ProductCode { get; set; }
        public string? Sku { get; set; }
        public string? Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
        public bool IsService { get; set; }
        public int? WarehouseId { get; set; }
        public string? Warehouse { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class SalePaymentRow
    {
        public int SaleId { get; set; }
        public int PaymentId { get; set; }
        public string? Folio { get; set; }
        public DateTime? PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentFormCode { get; set; }
        public string? PaymentForm { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public string? Reference { get; set; }
        public string? PaymentType { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    private sealed class SaleWarehouseRow
    {
        public int SaleId { get; set; }
        public int WarehouseId { get; set; }
        public string? Warehouse { get; set; }
        public int StatusId { get; set; }
        public string? StatusRaw { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
}
