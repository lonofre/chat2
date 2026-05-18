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
                    policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]!.Split(','))
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

        builder.Services.AddSingleton(sp =>
        {
            var url = builder.Configuration["Services:Users"]!;
            return new AuthService.AuthServiceClient(GrpcChannel.ForAddress(url));
        });

        var app = builder.Build();

        app.UseCors(myAllowSpecificOrigins);

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost("/api/register", (AuthService.AuthServiceClient authClient, RegisterData body) =>
        {
            var response = authClient.Register(new RegisterRequest { Username = body.Username, Password = body.Password });
            return response.Success;
        });

        app.MapPost("/api/login", (AuthService.AuthServiceClient authClient, LoginData body) =>
        {
            var response = authClient.Login(new LoginRequest { Username = body.Username, Password = body.Password });
            if (!response.Success)
                return Results.Unauthorized();
            return Results.Ok(new { token = response.Token });
        });

        app.Run();
    }
}
