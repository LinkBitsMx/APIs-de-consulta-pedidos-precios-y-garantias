using ApisConsulta.Application.Consultas.Precios.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IPrecioRepository
{
    Task<PrecioResponse?> GetByIdentificadorAsync(string identificador);
}
