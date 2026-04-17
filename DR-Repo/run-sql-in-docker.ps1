param(
    [string]$ComposeFile = "./docker-compose.yml",
    [string]$ServiceName = "postgres",
    [string]$SqlFolder = "./SQL",
    [string]$DbUser = "serviceuser",
    [string]$DbName = "RecordsDB",
    [int]$HealthTimeoutSeconds = 90,
    [switch]$NoAutoStart
)

$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found in PATH: $Name"
    }
}

function Get-ComposeContainerId {
    param(
        [string]$ComposeFilePath,
        [string]$Service
    )

    $id = docker compose -f $ComposeFilePath ps -q $Service
    return ($id | Select-Object -First 1).Trim()
}

Require-Command -Name "docker"

$composePath = Resolve-Path $ComposeFile
$sqlPath = Resolve-Path $SqlFolder

Write-Host "Using compose file: $composePath"
Write-Host "Using SQL folder:   $sqlPath"

if (-not $NoAutoStart) {
    Write-Host "Ensuring service '$ServiceName' is running..."
    docker compose -f $composePath up -d $ServiceName | Out-Host
}

$containerId = Get-ComposeContainerId -ComposeFilePath $composePath -Service $ServiceName
if ([string]::IsNullOrWhiteSpace($containerId)) {
    throw "Could not find a container for service '$ServiceName'."
}

$deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    $health = docker inspect -f "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" $containerId
    $health = ($health | Select-Object -First 1).Trim()

    if ($health -eq "healthy" -or $health -eq "running") {
        Write-Host "Container is ready (status: $health)."
        break
    }

    Write-Host "Waiting for container to become ready (current: $health)..."
    Start-Sleep -Seconds 2
}

if ((Get-Date) -ge $deadline) {
    throw "Container '$containerId' did not become ready within $HealthTimeoutSeconds seconds."
}

$sqlFiles = Get-ChildItem -Path $sqlPath -File -Filter "*.sql"
if (-not $sqlFiles) {
    throw "No .sql files found in folder: $sqlPath"
}

# Run schema scripts first, then seed scripts last.
$orderedFiles = @(
    $sqlFiles | Where-Object { $_.BaseName -notmatch "seed" } | Sort-Object Name
    $sqlFiles | Where-Object { $_.BaseName -match "seed" } | Sort-Object Name
)

Write-Host ""
Write-Host "Executing SQL files in this order:"
$orderedFiles | ForEach-Object { Write-Host " - $($_.Name)" }

foreach ($file in $orderedFiles) {
    Write-Host ""
    Write-Host "Applying $($file.Name)..."

    $content = Get-Content -Path $file.FullName -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        Write-Host "Skipped empty file: $($file.Name)"
        continue
    }

    $execArgs = @(
        "compose", "-f", "$composePath", "exec", "-T", "$ServiceName",
        "psql", "-v", "ON_ERROR_STOP=1", "-U", "$DbUser", "-d", "$DbName", "-f", "-"
    )

    $content | & docker @execArgs | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "Failed while executing file: $($file.Name)"
    }
}

Write-Host ""
Write-Host "All SQL files executed successfully."
