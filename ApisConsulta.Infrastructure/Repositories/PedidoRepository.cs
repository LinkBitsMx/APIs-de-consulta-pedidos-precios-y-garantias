using ApisConsulta.Application.Consultas.Pedidos.Response;
using ApisConsulta.Application.Interfaces;
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
            Estatus  = MapearEstatus(raw.EstatusRaw),
            FechaEstatus = raw.FechaEstatus
        };
    }

    private static string MapearEstatus(string? nombre)
    {
        return nombre?.ToLower() switch
        {
            "cotizacion"              => "En proceso de cotizacion",

            "pago sin proceso"        => "Se mando a surtir al CEDIS",
            "pago no valido"          => "Se mando a surtir al CEDIS",
            "pago validado"           => "Se mando a surtir al CEDIS",

            "sin procesar"            => "En proceso de surtido",
            "surtido en proceso"      => "En proceso de surtido",
            "productos por llegar"    => "En proceso de surtido",
            "agotados"                => "En proceso de surtido",
            "agotados y por llegar"   => "En proceso de surtido",
            "pendiente por cerrar"    => "En proceso de surtido",
            "surtido"                 => "En proceso de surtido",
            "surtido sin proceso"     => "En proceso de surtido",
            "sin enviar"              => "En proceso de surtido",
            "enviado"                 => "En proceso de surtido",
            "recibido"                => "En proceso de surtido",

            "empacado sin procesar"   => "En proceso de empacado o revision",
            "empacado incidencia"     => "En proceso de empacado o revision",
            "empacado en proceso"     => "En proceso de empacado o revision",
            "empacado finalizado"     => "En proceso de empacado o revision",
            "validar sin proceso"     => "En proceso de empacado o revision",
            "validar incidencia"      => "En proceso de empacado o revision",
            "validar proceso"         => "En proceso de empacado o revision",
            "validado por seguridad"  => "En proceso de empacado o revision",
            "orden local"             => "En proceso de empacado o revision",

            "guía sin proceso"        => "En proceso de guias de envio",
            "guia sin proceso"        => "En proceso de guias de envio",
            "guia en proceso"         => "En proceso de guias de envio",
            "guia generada"           => "En proceso de guias de envio",
            "pegar guia en proceso"   => "En proceso de guias de envio",
            "guia pegada"             => "En proceso de guias de envio",

            "recolectado"             => "Entregado",
            "recolectado local"       => "Entregado",
            "cobrado local"           => "Entregado",

            "cancelado"               => "Cancelado",

            _ => "Desconocido"
        };
    }

    private class PedidoEstatusRaw
    {
        public int PedidoId { get; set; }
        public string? EstatusRaw { get; set; }
        public DateTime? FechaEstatus { get; set; }
    }
}
