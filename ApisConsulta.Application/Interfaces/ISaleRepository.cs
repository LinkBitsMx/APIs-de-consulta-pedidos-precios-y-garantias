using ApisConsulta.Application.Consultas.Sales.Queries;
using ApisConsulta.Application.Consultas.Sales.Response;

namespace ApisConsulta.Application.Interfaces;

public interface ISaleRepository
{
    Task<PagedSalesResponse> GetSalesAsync(SalesFilter filter);
    Task<SaleDetailResponse?> GetDetailByFolioAsync(string folio);
}
