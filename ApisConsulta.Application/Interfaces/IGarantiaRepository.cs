using ApisConsulta.Application.Consultas.Garantias.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IGarantiaRepository
{
    Task<GarantiaResponse?> GetByFolioAsync(string folioTicket);
}
