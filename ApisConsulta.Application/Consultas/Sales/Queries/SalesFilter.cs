namespace ApisConsulta.Application.Consultas.Sales.Queries;

public class SalesFilter
{
    public const int MaxPageSize = 200;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CustomerCode { get; set; }
    public string? Folio { get; set; }
    /// <summary>
    /// Overall status of the sale (<c>quotation.status_id</c>). Only a handful of values
    /// ever reach this column: 1 (Sin procesar), 27 (Pago Validado), 23 (Cancelado),
    /// 28 (Pago no valido) and 29 (Pago sin proceso). The fulfillment stages such as 21
    /// (Recolectado) live on the per-warehouse orders — filter those with
    /// <see cref="WarehouseStatusId"/>.
    /// </summary>
    public int? StatusId { get; set; }

    /// <summary>
    /// Status of the per-warehouse orders: keeps the sales where at least one warehouse
    /// order is currently in this status (<c>startnet_sales_orders_status.id</c>).
    /// Example: 21 (Recolectado).
    /// </summary>
    public int? WarehouseStatusId { get; set; }

    public int? WarehouseId { get; set; }

    /// <summary>
    /// Branch code (<c>starnet_branches.code</c>). The sale reaches its branch through
    /// its department: <c>quotation.DepartamentoId</c> → <c>departments.branchId</c> →
    /// <c>starnet_branches.id</c>. Example: <c>801.10.02</c>.
    /// </summary>
    public string? BranchCode { get; set; }

    /// <summary>Only sales still in quotation (not split into per-warehouse orders yet).</summary>
    public bool? OnlyQuotations { get; set; }

    /// <summary>
    /// Include the payments of every sale in the list. Off by default: it costs one extra
    /// query per page, so it is only paid for when asked.
    /// </summary>
    public bool IncludePayments { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public SalesFilter Normalized() => new()
    {
        StartDate = StartDate,
        // EndDate is received as a day and compared against a datetime: take the whole
        // day when no time part was supplied.
        EndDate = EndDate.HasValue && EndDate.Value.TimeOfDay == TimeSpan.Zero
            ? EndDate.Value.AddDays(1).AddTicks(-1)
            : EndDate,
        CustomerCode = string.IsNullOrWhiteSpace(CustomerCode) ? null : CustomerCode.Trim(),
        Folio = string.IsNullOrWhiteSpace(Folio) ? null : Folio.Trim(),
        StatusId = StatusId,
        WarehouseStatusId = WarehouseStatusId,
        WarehouseId = WarehouseId,
        IncludePayments = IncludePayments,
        BranchCode = string.IsNullOrWhiteSpace(BranchCode) ? null : BranchCode.Trim(),
        OnlyQuotations = OnlyQuotations,
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => 50,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        }
    };
}
