using ApisConsulta.Application.CreditSales.Exceptions;
using ApisConsulta.Application.CreditSales.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.CreditSales.Queries;

public class GetCreditSalesQuery : IRequest<PagedCreditSalesResponse>
{
    public CreditSalesFilter Filter { get; set; } = new();
}

public class GetCreditSalesQueryHandler
    : IRequestHandler<GetCreditSalesQuery, PagedCreditSalesResponse>
{
    private readonly ICreditSaleRepository _repository;

    public GetCreditSalesQueryHandler(ICreditSaleRepository repository)
        => _repository = repository;

    public Task<PagedCreditSalesResponse> Handle(
        GetCreditSalesQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter.Normalized();

        // A status the API does not publish is rejected instead of silently returning an
        // empty page: a typo in the filter would otherwise read as "there are none".
        var unknown = filter.StatusValues()
            .Where(s => !CreditSalesFilter.Statuses.Contains(s))
            .ToList();

        if (unknown.Count > 0)
            throw new CreditSaleValidationException(
                $"Unknown status: {string.Join(", ", unknown)}. " +
                $"status must be one of: {string.Join(", ", CreditSalesFilter.Statuses)}.");

        return _repository.GetCreditSalesAsync(filter);
    }
}
