namespace HouseKeeper.Infrastructure.Configuration;

public sealed record PlatformConfiguration
{
    public const string DefaultRegion = "af-south-1";
    public const string DefaultGitHubRepository = "JohannesMogashoa/housekeeper";

    public string EnvironmentName { get; init; } = "development";

    public string Region { get; init; } = DefaultRegion;

    public string? Account { get; init; }

    public string GitHubRepository { get; init; } = DefaultGitHubRepository;

    public string GitHubBranch { get; init; } = "master";

    public string? ApiCertificateArn { get; init; }

    public string? ApiDomainName { get; init; }

    public string? ApiImageUri { get; init; }

    public int ApiDesiredCount { get; init; } = 1;

    public string CognitoDomainPrefix { get; init; } = "housekeeper-development";

    public IReadOnlyList<string> CallbackUrls { get; init; } =
        ["http://localhost:5136/authentication/login-callback"];

    public IReadOnlyList<string> LogoutUrls { get; init; } =
        ["http://localhost:5136/authentication/logout-callback"];

    public bool IsProduction => string.Equals(
        EnvironmentName,
        "production",
        StringComparison.OrdinalIgnoreCase);

    public bool IsProtectedEnvironment => IsProduction || string.Equals(
        EnvironmentName,
        "shared-development",
        StringComparison.OrdinalIgnoreCase);

    public static PlatformConfiguration Load()
    {
        PlatformConfiguration configuration = new()
        {
            EnvironmentName = Get("HOUSEKEEPER_ENVIRONMENT", "development"),
            Region = Get("HOUSEKEEPER_AWS_REGION", DefaultRegion),
            Account = GetOptional("HOUSEKEEPER_AWS_ACCOUNT") ?? GetOptional("CDK_DEFAULT_ACCOUNT"),
            GitHubRepository = Get("HOUSEKEEPER_GITHUB_REPOSITORY", DefaultGitHubRepository),
            GitHubBranch = Get("HOUSEKEEPER_GITHUB_BRANCH", "master"),
            ApiCertificateArn = GetOptional("HOUSEKEEPER_API_CERTIFICATE_ARN"),
            ApiDomainName = GetOptional("HOUSEKEEPER_API_DOMAIN_NAME"),
            ApiImageUri = GetOptional("HOUSEKEEPER_API_IMAGE_URI"),
            ApiDesiredCount = GetInt("HOUSEKEEPER_API_DESIRED_COUNT", 1),
            CognitoDomainPrefix = Get(
                "HOUSEKEEPER_COGNITO_DOMAIN_PREFIX",
                $"housekeeper-{Get("HOUSEKEEPER_ENVIRONMENT", "development")}"),
            CallbackUrls = GetList(
                "HOUSEKEEPER_COGNITO_CALLBACK_URLS",
                "http://localhost:5136/authentication/login-callback"),
            LogoutUrls = GetList(
                "HOUSEKEEPER_COGNITO_LOGOUT_URLS",
                "http://localhost:5136/authentication/logout-callback")
        };

        configuration.Validate();
        return configuration;
    }

    public void Validate()
    {
        if (!string.Equals(Region, DefaultRegion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"HouseKeeper infrastructure must target {DefaultRegion}.");
        }

        if (string.IsNullOrWhiteSpace(GitHubRepository) || !GitHubRepository.Contains('/'))
        {
            throw new InvalidOperationException(
                "HOUSEKEEPER_GITHUB_REPOSITORY must use the owner/name form.");
        }

        if (CallbackUrls.Count == 0 || LogoutUrls.Count == 0)
        {
            throw new InvalidOperationException(
                "Cognito callback and logout URL lists must not be empty.");
        }

        if (ApiDesiredCount < 0)
        {
            throw new InvalidOperationException(
                "HOUSEKEEPER_API_DESIRED_COUNT must be zero or greater.");
        }

        if (IsProtectedEnvironment && string.IsNullOrWhiteSpace(Account))
        {
            throw new InvalidOperationException(
                "HOUSEKEEPER_AWS_ACCOUNT or CDK_DEFAULT_ACCOUNT is required for protected environments.");
        }

        if (IsProduction &&
            (string.IsNullOrWhiteSpace(ApiCertificateArn) ||
             string.IsNullOrWhiteSpace(ApiDomainName)))
        {
            throw new InvalidOperationException(
                "Production requires HOUSEKEEPER_API_CERTIFICATE_ARN and HOUSEKEEPER_API_DOMAIN_NAME.");
        }
    }

    private static string Get(string name, string fallback) =>
        GetOptional(name) ?? fallback;

    private static string? GetOptional(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name)?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int GetInt(string name, int fallback)
    {
        string? value = GetOptional(name);
        return value is null
            ? fallback
            : int.TryParse(value, out int parsed)
                ? parsed
                : throw new InvalidOperationException($"{name} must be a valid integer.");
    }

    private static string[] GetList(string name, string fallback) =>
        (GetOptional(name) ?? fallback)
        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
