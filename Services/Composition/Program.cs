using Grpc.Net.Client;

namespace Composition;

public class Program
{
    private record RegisterData(string Username, string Password);
    private record LoginData(string Username, string Password);
    
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        
        var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: myAllowSpecificOrigins,
                policy  =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        var app = builder.Build();
        
        app.UseCors(myAllowSpecificOrigins);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        
        using var usersChannel = GrpcChannel.ForAddress("http://localhost:5001");
        var authClient = new AuthService.AuthServiceClient(usersChannel);
        
        app.MapPost("/api/register", (RegisterData body) =>
        {
            var username = body.Username;
            var password = body.Password;
            var response = authClient.Register(new RegisterRequest() { Username = username, Password = password });
            return response.Success;
        });
        
        app.MapPost("/api/login", (LoginData body) =>
        {
            var data = new LoginRequest(){Username = body.Username, Password = body.Password};
            var response = authClient.Login(data);
            return response.Success;
        });
        
        app.Run();
    }
}