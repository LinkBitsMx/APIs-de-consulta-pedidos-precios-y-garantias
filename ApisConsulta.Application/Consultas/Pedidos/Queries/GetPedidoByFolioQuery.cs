using ApisConsulta.Application.Consultas.Pedidos.Response;
using ApisConsulta.Application.Interfaces;
using MediatR;

namespace ApisConsulta.Application.Consultas.Pedidos.Queries;

public class GetPedidoByFolioQuery : IRequest<PedidoResponse?>
{
    public string Folio { get; set; } = string.Empty;
}

public class GetPedidoByFolioQueryHandler : IRequestHandler<GetPedidoByFolioQuery, PedidoResponse?>
{
    private readonly IPedidoRepository _repository;

    public GetPedidoByFolioQueryHandler(IPedidoRepository repository) => _repository = repository;

    public Task<PedidoResponse?> Handle(GetPedidoByFolioQuery request, CancellationToken cancellationToken)
        => _repository.GetByFolioAsync(request.Folio);
}
