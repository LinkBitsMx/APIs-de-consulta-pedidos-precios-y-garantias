using ApisConsulta.Application.Interfaces;
using ApisConsulta.Application.PreOrdenes.Exceptions;
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

    public async Task<PreOrdenResponse> Handle(CrearPreOrdenCommand request, CancellationToken cancellationToken)
    {
        var existeCustomer = await _repository.ExisteCustomerAsync(request.Datos.CustomerCode);
        if (!existeCustomer)
            throw new CustomerNoEncontradoException(request.Datos.CustomerCode);

        return await _repository.CrearAsync(request.Datos);
    }
}
