using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HouseKeeper.Api.Authentication;

internal sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";
    public const string SubjectHeader = "X-HouseKeeper-Subject";
    public const string DisplayNameHeader = "X-HouseKeeper-Display-Name";
    public const string EmailHeader = "X-HouseKeeper-Email";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment())
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string subject = Request.Headers[SubjectHeader].ToString().Trim();
        string displayName = Request.Headers[DisplayNameHeader].ToString().Trim();
        string email = Request.Headers[EmailHeader].ToString().Trim();

        if (subject.Length == 0 || displayName.Length == 0 || email.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new("email_verified", "true")
        ];

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
