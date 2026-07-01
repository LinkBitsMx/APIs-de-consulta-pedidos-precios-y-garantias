using ApisConsulta.Application.PreOrdenes.Requests;
using ApisConsulta.Application.PreOrdenes.Response;

namespace ApisConsulta.Application.Interfaces;

public interface IPreOrdenRepository
{
    Task<PreOrdenResponse> CrearAsync(CrearPreOrdenRequest request);
    Task<IReadOnlyList<PreOrdenResumenResponse>> GetAllAsync(string? estatus);
    Task<PreOrdenResponse?> GetByIdAsync(int id);
}
