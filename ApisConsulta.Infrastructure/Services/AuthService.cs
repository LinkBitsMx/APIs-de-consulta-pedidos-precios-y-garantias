using ApisConsulta.Application.Interfaces;
using ApisConsulta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ApisConsulta.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Token, string? Message)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, null, "Usuario y contraseña requeridos");

        var user = await _context.Database
            .SqlQuery<dynamic>($@"
                SELECT TOP 1
                    idUsuario,
                    vchUsuario,
                    vchPassword,
                    idEstatus
                FROM catUsers
                WHERE vchUsuario = {username}")
            .FirstOrDefaultAsync();

        if (user == null)
            return (false, null, "Usuario no encontrado");

        var storedPassword = user.vchPassword as string;
        var idEstatus = user.idEstatus as int? ?? 0;

        if (idEstatus != 1)
            return (false, null, "Usuario inactivo");

        var passwordHash = ComputeMd5(password);
        if (storedPassword != passwordHash)
            return (false, null, "Contraseña incorrecta");

        var token = GenerateJwtToken(user.vchUsuario);
        return (true, token, null);
    }

    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpirationMinutes"]!)),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string ComputeMd5(string input)
    {
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes)
            sb.Append(hashByte.ToString("x2"));
        return sb.ToString();
    }
}
