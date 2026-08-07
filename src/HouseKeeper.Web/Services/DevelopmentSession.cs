using Microsoft.JSInterop;

namespace HouseKeeper.Web.Services;

public sealed class DevelopmentSession(IJSRuntime jsRuntime)
{
    public const string SubjectHeader = "X-HouseKeeper-Subject";
    public const string DisplayNameHeader = "X-HouseKeeper-Display-Name";
    public const string EmailHeader = "X-HouseKeeper-Email";

    private const string SubjectKey = "housekeeper.dev.subject";
    private const string DisplayNameKey = "housekeeper.dev.displayName";
    private const string EmailKey = "housekeeper.dev.email";

    public async ValueTask<DevelopmentIdentity?> LoadAsync()
    {
        string? subject = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            SubjectKey);
        string? displayName = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            DisplayNameKey);
        string? email = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            EmailKey);

        return string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(email)
            ? null
            : new DevelopmentIdentity(subject, displayName, email);
    }

    public async ValueTask<DevelopmentIdentity> SignInAsync(
        string displayName,
        string? email = null)
    {
        string normalizedDisplayName = displayName.Trim();
        if (normalizedDisplayName.Length < 2)
        {
            throw new ArgumentException(
                "Display name must contain at least two characters.",
                nameof(displayName));
        }

        string? existingSubject = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            SubjectKey);
        string subject = string.IsNullOrWhiteSpace(existingSubject)
            ? Guid.NewGuid().ToString("N")
            : existingSubject;
        string normalizedEmail = string.IsNullOrWhiteSpace(email)
            ? $"{subject}@local.test"
            : email.Trim().ToLowerInvariant();

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", SubjectKey, subject);
        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            DisplayNameKey,
            normalizedDisplayName);
        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            EmailKey,
            normalizedEmail);

        return new DevelopmentIdentity(subject, normalizedDisplayName, normalizedEmail);
    }

    public async ValueTask SignOutAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", SubjectKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", DisplayNameKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", EmailKey);
    }
}
