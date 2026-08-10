[CmdletBinding()]
param(
    [ValidateSet("shared-development", "production")]
    [string]$EnvironmentName = "shared-development",

    [string]$AwsProfile = "housekeeper-bootstrap",

    [string]$AwsRegion = "af-south-1",

    [string]$ExpectedAwsAccount = "713554442303",

    [string]$Repository = "JohannesMogashoa/housekeeper",

    [string]$SmokeUsername = "housekeeper-smoke@example.com",

    [string]$PwaDomainName,

    [string]$DnsResolver = "1.1.1.1",

    [string]$SecretName = "HOUSEKEEPER_SMOKE_ACCESS_TOKEN",

    [switch]$SkipUserProvisioning,

    [switch]$ResetSmokeUserPassword,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$plainPassword = $null
$authorizationCode = $null
$codeVerifier = $null
$codeChallenge = $null
$state = $null
$tokenResponse = $null
$accessToken = $null

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = @(& $FilePath @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine

    if ($exitCode -ne 0) {
        throw "$FilePath failed with exit code $exitCode.`n$text"
    }

    return $text
}

function Invoke-NativeCommandAllowFailure {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = @(& $FilePath @Arguments 2>&1)
    [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = ($output | ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
    }
}

function Get-StackOutputs {
    param(
        [Parameter(Mandatory)]
        [string]$AwsCommand,

        [Parameter(Mandatory)]
        [string]$StackName
    )

    $json = Invoke-NativeCommand $AwsCommand @(
        "cloudformation",
        "describe-stacks",
        "--stack-name",
        $StackName,
        "--profile",
        $AwsProfile,
        "--region",
        $AwsRegion,
        "--query",
        "Stacks[0].Outputs",
        "--output",
        "json"
    )
    $parsedOutputs = ConvertFrom-Json -InputObject $json
    $outputs = @($parsedOutputs)
    $result = @{}

    foreach ($output in $outputs) {
        $result[[string]$output.OutputKey] = [string]$output.OutputValue
    }

    # Keep the output map as one pipeline object for the caller.
    Write-Output -NoEnumerate $result
}

function ConvertTo-Base64Url {
    param(
        [Parameter(Mandatory)]
        [byte[]]$Value
    )

    return [Convert]::ToBase64String($Value).TrimEnd("=").Replace("+", "-").Replace("/", "_")
}

function ConvertFrom-SecureStringToPlainText {
    param(
        [Parameter(Mandatory)]
        [Security.SecureString]$Value
    )

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

try {
    $awsCommand = (Get-Command aws -ErrorAction Stop).Source
    $ghCommand = (Get-Command gh -ErrorAction Stop).Source

    if (-not (Get-Command Resolve-DnsName -ErrorAction SilentlyContinue)) {
        throw "Resolve-DnsName is required on Windows PowerShell 7 to verify the callback DNS record."
    }

    $identity = Invoke-NativeCommand $awsCommand @(
        "sts",
        "get-caller-identity",
        "--profile",
        $AwsProfile,
        "--region",
        $AwsRegion,
        "--output",
        "json"
    ) | ConvertFrom-Json

    if ([string]$identity.Account -ne $ExpectedAwsAccount) {
        throw "AWS account mismatch. Expected $ExpectedAwsAccount but authenticated to $($identity.Account)."
    }

    if ($EnvironmentName -eq "shared-development") {
        $defaultPwaDomain = "housekeeper-dev.yngstln.dev"
    }
    else {
        $defaultPwaDomain = "housekeeper.yngstln.dev"
    }

    if ([string]::IsNullOrWhiteSpace($PwaDomainName)) {
        $PwaDomainName = $defaultPwaDomain
    }

    $stackPrefix = "HouseKeeper-$EnvironmentName-"
    $identityOutputs = Get-StackOutputs $awsCommand "${stackPrefix}Identity"
    $storageOutputs = Get-StackOutputs $awsCommand "${stackPrefix}Storage"

    foreach ($requiredOutput in @("UserPoolId", "WebClientId", "HostedUiAuthority")) {
        if (-not $identityOutputs.ContainsKey($requiredOutput)) {
            throw "Identity stack output '$requiredOutput' is missing."
        }
    }

    if (-not $storageOutputs.ContainsKey("PwaDistributionDomain")) {
        throw "Storage stack output 'PwaDistributionDomain' is missing."
    }

    $pwaHost = ([Uri]"https://$PwaDomainName").Host
    $dnsAnswers = @(Resolve-DnsName -Name $pwaHost -Type CNAME -Server $DnsResolver -ErrorAction Stop)
    $cnameTargets = @(
        $dnsAnswers |
            Where-Object { $_.Type -eq "CNAME" } |
            ForEach-Object { ([string]$_.NameHost).TrimEnd(".").ToLowerInvariant() }
    )
    $expectedCname = ([string]$storageOutputs["PwaDistributionDomain"]).TrimEnd(".").ToLowerInvariant()

    if ($cnameTargets -notcontains $expectedCname) {
        throw "DNS for $PwaDomainName does not point to the deployed CloudFront distribution $expectedCname."
    }

    $userPoolId = [string]$identityOutputs["UserPoolId"]
    $clientId = [string]$identityOutputs["WebClientId"]
    $authority = ([string]$identityOutputs["HostedUiAuthority"]).TrimEnd("/")
    $redirectUri = "https://$PwaDomainName/authentication/login-callback"

    $clientJson = Invoke-NativeCommand $awsCommand @(
        "cognito-idp",
        "describe-user-pool-client",
        "--user-pool-id",
        $userPoolId,
        "--client-id",
        $clientId,
        "--profile",
        $AwsProfile,
        "--region",
        $AwsRegion,
        "--query",
        "UserPoolClient",
        "--output",
        "json"
    )
    $client = $clientJson | ConvertFrom-Json

    if (@($client.CallbackURLs) -notcontains $redirectUri) {
        throw "Cognito callback URL '$redirectUri' is not registered on the deployed client."
    }

    if (@($client.AllowedOAuthFlows) -notcontains "code") {
        throw "The Cognito client is not configured for authorization-code flow."
    }

    foreach ($requiredScope in @("housekeeper-api/read", "housekeeper-api/write")) {
        if (@($client.AllowedOAuthScopes) -notcontains $requiredScope) {
            throw "Cognito scope '$requiredScope' is not registered on the deployed client."
        }
    }

    $environmentCheck = Invoke-NativeCommandAllowFailure $ghCommand @(
        "api",
        "repos/$Repository/environments/$EnvironmentName",
        "--jq",
        ".name"
    )
    if ($environmentCheck.ExitCode -ne 0) {
        throw "GitHub environment '$EnvironmentName' could not be verified for '$Repository'."
    }

    $existingSecrets = Invoke-NativeCommand $ghCommand @(
        "secret",
        "list",
        "--env",
        $EnvironmentName,
        "--repo",
        $Repository,
        "--json",
        "name",
        "--jq",
        ".[].name"
    )
    $secretExists = @($existingSecrets -split "`r?`n" | Where-Object { $_ -eq $SecretName }).Count -gt 0
    if ($secretExists -and -not $Force) {
        $replace = Read-Host "GitHub secret $SecretName already exists. Replace it? (y/N)"
        if ($replace -notmatch "^(?i)y(es)?$") {
            throw "Existing GitHub secret was left unchanged."
        }
    }

    if (-not $SkipUserProvisioning) {
        $userCheck = Invoke-NativeCommandAllowFailure $awsCommand @(
            "cognito-idp",
            "admin-get-user",
            "--user-pool-id",
            $userPoolId,
            "--username",
            $SmokeUsername,
            "--profile",
            $AwsProfile,
            "--region",
            $AwsRegion,
            "--output",
            "json"
        )

        if ($userCheck.ExitCode -eq 0) {
            $userStatus = ([string]($userCheck.Output | ConvertFrom-Json).UserStatus)
            if ($userStatus -eq "UNCONFIRMED") {
                Invoke-NativeCommand $awsCommand @(
                    "cognito-idp",
                    "admin-confirm-sign-up",
                    "--user-pool-id",
                    $userPoolId,
                    "--username",
                    $SmokeUsername,
                    "--profile",
                    $AwsProfile,
                    "--region",
                    $AwsRegion
                ) | Out-Null
            }
            elseif ($userStatus -ne "CONFIRMED") {
                throw "Smoke user exists but has Cognito status '$userStatus'."
            }

            if ($ResetSmokeUserPassword) {
                $securePassword = Read-Host "Enter a new smoke-user password" -AsSecureString
                $plainPassword = ConvertFrom-SecureStringToPlainText $securePassword
                try {
                    Invoke-NativeCommand $awsCommand @(
                        "cognito-idp",
                        "admin-set-user-password",
                        "--user-pool-id",
                        $userPoolId,
                        "--username",
                        $SmokeUsername,
                        "--password",
                        $plainPassword,
                        "--permanent",
                        "--profile",
                        $AwsProfile,
                        "--region",
                        $AwsRegion
                    ) | Out-Null
                }
                finally {
                    Remove-Variable plainPassword, securePassword -ErrorAction SilentlyContinue
                    $plainPassword = $null
                }
            }
        }
        elseif ($userCheck.Output -notmatch "UserNotFoundException") {
            throw "Could not inspect the Cognito smoke user."
        }
        else {
            $securePassword = Read-Host "Enter the smoke-user password" -AsSecureString
            $plainPassword = ConvertFrom-SecureStringToPlainText $securePassword
            try {
                Invoke-NativeCommand $awsCommand @(
                    "cognito-idp",
                    "sign-up",
                    "--client-id",
                    $clientId,
                    "--username",
                    $SmokeUsername,
                    "--password",
                    $plainPassword,
                    "--user-attributes",
                    "Name=email,Value=$SmokeUsername",
                    "--profile",
                    $AwsProfile,
                    "--region",
                    $AwsRegion
                ) | Out-Null
            }
            finally {
                Remove-Variable plainPassword, securePassword -ErrorAction SilentlyContinue
                $plainPassword = $null
            }

            Invoke-NativeCommand $awsCommand @(
                "cognito-idp",
                "admin-confirm-sign-up",
                "--user-pool-id",
                $userPoolId,
                "--username",
                $SmokeUsername,
                "--profile",
                $AwsProfile,
                "--region",
                $AwsRegion
            ) | Out-Null
        }
    }

    $randomNumberGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $verifierBytes = New-Object -TypeName byte[] -ArgumentList 32
        $randomNumberGenerator.GetBytes($verifierBytes)

        $stateBytes = New-Object -TypeName byte[] -ArgumentList 16
        $randomNumberGenerator.GetBytes($stateBytes)
    }
    finally {
        $randomNumberGenerator.Dispose()
    }

    $codeVerifier = ConvertTo-Base64Url $verifierBytes

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $challengeHash = $sha256.ComputeHash([Text.Encoding]::ASCII.GetBytes($codeVerifier))
    }
    finally {
        $sha256.Dispose()
    }

    $codeChallenge = ConvertTo-Base64Url $challengeHash

    $state = ConvertTo-Base64Url $stateBytes

    $scope = "openid email profile housekeeper-api/read housekeeper-api/write"
    $authorizeUrl = "$authority/oauth2/authorize?client_id=$clientId&response_type=code&scope=$([Uri]::EscapeDataString($scope))&redirect_uri=$([Uri]::EscapeDataString($redirectUri))&code_challenge_method=S256&code_challenge=$codeChallenge&state=$state"

    Start-Process $authorizeUrl
    $authorizationCode = Read-Host "After signing in, paste only the code query parameter from the redirected URL"
    if ([string]::IsNullOrWhiteSpace($authorizationCode)) {
        throw "No authorization code was supplied."
    }

    try {
        $tokenResponse = Invoke-RestMethod `
            -Method Post `
            -Uri "$authority/oauth2/token" `
            -ContentType "application/x-www-form-urlencoded" `
            -Body @{
                grant_type = "authorization_code"
                client_id = $clientId
                code = $authorizationCode
                redirect_uri = $redirectUri
                code_verifier = $codeVerifier
            }
    }
    catch {
        throw "Cognito token exchange failed. The code may be expired, already used, or associated with a different PKCE verifier."
    }

    $accessToken = [string]$tokenResponse.access_token
    if ([string]::IsNullOrWhiteSpace($accessToken)) {
        throw "Cognito returned no access token."
    }

    $null = $accessToken | & $ghCommand `
        secret set $SecretName `
        --env $EnvironmentName `
        --repo $Repository `
        2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub secret update failed with exit code $LASTEXITCODE."
    }

    $verifiedSecrets = Invoke-NativeCommand $ghCommand @(
        "secret",
        "list",
        "--env",
        $EnvironmentName,
        "--repo",
        $Repository,
        "--json",
        "name",
        "--jq",
        ".[].name"
    )
    if (@($verifiedSecrets -split "`r?`n" | Where-Object { $_ -eq $SecretName }).Count -eq 0) {
        throw "GitHub did not report the expected secret name after saving."
    }

    Write-Host "Smoke user confirmed, Cognito PKCE exchange succeeded, and $SecretName was saved to GitHub environment '$EnvironmentName'."
}
finally {
    Remove-Variable plainPassword, authorizationCode, codeVerifier, codeChallenge, state, tokenResponse, accessToken -ErrorAction SilentlyContinue
}
