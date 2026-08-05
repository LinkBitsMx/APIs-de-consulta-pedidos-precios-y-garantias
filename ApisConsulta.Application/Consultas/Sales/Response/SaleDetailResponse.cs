namespace ApisConsulta.Application.Consultas.Sales.Response;

/// <summary>
/// Full detail of a sale. English-facing endpoint (reviewed by the China team):
/// routes, fields and status values are in English.
/// </summary>
public class SaleDetailResponse
{
    public int SaleId { get; set; }
    public string? Folio { get; set; }
    public DateTime? Date { get; set; }
    public SaleCustomerResponse Customer { get; set; } = new();

    /// <summary>Salesperson who registered the sale (<c>quotation.usuarioId</c> → <c>catUsers</c>).</summary>
    public SaleSellerResponse? Seller { get; set; }

    /// <summary>
    /// Branch the sale belongs to (<c>starnet_branches</c>), reached through its
    /// department. Null when the sale has no department or it no longer exists.
    /// </summary>
    public SaleBranchResponse? Branch { get; set; }

    /// <summary>Department the sale belongs to (<c>departments</c>). Null when the sale has none.</summary>
    public SaleDepartmentResponse? Department { get; set; }

    public SaleStatusResponse Status { get; set; } = new();
    public SaleTotalsResponse Totals { get; set; } = new();
    public SaleInvoicingResponse Invoicing { get; set; } = new();

    /// <summary>
    /// Orders the sale was split into once the quotation was validated: one per
    /// warehouse, each with its own status. Empty while the sale is still a quotation
    /// (see <see cref="SaleStatusResponse.IsQuotation"/>).
    /// </summary>
    public List<SaleWarehouseResponse> Warehouses { get; set; } = [];

    public List<SaleItemResponse> Items { get; set; } = [];

    /// <summary>
    /// Payments registered against the sale, each with its payment form, ordered from
    /// oldest to newest. Empty when the sale has no payment registered yet.
    /// </summary>
    public List<SalePaymentResponse> Payments { get; set; } = [];
}

public class SaleSellerResponse
{
    public int SellerId { get; set; }
    public string? Name { get; set; }

    /// <summary>Seller code (<c>catUsers.code_seller</c>).</summary>
    public string? Code { get; set; }

    public string? Email { get; set; }
    public string? Username { get; set; }
}

public class SalePaymentResponse
{
    public int PaymentId { get; set; }

    /// <summary>Payment folio (<c>Payments.Folio</c>). Example: <c>PAY-0726-000010</c>.</summary>
    public string? Folio { get; set; }

    public DateTime? PaymentDate { get; set; }

    /// <summary>Amount of this payment (<c>Payments.Amount</c>).</summary>
    public decimal Amount { get; set; }

    /// <summary>SAT code of the payment form (<c>sat_FormaPago.vchCode</c>). Example: <c>03</c>.</summary>
    public string? PaymentFormCode { get; set; }

    /// <summary>Payment form name. Example: <c>Transferencia electronica de fondos</c>.</summary>
    public string? PaymentForm { get; set; }

    public int StatusId { get; set; }

    /// <summary>Payment status as stored in BambooERP (<c>catEstatus</c>, in Spanish).</summary>
    public string? StatusRaw { get; set; }

    /// <summary>VALID, REJECTED, PENDING, IN_PROCESS, CANCELLED or UNKNOWN.</summary>
    public string? Status { get; set; }

    public string? Reference { get; set; }

    /// <summary>Payment type as stored (<c>Payments.PaymentType</c>). Example: <c>payment</c>.</summary>
    public string? PaymentType { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class SaleCustomerResponse
{
    public string? CustomerCode { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Branch { get; set; }
}

public class SaleBranchResponse
{
    public int BranchId { get; set; }

    /// <summary>Branch code (<c>starnet_branches.code</c>), the value the `branchCode` filter takes. Example: `801.10.02`.</summary>
    public string? Code { get; set; }

    public string? Name { get; set; }
}

public class SaleDepartmentResponse
{
    public int DepartmentId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Zone { get; set; }
}

public class SaleStatusResponse
{
    public int StatusId { get; set; }

    /// <summary>Status name exactly as stored in BambooERP (<c>startnet_sales_orders_status</c>, in Spanish).</summary>
    public string? StatusRaw { get; set; }

    /// <summary>
    /// IN_QUOTATION, SENT_TO_CEDIS, IN_PICKING, IN_PACKING_OR_REVIEW,
    /// IN_SHIPPING_LABEL, DELIVERED, CANCELLED or UNKNOWN.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The sale is still a quotation: it has not been split into per-warehouse orders
    /// yet, so this overall status is the only one that applies. Once it turns false,
    /// the current status of each warehouse is in
    /// <see cref="SaleDetailResponse.Warehouses"/>.
    /// </summary>
    public bool IsQuotation { get; set; }

    public DateTime? StatusDate { get; set; }
}

/// <summary>
/// Sale totals. <see cref="ProductsSubtotal"/>, <see cref="ServicesTotal"/>,
/// <see cref="Units"/> and <see cref="LineDiscount"/> are computed by summing the
/// detail lines; the rest are the <c>quotation</c> columns exactly as BambooERP
/// stores them.
/// </summary>
public class SaleTotalsResponse
{
    public int Units { get; set; }
    public int TotalLines { get; set; }

    /// <summary>Sum of the product lines (<c>is_service = 0</c>).</summary>
    public decimal ProductsSubtotal { get; set; }

    /// <summary>Sum of the service lines (<c>is_service = 1</c>): shipping, freight, etc.</summary>
    public decimal ServicesTotal { get; set; }

    public decimal LineDiscount { get; set; }

    /// <summary>
    /// Sum of <c>payments[].amount</c>: what has actually been paid against the sale.
    /// It can differ from <see cref="Total"/> — a sale may be partially paid, or carry
    /// payments registered above its total.
    /// </summary>
    public decimal PaymentsTotal { get; set; }

    /// <summary>Column <c>quotation.total_deliver</c>.</summary>
    public decimal DeliveryTotal { get; set; }

    /// <summary>Column <c>quotation.total_of_assured</c>.</summary>
    public decimal AssuredTotal { get; set; }

    /// <summary>Column <c>quotation.total_fletera</c> (freight carrier).</summary>
    public decimal FreightCarrierTotal { get; set; }

    /// <summary>Column <c>quotation.total</c>: the amount the sale closes with.</summary>
    public decimal Total { get; set; }

    /// <summary>Column <c>quotation.total_initial</c>, before any modification.</summary>
    public decimal? InitialTotal { get; set; }

    public bool HasDiscount { get; set; }
}

public class SaleInvoicingResponse
{
    public bool RequiresInvoice { get; set; }
    public bool Invoiced { get; set; }
    public bool IsCredit { get; set; }
    public bool IsDirectSale { get; set; }
}

public class SaleWarehouseResponse
{
    public int WarehouseId { get; set; }
    public string? Warehouse { get; set; }
    public int StatusId { get; set; }
    public string? StatusRaw { get; set; }
    public string? Status { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int Units { get; set; }
    public int TotalLines { get; set; }
    public decimal Amount { get; set; }
}

public class SaleItemResponse
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

    /// <summary>Status of the order of the warehouse fulfilling this item (null while still a quotation).</summary>
    public string? WarehouseStatus { get; set; }

    public string? Notes { get; set; }
}
