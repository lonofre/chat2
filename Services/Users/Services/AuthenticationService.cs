using Grpc.Core;

namespace Users.Services;

public class AuthenticationService(ILogger<AuthenticationService> logger) : AuthService.AuthServiceBase
{
    
    private record User(string Username, string Password);
    private static readonly Dictionary<string, User> _users = new Dictionary<string, User>();
    
    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        // TODO: Check why logging is expensive and why we need to use static or fixed template (Ask claude)
        logger.LogInformation("Login request received: {Username}", request.Username);

        if (_users.ContainsKey(request.Username) && _users[request.Username].Password == request.Password)
        {
            logger.LogInformation("User with username {Username} successfully logged in", request.Username);
            return Task.FromResult(new LoginResponse { Success = true });
        }
        
        logger.LogInformation("User with username {Username} failed to login", request.Username);
        return Task.FromResult(new LoginResponse { Success = false });
    }

    public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        logger.LogInformation("Register request received: {Username}", request.Username);

        if (_users.ContainsKey(request.Username))
        {
            logger.LogInformation("User with username {Username} is already registered", request.Username);
            return Task.FromResult(new RegisterResponse(){ Success = false });
        }
        
        _users.Add(request.Username, new User(request.Username, request.Password));
        
        return Task.FromResult(new RegisterResponse { Success = true });
    }
}