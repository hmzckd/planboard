using ChilliCream.Nitro.App;
using Microsoft.EntityFrameworkCore;
using Planboard.Api.Auth;
using Planboard.Api.Data;
using Planboard.Api.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlanboardDatabase(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddSingleton<ApiTokenAuthenticator>();
builder.Services.AddSingleton<ApiTokenIssuer>();

builder.Services
    .AddAuthentication(ApiTokenAuthenticationHandler.SchemeName)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
        ApiTokenAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization(options => options.AddPlanboardPolicies());

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .RegisterDbContextFactory<PlanboardDbContext>()
    .AddApiTypes()
    .AddGlobalObjectIdentification()
    .AddMutationConventions(applyToAllMutations: true)
    .AddInMemorySubscriptions()
    .AddSocketSessionInterceptor<ApiTokenSocketSessionInterceptor>()
    .AddPagingArguments()
    .AddMaxExecutionDepthRule(8, skipIntrospectionFields: true)
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 10_000;
        options.MaxTypeCost = 10_000;
    })
    .DisableIntrospection(!builder.Environment.IsDevelopment())
    .ModifyPagingOptions(options =>
    {
        options.DefaultPageSize = 20;
        options.MaxPageSize = 50;
        options.IncludeTotalCount = false;
        options.AllowBackwardPagination = false;
    })
    .ModifyServerOptions(options =>
    {
        options.Tool.ServeMode = ServeMode.Embedded;
        options.Tool.Enable = builder.Environment.IsDevelopment();
    });

var app = builder.Build();

await DatabaseSetup.PrepareAsync(app.Configuration);

app.UseAuthentication();

if (app.Configuration.GetValue<bool>("Diagnostics:SqlCommandCountHeader"))
{
    app.Use(async (context, next) =>
    {
        var counter = new SqlCommandCounter();
        context.Features.Set(counter);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Sql-Command-Count"] = counter.Count.ToString();
            return Task.CompletedTask;
        });
        await next();
    });
}

app.MapGet("/health", async (IDbContextFactory<PlanboardDbContext> dbFactory, CancellationToken cancellationToken) =>
{
    await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
    var databaseUp = await db.Database.CanConnectAsync(cancellationToken);

    return databaseUp
        ? Results.Ok(new { status = "ok", database = "up" })
        : Results.Json(new { status = "unavailable", database = "down" }, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.UseWebSockets();
app.MapGraphQL();

app.Run();

public partial class Program;
