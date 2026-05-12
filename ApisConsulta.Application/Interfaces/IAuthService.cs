namespace ApisConsulta.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Token, string? Message)> LoginAsync(string username, string password);
}
