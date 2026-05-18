using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens;

namespace Users.Services;

public class AuthenticationService(ILogger<AuthenticationService> logger, IConfiguration config) : AuthService.AuthServiceBase
{
    private record User(string Username, string Password);
    private static readonly Dictionary<string, User> _users = new();

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        logger.LogInformation("Login request received: {Username}", request.Username);

        if (!_users.TryGetValue(request.Username, out var user) || user.Password != request.Password)
        {
            logger.LogInformation("User with username {Username} failed to login", request.Username);
            return Task.FromResult(new LoginResponse { Success = false });
        }

        logger.LogInformation("User with username {Username} successfully logged in", request.Username);
        return Task.FromResult(new LoginResponse { Success = true, Token = GenerateToken(request.Username) });
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        logger.LogInformation("Register request received: {Username}", request.Username);

        if (_users.ContainsKey(request.Username))
        {
            logger.LogInformation("User with username {Username} is already registered", request.Username);
            return Task.FromResult(new RegisterResponse { Success = false });
        }

        _users.Add(request.Username, new User(request.Username, request.Password));
        return Task.FromResult(new RegisterResponse { Success = true });
    }

    private string GenerateToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: [new Claim(JwtRegisteredClaimNames.Sub, username)],
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
