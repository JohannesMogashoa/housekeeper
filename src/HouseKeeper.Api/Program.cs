using System.Diagnostics;
using System.Security.Claims;

using HouseKeeper.Api.Authentication;
using HouseKeeper.Contracts.Authentication;
using HouseKeeper.Modules.Households;
using HouseKeeper.Modules.Households.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("HouseKeeperWeb", policy =>
    {
        policy
            .WithOrigins("http://localhost:5136", "https://localhost:7229")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHouseholdsModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("HouseKeeperWeb");

app.Use(async (context, next) =>
{
    string traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Trace-Id"] = traceId;

    using IDisposable? scope = app.Logger.BeginScope(
        new Dictionary<string, object?>
        {
            ["TraceId"] = traceId
        });

    await next(context);
});

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.MapGet(
    "/health/ready",
    async (HouseholdsDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.Problem(
                title: "Database unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable));

app.MapGet("/api/me", (ClaimsPrincipal principal) =>
{
    string? subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    string? displayName = principal.Identity?.Name;

    return subject is not null && displayName is not null
        ? Results.Ok(new CurrentUserResponse(subject, displayName))
        : Results.Unauthorized();
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithTags("Authentication");

app.MapHouseholdsModule();

app.Run();

public partial class Program;
