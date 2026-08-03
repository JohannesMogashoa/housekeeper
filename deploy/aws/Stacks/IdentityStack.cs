using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Cdklabs.CdkNag;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class IdentityStack : Stack
{
    public IdentityStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration)
        : base(scope, id, props)
    {
        UserPool = new UserPool(
            this,
            "UserPool",
            new UserPoolProps
            {
                UserPoolName = $"housekeeper-{configuration.EnvironmentName}",
                SelfSignUpEnabled = true,
                SignInAliases = new SignInAliases { Email = true },
                AutoVerify = new AutoVerifiedAttrs { Email = true },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 12,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireDigits = true,
                    RequireSymbols = true
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
                DeletionProtection = configuration.IsProduction,
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.DESTROY
            });

        ResourceServerScope readScope = new(new ResourceServerScopeProps
        {
            ScopeName = "read",
            ScopeDescription = "Read HouseKeeper API resources."
        });

        ResourceServerScope writeScope = new(new ResourceServerScopeProps
        {
            ScopeName = "write",
            ScopeDescription = "Mutate HouseKeeper API resources."
        });

        ApiResourceServer = UserPool.AddResourceServer(
            "ApiResourceServer",
            new UserPoolResourceServerOptions
            {
                Identifier = "housekeeper-api",
                Scopes = [readScope, writeScope]
            });

        WebDomain = UserPool.AddDomain(
            "CognitoDomain",
            new UserPoolDomainOptions
            {
                CognitoDomain = new CognitoDomainOptions
                {
                    DomainPrefix = configuration.CognitoDomainPrefix
                }
            });

        WebClient = UserPool.AddClient(
            "PwaClient",
            new UserPoolClientOptions
            {
                GenerateSecret = false,
                PreventUserExistenceErrors = true,
                OAuth = new OAuthSettings
                {
                    Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                    CallbackUrls = configuration.CallbackUrls.ToArray(),
                    LogoutUrls = configuration.LogoutUrls.ToArray(),
                    Scopes =
                    [
                        OAuthScope.OPENID,
                        OAuthScope.EMAIL,
                        OAuthScope.PROFILE,
                        OAuthScope.ResourceServer(ApiResourceServer, readScope),
                        OAuthScope.ResourceServer(ApiResourceServer, writeScope)
                    ]
                }
            });

        HostedUiAuthority =
            $"https://{configuration.CognitoDomainPrefix}.auth.{configuration.Region}.amazoncognito.com";
        Issuer =
            $"https://cognito-idp.{configuration.Region}.amazonaws.com/{UserPool.UserPoolId}";

        _ = new CfnOutput(
            this,
            "UserPoolId",
            new CfnOutputProps { Value = UserPool.UserPoolId });
        _ = new CfnOutput(
            this,
            "WebClientId",
            new CfnOutputProps { Value = WebClient.UserPoolClientId });
        _ = new CfnOutput(
            this,
            "HostedUiAuthority",
            new CfnOutputProps { Value = HostedUiAuthority });
        _ = new CfnOutput(
            this,
            "Issuer",
            new CfnOutputProps { Value = Issuer });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        NagSuppressions.AddResourceSuppressions(
            UserPool,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-COG2",
                    Reason = "MFA policy is a product and onboarding decision; the shared development pool uses verified email and a strong password policy."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-COG8",
                    Reason = "The shared development pool uses the default Cognito feature tier to keep the disposable environment within budget."
                }
            });
    }

    public UserPool UserPool { get; }

    public UserPoolResourceServer ApiResourceServer { get; }

    public UserPoolDomain WebDomain { get; }

    public UserPoolClient WebClient { get; }

    public string HostedUiAuthority { get; }

    public string Issuer { get; }
}
