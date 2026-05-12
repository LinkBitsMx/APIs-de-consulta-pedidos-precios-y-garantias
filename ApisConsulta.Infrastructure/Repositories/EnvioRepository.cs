using ApisConsulta.Application.Consultas.Envios.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

public class EnvioRepository : IEnvioRepository
{
    private readonly ApplicationDbContext _context;
    public EnvioRepository(ApplicationDbContext context) => _context = context;

    public async Task<EnvioResponse?> GetByFolioAsync(string folio)
    {
        if (string.IsNullOrWhiteSpace(folio))
            return null;

        return await _context.Database
            .SqlQuery<EnvioResponse>($@"
                SELECT TOP 1
                    CAST(q.id AS VARCHAR(50)) AS PedidoId,
                    ISNULL(UPPER(ssc.paqueteria), 'No asignada') AS Paqueteria,
                    ISNULL(ssc.caja_guia_rastreo, 'Sin guia') AS Guia,
                    CASE UPPER(ISNULL(ssc.paqueteria, ''))
                        WHEN 'DHL'           THEN 'https://www.dhl.com.mx/es/es/enviador/rastrear.html?awb=' + ssc.caja_guia_rastreo
                        WHEN 'ESTAFETA'      THEN 'https://www.estafeta.com.mx/seguimiento/' + ssc.caja_guia_rastreo
                        WHEN 'FEDEX'         THEN 'https://tracking.fedex.com/en/tracking?shipment_id=' + ssc.caja_guia_rastreo
                        WHEN 'PAQUETEXPRESS' THEN 'https://www.paquetexpress.com.mx/rastreo/' + ssc.caja_guia_rastreo
                        WHEN 'SENDEX'        THEN 'https://www.sendex.mx/Rastreo/Rastreo/' + ssc.caja_guia_rastreo
                        ELSE NULL
                    END AS TrackingUrl,
                    'activo' AS EstatusEnvio,
                    q.created_at AS FechaPedido
                FROM quotation q
                LEFT JOIN startnet_sales_caja ssc ON q.id = ssc.quote_id
                WHERE q.billCode = {folio}
                  AND (q.is_hide = 0 OR q.is_hide IS NULL)
                ORDER BY ssc.id DESC")
            .FirstOrDefaultAsync();
    }
}
