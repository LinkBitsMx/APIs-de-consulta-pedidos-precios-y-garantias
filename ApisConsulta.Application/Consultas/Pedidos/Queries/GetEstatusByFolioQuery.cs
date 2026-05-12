using ApisConsulta.Application.Consultas.Pedidos.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Pedidos.Queries;

public class GetEstatusByFolioQuery : IRequest<PedidoEstatusResponse?>
{
    public string Folio { get; set; } = string.Empty;
}

public class GetEstatusByFolioQueryHandler : IRequestHandler<GetEstatusByFolioQuery, PedidoEstatusResponse?>
{
    private readonly IPedidoRepository _repository;

    public GetEstatusByFolioQueryHandler(IPedidoRepository repository) => _repository = repository;

    public Task<PedidoEstatusResponse?> Handle(GetEstatusByFolioQuery request, CancellationToken cancellationToken)
        => _repository.GetEstatusByFolioAsync(request.Folio);
}
