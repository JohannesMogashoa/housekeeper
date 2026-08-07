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

    public async Task CreateInvitationAsync(
        IApiAuthentication authentication,
        Guid householdId,
        string email,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"api/households/{householdId}/invitations");
        await authentication.AttachAsync(request, cancellationToken);
        request.Content = JsonContent.Create(new CreateInvitationRequest(email));

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<InvitationSummary>> ListInvitationsAsync(
        IApiAuthentication authentication,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"api/households/{householdId}/invitations");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<InvitationSummary>>(
            cancellationToken)
            ?? [];
    }

    public async Task RevokeInvitationAsync(
        IApiAuthentication authentication,
        Guid householdId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"api/households/{householdId}/invitations/{invitationId}/revoke");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<HouseholdMemberSummary>> ListMembersAsync(
        IApiAuthentication authentication,
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"api/households/{householdId}/members");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<HouseholdMemberSummary>>(
            cancellationToken)
            ?? [];
    }

    public async Task RemoveMemberAsync(
        IApiAuthentication authentication,
        Guid householdId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"api/households/{householdId}/members/{memberId}/remove");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<InvitationPreview> PreviewInvitationAsync(
        IApiAuthentication authentication,
        string token,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"api/invitations/{Uri.EscapeDataString(token)}/preview");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<InvitationPreview>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty invitation preview.");
    }

    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(
        IApiAuthentication authentication,
        string token,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            $"api/invitations/{Uri.EscapeDataString(token)}/accept");
        await authentication.AttachAsync(request, cancellationToken);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AcceptInvitationResponse>(
            cancellationToken)
            ?? throw new InvalidOperationException(
                "The API returned an empty invitation acceptance response.");
    }
}
