var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// TODO: Entity Framework setup will be completed in next phase

app.MapGet("/", () => "Hello World!");

app.Run();
