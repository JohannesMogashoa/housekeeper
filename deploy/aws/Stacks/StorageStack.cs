using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Cdklabs.CdkNag;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class StorageStack : Stack
{
    public StorageStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration)
        : base(scope, id, props)
    {
        PwaBucket = CreatePrivateBucket(
            "PwaBucket",
            configuration,
            "pwa",
            versioned: true,
            enableCors: false);

        AttachmentBucket = CreatePrivateBucket(
            "AttachmentBucket",
            configuration,
            "attachments",
            versioned: true,
            enableCors: true);

        MalwareProtectionRole = null;
        GuardDutyDetector = null;
        MalwareProtectionPlan = null;

        if (configuration.EnableGuardDuty)
        {
            Role malwareProtectionRole = new(
                this,
                "MalwareProtectionRole",
                new RoleProps
                {
                    AssumedBy = new ServicePrincipal("malware-protection-plan.guardduty.amazonaws.com"),
                    Description = "GuardDuty Malware Protection for S3 access to HouseKeeper attachment objects."
                });

            malwareProtectionRole.AddToPolicy(
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Effect = Effect.ALLOW,
                        Actions =
                        [
                            "s3:GetBucketLocation",
                            "s3:ListBucket",
                            "s3:GetObject",
                            "s3:GetObjectVersion",
                            "s3:PutObject",
                            "s3:PutObjectAcl"
                        ],
                        Resources =
                        [
                            AttachmentBucket.BucketArn,
                            AttachmentBucket.ArnForObjects("*")
                        ]
                    }));

            MalwareProtectionRole = malwareProtectionRole;
            GuardDutyDetector = new CfnResource(
                this,
                "GuardDutyDetector",
                new CfnResourceProps
                {
                    Type = "AWS::GuardDuty::Detector",
                    Properties = new Dictionary<string, object>
                    {
                        ["Enable"] = true
                    }
                });

            MalwareProtectionPlan = new CfnResource(
                this,
                "MalwareProtectionPlan",
                new CfnResourceProps
                {
                    Type = "AWS::GuardDuty::MalwareProtectionPlan",
                    Properties = new Dictionary<string, object>
                    {
                        ["Role"] = malwareProtectionRole.RoleArn,
                        ["ProtectedResource"] = new Dictionary<string, object>
                        {
                            ["S3Bucket"] = new Dictionary<string, object>
                            {
                                ["BucketName"] = AttachmentBucket.BucketName
                            }
                        },
                        ["Actions"] = new Dictionary<string, object>
                        {
                            ["Tagging"] = new Dictionary<string, object>
                            {
                                ["Status"] = "ENABLED"
                            }
                        }
                    }
                });
            MalwareProtectionPlan.Node.AddDependency(GuardDutyDetector);
        }

        ICertificate? pwaCertificate = configuration.PwaCertificateArn is null
            ? null
            : Certificate.FromCertificateArn(this, "PwaCertificate", configuration.PwaCertificateArn);

        PwaDistribution = new Distribution(
            this,
            "PwaDistribution",
            new DistributionProps
            {
                Certificate = pwaCertificate,
                DefaultRootObject = "index.html",
                DomainNames = configuration.PwaDomainName is null
                    ? null
                    : [configuration.PwaDomainName],
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = S3BucketOrigin.WithOriginAccessControl(PwaBucket),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    AllowedMethods = AllowedMethods.ALLOW_GET_HEAD,
                    CachePolicy = CachePolicy.CACHING_OPTIMIZED
                },
                ErrorResponses =
                [
                    new ErrorResponse
                    {
                        HttpStatus = 403,
                        ResponseHttpStatus = 200,
                        ResponsePagePath = "/index.html",
                        Ttl = Duration.Minutes(1)
                    },
                    new ErrorResponse
                    {
                        HttpStatus = 404,
                        ResponseHttpStatus = 200,
                        ResponsePagePath = "/index.html",
                        Ttl = Duration.Minutes(1)
                    }
                ],
                EnableLogging = false,
                Comment = "HouseKeeper private PWA distribution."
            });

        _ = new CfnOutput(
            this,
            "PwaDistributionDomain",
            new CfnOutputProps { Value = PwaDistribution.DistributionDomainName });
        _ = new CfnOutput(
            this,
            "PwaDistributionId",
            new CfnOutputProps { Value = PwaDistribution.DistributionId });
        _ = new CfnOutput(
            this,
            "PwaBucketName",
            new CfnOutputProps { Value = PwaBucket.BucketName });
        _ = new CfnOutput(
            this,
            "AttachmentBucketName",
            new CfnOutputProps { Value = AttachmentBucket.BucketName });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
        Amazon.CDK.Tags.Of(this).Add("DataClassification", "Household-private");

        NagSuppressions.AddResourceSuppressions(
            PwaBucket,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-S1",
                    Reason = "The shared development PWA bucket is private and disposable; CloudFront and CloudWatch provide the operational access path."
                }
            },
            true);
        NagSuppressions.AddResourceSuppressions(
            AttachmentBucket,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-S1",
                    Reason = "The shared development attachment bucket is private and disposable; attachment lifecycle and malware telemetry are the required controls."
                }
            },
            true);
        if (MalwareProtectionRole is not null)
        {
            NagSuppressions.AddResourceSuppressions(
                MalwareProtectionRole,
                new[]
                {
                    new NagPackSuppression
                    {
                        Id = "AwsSolutions-IAM5",
                        Reason = "GuardDuty Malware Protection must inspect every object version in this exact attachment bucket."
                    }
                },
                true);
        }
        NagSuppressions.AddResourceSuppressions(
            PwaDistribution,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-CFR3",
                    Reason = "CloudFront standard access logging is deferred for shared development; the origin remains private through Origin Access Control."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-CFR4",
                    Reason = "The distribution uses the CDK default modern TLS policy; the rule pack version predates the current CloudFront default."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-CFR1",
                    Reason = "HouseKeeper is a South Africa-focused development service and does not use geo restriction."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-CFR2",
                    Reason = "WAF is deferred for the disposable development distribution; the origin remains private through Origin Access Control."
                }
            });
    }

    public Bucket PwaBucket { get; }

    public Bucket AttachmentBucket { get; }

    public Role? MalwareProtectionRole { get; }

    public CfnResource? GuardDutyDetector { get; }

    public CfnResource? MalwareProtectionPlan { get; }

    public Distribution PwaDistribution { get; }

    private Bucket CreatePrivateBucket(
        string id,
        PlatformConfiguration configuration,
        string purpose,
        bool versioned,
        bool enableCors)
    {
        Bucket bucket = new(
            this,
            id,
            new BucketProps
            {
                BucketKeyEnabled = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                Encryption = BucketEncryption.S3_MANAGED,
                ObjectOwnership = ObjectOwnership.BUCKET_OWNER_ENFORCED,
                Versioned = versioned,
                AutoDeleteObjects = !configuration.IsProduction,
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.DESTROY,
                LifecycleRules =
                [
                    new LifecycleRule
                    {
                        Id = $"ExpireIncomplete{purpose}Uploads",
                        AbortIncompleteMultipartUploadAfter = Duration.Days(1)
                    }
                ],
                Cors = enableCors
                    ?
                    [
                        new CorsRule
                        {
                            AllowedMethods = [HttpMethods.PUT, HttpMethods.GET, HttpMethods.HEAD],
                            AllowedOrigins = configuration.CallbackUrls
                                .Select(static url => new Uri(url).GetLeftPart(UriPartial.Authority))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToArray(),
                            AllowedHeaders = ["*"],
                            ExposedHeaders = ["ETag"],
                            MaxAge = 300
                        }
                    ]
                    : null
            });

        bucket.AddToResourcePolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.DENY,
                    Principals = [new AnyPrincipal()],
                    Actions = ["s3:*"],
                    Resources = [bucket.BucketArn, bucket.ArnForObjects("*")],
                    Conditions = new Dictionary<string, object>
                    {
                        ["Bool"] = new Dictionary<string, object>
                        {
                            ["aws:SecureTransport"] = "false"
                        }
                    }
                }));

        return bucket;
    }
}
