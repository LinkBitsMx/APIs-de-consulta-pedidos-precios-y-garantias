using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Requests;
using ApisConsulta.Application.PreOrdenes.Response;
using MediatR;

namespace ApisConsulta.Application.PreOrdenes.Commands;

public class CrearPreOrdenCommand : IRequest<PreOrdenResponse>
{
    public CrearPreOrdenRequest Datos { get; set; } = new();
}

public class CrearPreOrdenCommandHandler : IRequestHandler<CrearPreOrdenCommand, PreOrdenResponse>
{
    private readonly IPreOrdenRepository _repository;

    public CrearPreOrdenCommandHandler(IPreOrdenRepository repository) => _repository = repository;

    public Task<PreOrdenResponse> Handle(CrearPreOrdenCommand request, CancellationToken cancellationToken)
        => _repository.CrearAsync(request.Datos);
}
