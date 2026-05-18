using System.Text;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
                policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseCors(myAllowSpecificOrigins);

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseAuthentication();
        app.UseAuthorization();

        using var usersChannel = GrpcChannel.ForAddress("http://localhost:5001");
        var authClient = new AuthService.AuthServiceClient(usersChannel);

        app.MapPost("/api/register", (RegisterData body) =>
        {
            var response = authClient.Register(new RegisterRequest { Username = body.Username, Password = body.Password });
            return response.Success;
        });

        app.MapPost("/api/login", (LoginData body) =>
        {
            var response = authClient.Login(new LoginRequest { Username = body.Username, Password = body.Password });
            if (!response.Success)
                return Results.Unauthorized();
            return Results.Ok(new { token = response.Token });
        });

        app.Run();
    }
}
