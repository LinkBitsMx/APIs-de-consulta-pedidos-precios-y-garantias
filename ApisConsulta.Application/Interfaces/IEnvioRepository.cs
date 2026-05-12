using ApisConsulta.Application.Consultas.Envios.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IEnvioRepository
{
    Task<EnvioResponse?> GetByFolioAsync(string folio);
}
