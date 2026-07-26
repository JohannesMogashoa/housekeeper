using System.Diagnostics;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using HouseKeeper.Api.Authentication;
using HouseKeeper.Contracts.Authentication;
using HouseKeeper.Modules.Households;
using HouseKeeper.Modules.Households.Persistence;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

bool developmentAuthentication = builder.Environment.IsDevelopment() &&
    string.Equals(
        builder.Configuration["Authentication:Mode"],
        "Development",
        StringComparison.OrdinalIgnoreCase);

AuthenticationBuilder authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = developmentAuthentication
        ? DevelopmentAuthenticationHandler.SchemeName
        : JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = developmentAuthentication
        ? DevelopmentAuthenticationHandler.SchemeName
        : JwtBearerDefaults.AuthenticationScheme;
});

if (developmentAuthentication)
{
    _ = authentication.AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
        DevelopmentAuthenticationHandler.SchemeName,
        _ => { });
}
else
{
    string authority = builder.Configuration["Authentication:Cognito:Authority"]?.Trim()
        ?? string.Empty;
    string audience = builder.Configuration["Authentication:Cognito:Audience"]?.Trim()
        ?? string.Empty;
    if (authority.Length == 0 || audience.Length == 0)
    {
        throw new InvalidOperationException(
            "Cognito authority and audience are required outside local development.");
    }

    _ = authentication.AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                string? subject = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrWhiteSpace(subject))
                {
                    context.Fail("The access token does not contain a subject.");
                }

                return Task.CompletedTask;
            }
        };
    });
}

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("HouseKeeperWeb", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5136",
                "http://127.0.0.1:5136",
                "https://localhost:7229")
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
    string? displayName = principal.Identity?.Name
        ?? principal.FindFirst("preferred_username")?.Value
        ?? principal.FindFirst(ClaimTypes.Email)?.Value
        ?? subject;

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
