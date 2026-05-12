using ApisConsulta.Application.Consultas.Precios.Response;
using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApisConsulta.Infrastructure.Repositories;

public class PrecioRepository : IPrecioRepository
{
    private readonly ApplicationDbContext _context;
    public PrecioRepository(ApplicationDbContext context) => _context = context;

    public async Task<PrecioResponse?> GetByIdentificadorAsync(string identificador)
    {
        if (string.IsNullOrWhiteSpace(identificador))
            return null;

        return await _context.Database
            .SqlQuery<PrecioResponse>($@"
                SELECT TOP 1
                    CONVERT(VARCHAR(50), id) AS ProductoId,
                    code            AS Codigo,
                    name            AS Nombre,
                    spec            AS Sku,
                    ISNULL(wholesale_price, 0) AS PrecioMayoreo,
                    ISNULL(price4, 0)          AS PrecioCaja,
                    'MXN'           AS Moneda,
                    CAST(1 AS BIT)  AS IncluyeIva
                FROM starnet_products
                WHERE disabled = 0
                  AND (code = {identificador} OR spec = {identificador})")
            .FirstOrDefaultAsync();
    }
}
