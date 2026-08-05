namespace ApisConsulta.Application.Consultas.Sales.Response;

public class PagedSalesResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
    public List<SaleSummaryResponse> Sales { get; set; } = [];
}

public class SaleSummaryResponse
{
    public int SaleId { get; set; }
    public string? Folio { get; set; }
    public DateTime? Date { get; set; }
    public string? CustomerCode { get; set; }
    public string? Customer { get; set; }

    /// <summary>
    /// Branch the sale belongs to, reached through its department. Null when the sale
    /// has no department or it no longer exists.
    /// </summary>
    public string? BranchCode { get; set; }

    public string? Branch { get; set; }

    /// <summary>Salesperson who registered the sale (<c>quotation.usuarioId</c>).</summary>
    public int? SellerId { get; set; }

    public string? Seller { get; set; }

    public int StatusId { get; set; }

    /// <summary>Status name exactly as stored in BambooERP (in Spanish).</summary>
    public string? StatusRaw { get; set; }

    public string? Status { get; set; }

    /// <summary>Not split into per-warehouse orders yet: only the overall status applies.</summary>
    public bool IsQuotation { get; set; }

    public int Units { get; set; }
    public int TotalLines { get; set; }
    public decimal Total { get; set; }
    public List<SaleWarehouseSummaryResponse> Warehouses { get; set; } = [];

    /// <summary>
    /// Payments of the sale. Only filled when the request asks for them with
    /// <c>includePayments=true</c>; otherwise it comes back empty (the detail endpoint
    /// always returns them).
    /// </summary>
    public List<SalePaymentResponse> Payments { get; set; } = [];

    /// <summary>Sum of <c>payments[].amount</c>. Zero unless <c>includePayments=true</c>.</summary>
    public decimal PaymentsTotal { get; set; }
}

public class SaleWarehouseSummaryResponse
{
    public int WarehouseId { get; set; }
    public string? Warehouse { get; set; }
    public int StatusId { get; set; }
    public string? StatusRaw { get; set; }
    public string? Status { get; set; }
}
