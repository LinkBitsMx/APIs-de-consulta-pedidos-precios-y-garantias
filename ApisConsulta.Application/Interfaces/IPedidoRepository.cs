using ApisConsulta.Application.Consultas.Pedidos.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IPedidoRepository
{
    Task<PedidoResponse?> GetByFolioAsync(string folio);
    Task<PedidoEstatusResponse?> GetEstatusByFolioAsync(string folio);
}
