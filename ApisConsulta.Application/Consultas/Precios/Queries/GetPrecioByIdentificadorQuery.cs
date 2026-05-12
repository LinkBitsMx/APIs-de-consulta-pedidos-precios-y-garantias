using ApisConsulta.Application.Consultas.Precios.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Precios.Queries;

public class GetPrecioByIdentificadorQuery : IRequest<PrecioResponse?>
{
    public string Identificador { get; set; } = string.Empty;
}

public class GetPrecioByIdentificadorQueryHandler : IRequestHandler<GetPrecioByIdentificadorQuery, PrecioResponse?>
{
    private readonly IPrecioRepository _repository;

    public GetPrecioByIdentificadorQueryHandler(IPrecioRepository repository) => _repository = repository;

    public Task<PrecioResponse?> Handle(GetPrecioByIdentificadorQuery request, CancellationToken cancellationToken)
        => _repository.GetByIdentificadorAsync(request.Identificador);
}
