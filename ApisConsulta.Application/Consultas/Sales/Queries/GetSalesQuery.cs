using ApisConsulta.Application.Consultas.Sales.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Sales.Queries;

public class GetSalesQuery : IRequest<PagedSalesResponse>
{
    public SalesFilter Filter { get; set; } = new();
}

public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, PagedSalesResponse>
{
    private readonly ISaleRepository _repository;

    public GetSalesQueryHandler(ISaleRepository repository) => _repository = repository;

    public Task<PagedSalesResponse> Handle(GetSalesQuery request, CancellationToken cancellationToken)
        => _repository.GetSalesAsync(request.Filter.Normalized());
}
