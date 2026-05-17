var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Chat>("chat");
builder.AddProject<Projects.Users>("users");
builder.AddProject<Projects.Composition>("composition");
builder.AddProject<Projects.Gateway>("gateway");

builder.Build().Run();