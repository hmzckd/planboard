using ChilliCream.Nitro.App;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;
using Planboard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlanboardDatabase(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();

builder.Services
    .AddAuthentication(ApiTokenAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
        ApiTokenAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .RegisterDbContextFactory<PlanboardDbContext>()
    .AddApiTypes()
    .ModifyServerOptions(options => options.Tool.ServeMode = ServeMode.Embedded);

var app = builder.Build();

await DatabaseSetup.PrepareAsync(app.Configuration);

app.UseAuthentication();

app.MapGet("/health", async (IDbContextFactory<PlanboardDbContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
    var databaseUp = await db.Database.CanConnectAsync(cancellationToken);

    return databaseUp
        ? Results.Ok(new { status = "ok", database = "up" })
        : Results.Json(new { status = "unavailable", database = "down" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapGraphQL();

app.Run();

public partial class Program;
