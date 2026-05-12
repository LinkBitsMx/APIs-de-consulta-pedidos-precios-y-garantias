using ApisConsulta.Application.Consultas.Envios.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Envios.Queries;

public class GetEnvioByFolioQuery : IRequest<EnvioResponse?>
{
    public string Folio { get; set; } = string.Empty;
}

public class GetEnvioByFolioQueryHandler : IRequestHandler<GetEnvioByFolioQuery, EnvioResponse?>
{
    private readonly IEnvioRepository _repository;

    public GetEnvioByFolioQueryHandler(IEnvioRepository repository) => _repository = repository;

    public Task<EnvioResponse?> Handle(GetEnvioByFolioQuery request, CancellationToken cancellationToken)
        => _repository.GetByFolioAsync(request.Folio);
}
