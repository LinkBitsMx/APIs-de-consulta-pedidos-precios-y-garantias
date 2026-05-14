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
                    CASE
                        WHEN envio.PaqueteriaRaw IS NULL THEN 'No asignada'
                        WHEN envio.PaqueteriaNormalizada IN ('PAQUETEXPRESS', 'PAQUETEEXPRESS') THEN 'PAQUETEXPRESS'
                        ELSE UPPER(envio.PaqueteriaRaw)
                    END AS Paqueteria,
                    ISNULL(ssc.caja_guia_rastreo, 'Sin guia') AS Guia,
                    CASE
                        WHEN envio.Guia IS NULL THEN NULL
                        WHEN envio.PaqueteriaNormalizada = 'DHL' THEN 'https://www.dhl.com.mx/es/es/enviador/rastrear.html?awb=' + envio.Guia
                        WHEN envio.PaqueteriaNormalizada = 'ESTAFETA' THEN 'https://www.estafeta.com.mx/seguimiento/' + envio.Guia
                        WHEN envio.PaqueteriaNormalizada = 'FEDEX' THEN 'https://tracking.fedex.com/en/tracking?shipment_id=' + envio.Guia
                        WHEN envio.PaqueteriaNormalizada IN ('PAQUETEXPRESS', 'PAQUETEEXPRESS') THEN 'https://www.paquetexpress.com.mx/rastreo/' + envio.Guia
                        WHEN envio.PaqueteriaNormalizada = 'SENDEX' THEN 'https://www.sendex.mx/Rastreo/Rastreo/' + envio.Guia
                        ELSE NULL
                    END AS TrackingUrl,
                    'activo' AS EstatusEnvio,
                    q.created_at AS FechaPedido
                FROM quotation q
                LEFT JOIN startnet_sales_caja ssc ON q.id = ssc.quote_id
                OUTER APPLY (
                    SELECT
                        NULLIF(LTRIM(RTRIM(COALESCE(ssc.paqueteria, JSON_VALUE(q.Address, '$.Paqueteria')))), '') AS PaqueteriaRaw,
                        NULLIF(LTRIM(RTRIM(ssc.caja_guia_rastreo)), '') AS Guia
                ) envioBase
                OUTER APPLY (
                    SELECT
                        envioBase.PaqueteriaRaw,
                        envioBase.Guia,
                        UPPER(REPLACE(envioBase.PaqueteriaRaw, ' ', '')) AS PaqueteriaNormalizada
                ) envio
                WHERE q.billCode = {folio}
                  AND (q.is_hide = 0 OR q.is_hide IS NULL)
                ORDER BY ssc.id DESC")
            .FirstOrDefaultAsync();
    }
}
