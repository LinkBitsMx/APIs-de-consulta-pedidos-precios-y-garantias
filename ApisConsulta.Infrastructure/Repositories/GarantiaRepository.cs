using ApisConsulta.Application.Consultas.Garantias.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

public class GarantiaRepository : IGarantiaRepository
{
    private readonly ApplicationDbContext _context;
    public GarantiaRepository(ApplicationDbContext context) => _context = context;

    public async Task<GarantiaResponse?> GetByFolioAsync(string folioTicket)
    {
        if (string.IsNullOrWhiteSpace(folioTicket))
            return null;

        return await _context.Database
            .SqlQuery<GarantiaResponse>($@"
                SELECT TOP 1
                    vchFolioTicket  AS FolioTicket,
                    productName     AS Producto,
                    dFecha          AS FechaIngreso,
                    CASE finalStatusDesc
                        WHEN N'Nota de crédito' THEN 'Nota de credito'
                        ELSE finalStatusDesc
                    END AS Resultado,
                    CASE
                        WHEN vchNombre = 'PENDIENTE'  THEN 'pendiente'
                        WHEN vchNombre = 'REVISION'   THEN 'en_revision'
                        WHEN vchNombre = 'ACTIVO'     THEN 'en_proceso'
                        WHEN vchNombre = 'FINALIZADO' THEN
                            CASE
                                WHEN finalStatusDesc = 'Reparado'        THEN 'aprobada'
                                WHEN finalStatusDesc = 'No reparado'     THEN 'rechazada'
                                WHEN finalStatusDesc = 'No aplica'       THEN 'rechazada'
                                WHEN finalStatusDesc = 'Otros'           THEN 'pendiente'
                                WHEN finalStatusDesc = N'Nota de crédito' THEN 'nota_de_credito'
                                ELSE 'pendiente'
                            END
                        ELSE 'pendiente'
                    END AS Estatus
                FROM vw_Garantias
                WHERE vchFolioTicket = {folioTicket}")
            .FirstOrDefaultAsync();
    }
}
