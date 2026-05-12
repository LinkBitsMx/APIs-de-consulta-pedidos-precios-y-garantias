using ApisConsulta.Application.Consultas.Garantias.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Garantias.Queries;

public class GetGarantiaByFolioQuery : IRequest<GarantiaResponse?>
{
    public string FolioTicket { get; set; } = string.Empty;
}

public class GetGarantiaByFolioQueryHandler : IRequestHandler<GetGarantiaByFolioQuery, GarantiaResponse?>
{
    private readonly IGarantiaRepository _repository;

    public GetGarantiaByFolioQueryHandler(IGarantiaRepository repository) => _repository = repository;

    public Task<GarantiaResponse?> Handle(GetGarantiaByFolioQuery request, CancellationToken cancellationToken)
        => _repository.GetByFolioAsync(request.FolioTicket);
}
