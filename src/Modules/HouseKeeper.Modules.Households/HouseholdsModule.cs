using System.Diagnostics;
using System.Security.Claims;

using HouseKeeper.Contracts.Households;
using HouseKeeper.Modules.Households.Diagnostics;
using HouseKeeper.Modules.Households.Domain;
using HouseKeeper.Modules.Households.Persistence;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HouseKeeper.Modules.Households;

public static class HouseholdsModule
{
    public static IServiceCollection AddHouseholdsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("HouseKeeper")
            ?? throw new InvalidOperationException(
                "Connection string 'HouseKeeper' is required.");

        services.AddDbContext<HouseholdsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    HouseholdsDbContext.Schema)));

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdsModule(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/households")
            .RequireAuthorization()
            .WithTags("Households");

        group.MapGet("/", ListHouseholdsAsync)
            .WithName("ListHouseholds");

        group.MapPost("/", CreateHouseholdAsync)
            .WithName("CreateHousehold");

        return endpoints;
    }

    private static async Task<IResult> ListHouseholdsAsync(
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        List<HouseholdSummary> households = await (
            from household in dbContext.Households.AsNoTracking()
            join member in dbContext.Members.AsNoTracking()
                on household.Id equals member.HouseholdId
            where member.Subject == subject
            orderby household.CreatedAtUtc
            select new HouseholdSummary(
                household.Id,
                household.Name,
                household.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(households);
    }

    private static async Task<IResult> CreateHouseholdAsync(
        CreateHouseholdRequest request,
        ClaimsPrincipal principal,
        HouseholdsDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<HouseholdsDbContext> logger,
        CancellationToken cancellationToken)
    {
        string? subject = GetSubject(principal);
        if (subject is null)
        {
            return Results.Unauthorized();
        }

        if (!HouseholdName.TryCreate(request.Name, out HouseholdName? name, out string? error))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["name"] = [error ?? "Household name is invalid."]
                });
        }

        Guid householdId = Guid.NewGuid();
        DateTimeOffset now = timeProvider.GetUtcNow();
        Household household = Household.Create(householdId, name!, now);
        HouseholdMember owner = HouseholdMember.CreateOwner(householdId, subject, now);

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        dbContext.Add(household);
        dbContext.Add(owner);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        string traceId = Activity.Current?.TraceId.ToString() ?? "unavailable";
        HouseholdsLog.HouseholdCreated(logger, householdId, subject, traceId);

        HouseholdSummary response = new(household.Id, household.Name, household.CreatedAtUtc);
        return Results.Created($"/api/households/{household.Id}", response);
    }

    private static string? GetSubject(ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
