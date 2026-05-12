using ApisConsulta.Application.Auth;
using ApisConsulta.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApisConsulta.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, token, message) = await _authService.LoginAsync(request.Username, request.Password);

        if (!success)
            return Unauthorized(new { message });

        var expirationMinutes = int.Parse(_configuration.GetSection("JwtSettings")["ExpirationMinutes"]!);

        return Ok(new LoginResponse
        {
            Token = token!,
            Expiration = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Username = request.Username
        });
    }
}
