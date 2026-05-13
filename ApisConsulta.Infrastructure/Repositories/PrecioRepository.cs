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

        var productos = await _context.Database
            .SqlQuery<PrecioProductoRaw>($@"
                SELECT TOP 1
                    CONVERT(VARCHAR(50), id) AS ProductoId,
                    code            AS Codigo,
                    name            AS Nombre,
                    spec            AS Sku
                FROM starnet_products
                WHERE disabled = 0
                  AND (code = {identificador} OR spec = {identificador})
                  AND branch_name IN (N'México', N'Sucursal Monterrey')
                ORDER BY
                    CASE branch_name
                        WHEN N'México' THEN 1
                        WHEN N'Sucursal Monterrey' THEN 2
                        ELSE 3
                    END")
            .ToListAsync();

        if (productos.Count == 0)
            return null;

        var precios = await _context.Database
            .SqlQuery<PrecioSucursalResponse>($@"
                SELECT
                    branch_name AS Sucursal,
                    ISNULL(wholesale_price, 0) AS PrecioMayoreo,
                    ISNULL(price4, 0)          AS PrecioCaja,
                    'MXN'           AS Moneda,
                    CAST(1 AS BIT)  AS IncluyeIva
                FROM starnet_products
                WHERE disabled = 0
                  AND (code = {identificador} OR spec = {identificador})
                  AND branch_name IN (N'México', N'Sucursal Monterrey')
                ORDER BY
                    CASE branch_name
                        WHEN N'México' THEN 1
                        WHEN N'Sucursal Monterrey' THEN 2
                        ELSE 3
                    END")
            .ToListAsync();

        var producto = productos[0];

        return new PrecioResponse
        {
            ProductoId = producto.ProductoId,
            Codigo = producto.Codigo,
            Nombre = producto.Nombre,
            Sku = producto.Sku,
            PreciosPorSucursal = precios
        };
    }

    private class PrecioProductoRaw
    {
        public string ProductoId { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Sku { get; set; }
    }
}
