param(
    [string]$OutputDirectory = "dev/kafka-tls/certs",
    [string]$Password = "changeit",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$openssl = (Get-Command openssl -ErrorAction Stop).Source
$keytoolCommand = Get-Command keytool -ErrorAction SilentlyContinue

if ($null -ne $keytoolCommand) {
    $keytool = $keytoolCommand.Source
}
else {
    $candidatePaths = @(
        "C:\Program Files\Java\jdk1.8.0_271\bin\keytool.exe",
        "C:\Program Files\Java\jre1.8.0_361\bin\keytool.exe",
        "C:\Program Files\Java\jre1.8.0_271\bin\keytool.exe"
    )

    $keytool = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($keytool)) {
    throw "Could not locate keytool.exe. Install a JDK/JRE or add keytool to PATH before generating Kafka TLS assets."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$certDirectory = Join-Path $repoRoot $OutputDirectory

New-Item -ItemType Directory -Force -Path $certDirectory | Out-Null

$files = @{
    CaCert = Join-Path $certDirectory "ca.pem"
    CaKey = Join-Path $certDirectory "ca.key"
    ServerKey = Join-Path $certDirectory "server.key"
    ServerCert = Join-Path $certDirectory "server.crt"
    ClientKey = Join-Path $certDirectory "client.key"
    ClientCert = Join-Path $certDirectory "client.crt"
    ServerKeystore = Join-Path $certDirectory "server.keystore.p12"
    ServerTruststore = Join-Path $certDirectory "server.truststore.p12"
}

$requiredFiles = @(
    $files.CaCert,
    $files.CaKey,
    $files.ServerKey,
    $files.ServerCert,
    $files.ClientKey,
    $files.ClientCert,
    $files.ServerKeystore,
    $files.ServerTruststore
)

if (-not $Force -and ($requiredFiles | Where-Object { -not (Test-Path $_) }).Count -eq 0) {
    Write-Host "TLS certificate assets already exist at $certDirectory"
    exit 0
}

if ($Force) {
    foreach ($path in $requiredFiles) {
        if (Test-Path $path) {
            Remove-Item -Force $path
        }
    }
}

$tempDirectory = Join-Path $certDirectory "tmp"
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

$serverCsr = Join-Path $tempDirectory "server.csr"
$clientCsr = Join-Path $tempDirectory "client.csr"
$serverExt = Join-Path $tempDirectory "server.ext"
$clientExt = Join-Path $tempDirectory "client.ext"
$caSerial = Join-Path $tempDirectory "ca.srl"

@"
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=DNS:localhost,DNS:kafka-tls,IP:127.0.0.1
"@ | Set-Content -Path $serverExt -NoNewline

@"
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=clientAuth
"@ | Set-Content -Path $clientExt -NoNewline

& $openssl req -x509 -newkey rsa:2048 -days 3650 -nodes `
    -keyout $files.CaKey `
    -out $files.CaCert `
    -subj "/CN=dotnet-kafka-adapter-ca"

& $openssl req -new -newkey rsa:2048 -nodes `
    -keyout $files.ServerKey `
    -out $serverCsr `
    -subj "/CN=localhost"

& $openssl x509 -req `
    -in $serverCsr `
    -CA $files.CaCert `
    -CAkey $files.CaKey `
    -CAcreateserial `
    -CAserial $caSerial `
    -out $files.ServerCert `
    -days 3650 `
    -sha256 `
    -extfile $serverExt

& $openssl req -new -newkey rsa:2048 -nodes `
    -keyout $files.ClientKey `
    -out $clientCsr `
    -subj "/CN=dotnet-kafka-adapter-client"

& $openssl x509 -req `
    -in $clientCsr `
    -CA $files.CaCert `
    -CAkey $files.CaKey `
    -CAserial $caSerial `
    -out $files.ClientCert `
    -days 3650 `
    -sha256 `
    -extfile $clientExt

& $openssl pkcs12 -export `
    -in $files.ServerCert `
    -inkey $files.ServerKey `
    -certfile $files.CaCert `
    -name "kafka-broker" `
    -out $files.ServerKeystore `
    -passout "pass:$Password"

if (Test-Path $files.ServerTruststore) {
    Remove-Item -Force $files.ServerTruststore
}

& $keytool -importcert `
    -noprompt `
    -alias "kafka-ca" `
    -file $files.CaCert `
    -keystore $files.ServerTruststore `
    -storetype PKCS12 `
    -storepass $Password

Remove-Item -Recurse -Force $tempDirectory
Write-Host "Generated TLS certificate assets at $certDirectory"
