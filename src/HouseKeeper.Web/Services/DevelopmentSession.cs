using Microsoft.JSInterop;

namespace HouseKeeper.Web.Services;

public sealed class DevelopmentSession(IJSRuntime jsRuntime)
{
    private const string SubjectKey = "housekeeper.dev.subject";
    private const string DisplayNameKey = "housekeeper.dev.displayName";

    public async ValueTask<DevelopmentIdentity?> LoadAsync()
    {
        string? subject = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            SubjectKey);
        string? displayName = await jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            DisplayNameKey);

        return string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(displayName)
            ? null
            : new DevelopmentIdentity(subject, displayName);
    }

    public async ValueTask<DevelopmentIdentity> SignInAsync(string displayName)
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

        await jsRuntime.InvokeVoidAsync("localStorage.setItem", SubjectKey, subject);
        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            DisplayNameKey,
            normalizedDisplayName);

        return new DevelopmentIdentity(subject, normalizedDisplayName);
    }

    public async ValueTask SignOutAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", SubjectKey);
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", DisplayNameKey);
    }
}
