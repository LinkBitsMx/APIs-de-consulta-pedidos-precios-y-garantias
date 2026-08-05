using ApisConsulta.Application.Consultas.Pedidos.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Mapping;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly ApplicationDbContext _context;
    public PedidoRepository(ApplicationDbContext context) => _context = context;

    public async Task<PedidoResponse?> GetByFolioAsync(string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return null;

        return await _context.Database
            .SqlQuery<PedidoResponse>($@"
                SELECT TOP 1
                    q.id         AS PedidoId,
                    q.billCode   AS Folio,
                    c.name       AS Cliente,
                    q.created_at AS Fecha,
                    q.total      AS Total,
                    e.vchNombre  AS Estatus
                FROM quotation q
                LEFT JOIN customers c ON c.customer_id = q.customer_id
                LEFT JOIN catEstatus e ON e.idEstatus = q.status_id
                WHERE q.billCode = {folio}
                  AND (q.is_hide = 0 OR q.is_hide IS NULL)")
            .FirstOrDefaultAsync();
    }

    public async Task<PedidoEstatusResponse?> GetEstatusByFolioAsync(string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return null;

        var raw = await _context.Database
            .SqlQuery<PedidoEstatusRaw>($@"
                SELECT TOP 1
                    q.id         AS PedidoId,
                    ISNULL(s.name, '') AS EstatusRaw,
                    q.updated_at AS FechaEstatus
                FROM quotation q
                LEFT JOIN startnet_sales_orders_status s ON s.id = q.status_id
                WHERE q.billCode = {folio}
                  AND (q.is_hide = 0 OR q.is_hide IS NULL)")
            .FirstOrDefaultAsync();

        if (raw == null)
            return null;

        return new PedidoEstatusResponse
        {
            PedidoId = raw.PedidoId,
            Estatus  = EstatusPedidoMapper.Mapear(raw.EstatusRaw),
            FechaEstatus = raw.FechaEstatus
        };
    }

    private class PedidoEstatusRaw
    {
        public int PedidoId { get; set; }
        public string? EstatusRaw { get; set; }
        public DateTime? FechaEstatus { get; set; }
    }
}
