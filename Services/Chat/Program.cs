using Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<GroupMembershipStore>();
builder.Services.AddSignalR();

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy  =>
        {
            policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]!.Split(','))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

var app = builder.Build();

app.UseCors(myAllowSpecificOrigins);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHub<ChatHub>("/chatHub");

app.Run();