using System.Net.Http.Json;

using HouseKeeper.Contracts.Authentication;
using HouseKeeper.Contracts.Households;

namespace HouseKeeper.Web.Services;

public sealed class HouseKeeperApiClient(HttpClient httpClient)
{
    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        IApiAuthentication authentication,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/me");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty current-user response.");
    }

    public async Task<IReadOnlyList<HouseholdSummary>> ListHouseholdsAsync(
        IApiAuthentication authentication,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/households");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<HouseholdSummary>>(
            cancellationToken)
            ?? [];
    }

    public async Task<HouseholdSummary> CreateHouseholdAsync(
        IApiAuthentication authentication,
        string householdName,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "api/households");
        await authentication.AttachAsync(request, cancellationToken);
        request.Content = JsonContent.Create(new CreateHouseholdRequest(householdName));

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<HouseholdSummary>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty household response.");
    }
}
