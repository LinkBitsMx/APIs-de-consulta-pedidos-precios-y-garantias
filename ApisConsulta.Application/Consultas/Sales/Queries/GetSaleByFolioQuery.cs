using ApisConsulta.Application.Consultas.Sales.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Sales.Queries;

public class GetSaleByFolioQuery : IRequest<SaleDetailResponse?>
{
    public string Folio { get; set; } = string.Empty;
}

public class GetSaleByFolioQueryHandler : IRequestHandler<GetSaleByFolioQuery, SaleDetailResponse?>
{
    private readonly ISaleRepository _repository;

    public GetSaleByFolioQueryHandler(ISaleRepository repository) => _repository = repository;

    public Task<SaleDetailResponse?> Handle(GetSaleByFolioQuery request, CancellationToken cancellationToken)
        => _repository.GetDetailByFolioAsync(request.Folio);
}
