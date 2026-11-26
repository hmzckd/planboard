using ChilliCream.Nitro.App;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddApiTypes()
    .ModifyServerOptions(options => options.Tool.ServeMode = ServeMode.Embedded);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGraphQL();

app.Run();

public partial class Program;
