using ApisConsulta.Application.Consultas.Envios.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IEnvioRepository
{
    Task<IReadOnlyList<EnvioResponse>> GetByFolioAsync(string folio);
}
