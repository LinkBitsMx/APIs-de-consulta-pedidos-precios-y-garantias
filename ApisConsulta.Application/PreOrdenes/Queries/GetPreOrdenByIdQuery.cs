using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Response;
using MediatR;

namespace ApisConsulta.Application.PreOrdenes.Queries;

public class GetPreOrdenByIdQuery : IRequest<PreOrdenResponse?>
{
    public int Id { get; set; }
}

public class GetPreOrdenByIdQueryHandler : IRequestHandler<GetPreOrdenByIdQuery, PreOrdenResponse?>
{
    private readonly IPreOrdenRepository _repository;

    public GetPreOrdenByIdQueryHandler(IPreOrdenRepository repository) => _repository = repository;

    public Task<PreOrdenResponse?> Handle(GetPreOrdenByIdQuery request, CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id);
}
