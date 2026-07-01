using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Response;
using MediatR;

namespace ApisConsulta.Application.PreOrdenes.Queries;

public class GetPreOrdenesQuery : IRequest<IReadOnlyList<PreOrdenResumenResponse>>
{
    /// <summary>Filtro opcional por estatus (PENDIENTE, TOMADA, CONVERTIDA, CANCELADA).</summary>
    public string? Status { get; set; }
}

public class GetPreOrdenesQueryHandler
    : IRequestHandler<GetPreOrdenesQuery, IReadOnlyList<PreOrdenResumenResponse>>
{
    private readonly IPreOrdenRepository _repository;

    public GetPreOrdenesQueryHandler(IPreOrdenRepository repository) => _repository = repository;

    public Task<IReadOnlyList<PreOrdenResumenResponse>> Handle(
        GetPreOrdenesQuery request, CancellationToken cancellationToken)
        => _repository.GetAllAsync(request.Status);
}
