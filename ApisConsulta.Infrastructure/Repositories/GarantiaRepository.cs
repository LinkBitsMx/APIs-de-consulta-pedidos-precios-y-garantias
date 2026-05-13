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

        var productos = await _context.Database
            .SqlQuery<GarantiaProductoResponse>($@"
                SELECT
                    productName AS Producto,
                    dFecha AS FechaIngreso,
                    finalStatusDesc AS Resultado,
                    CASE
                        WHEN UPPER(LTRIM(RTRIM(vchNombre))) = 'PENDIENTE' THEN 'pendiente'
                        WHEN UPPER(LTRIM(RTRIM(vchNombre))) = 'REVISION' THEN 'revision'
                        WHEN UPPER(LTRIM(RTRIM(vchNombre))) = 'ACTIVO' THEN 'activo'
                        WHEN UPPER(LTRIM(RTRIM(vchNombre))) = 'FINALIZADO' THEN 'finalizado'
                        ELSE 'pendiente'
                    END AS Estatus
                FROM vw_Garantias
                WHERE vchFolioTicket = {folioTicket}
                ORDER BY dFecha, productName")
            .ToListAsync();

        if (productos.Count == 0)
            return null;

        return new GarantiaResponse
        {
            FolioTicket = folioTicket,
            Productos = productos
        };
    }
}
