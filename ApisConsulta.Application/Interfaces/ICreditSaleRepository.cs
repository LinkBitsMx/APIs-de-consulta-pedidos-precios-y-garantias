using ApisConsulta.Application.CreditSales.Queries;
using ApisConsulta.Application.CreditSales.Response;

namespace ApisConsulta.Application.Interfaces;

public interface ICreditSaleRepository
{
    /// <summary>
    /// Page of credit sales with the payments applied to each one, plus the metrics of
    /// the whole filtered set.
    /// </summary>
    Task<PagedCreditSalesResponse> GetCreditSalesAsync(CreditSalesFilter filter);
}
