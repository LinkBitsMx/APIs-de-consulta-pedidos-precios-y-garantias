using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Response;
using MediatR;

namespace ApisConsulta.Application.PreOrdenes.Queries;

public class GetPreOrdenMetricasQuery : IRequest<IReadOnlyList<PreOrdenMetricasResponse>>
{
    /// <summary>Filtro opcional por estatus (PENDIENTE, TOMADA, CONVERTIDA, CANCELADA).</summary>
    public string? Status { get; set; }
}

public class GetPreOrdenMetricasQueryHandler
    : IRequestHandler<GetPreOrdenMetricasQuery, IReadOnlyList<PreOrdenMetricasResponse>>
{
    private readonly IPreOrdenRepository _repository;

    public GetPreOrdenMetricasQueryHandler(IPreOrdenRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PreOrdenMetricasResponse>> Handle(
        GetPreOrdenMetricasQuery request, CancellationToken cancellationToken)
        => _repository.GetMetricasAsync(request.Status);
}