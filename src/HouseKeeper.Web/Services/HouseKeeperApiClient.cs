using System.Net.Http.Json;

using HouseKeeper.Contracts.Authentication;
using HouseKeeper.Contracts.Households;

namespace HouseKeeper.Web.Services;

public sealed class HouseKeeperApiClient(HttpClient httpClient)
{
    private const string SubjectHeader = "X-HouseKeeper-Subject";
    private const string DisplayNameHeader = "X-HouseKeeper-Display-Name";

    public async Task<CurrentUserResponse> AuthenticateAsync(
        DevelopmentIdentity identity,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Get,
            "api/me",
            identity);
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
        DevelopmentIdentity identity,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Get,
            "api/households",
            identity);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<HouseholdSummary>>(
            cancellationToken)
            ?? [];
    }

    public async Task<HouseholdSummary> CreateHouseholdAsync(
        DevelopmentIdentity identity,
        string householdName,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            "api/households",
            identity);
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

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        DevelopmentIdentity identity)
    {
        HttpRequestMessage request = new(method, requestUri);
        request.Headers.Add(SubjectHeader, identity.Subject);
        request.Headers.Add(DisplayNameHeader, identity.DisplayName);
        return request;
    }
}
