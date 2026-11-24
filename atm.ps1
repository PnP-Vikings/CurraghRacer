[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Tool,

    [Parameter(Position = 1)]
    [string]$Parameters = "{}",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArguments,

    [Parameter()]
    [string]$ParametersFile,

    [Parameter()]
    [ValidateRange(1, 3600)]
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"
$utf8 = New-Object System.Text.UTF8Encoding -ArgumentList $false
[Console]::OutputEncoding = $utf8

function Write-BridgeError {
    param([string]$Message)
    $result = [ordered]@{
        success = $false
        message = $Message
    }
    $json = ($result | ConvertTo-Json -Compress).Replace('\u0026', '&').Replace('\u003c', '<').Replace('\u003e', '>').Replace('\u0027', "'")
    [Console]::Out.WriteLine($json)
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Tool)) {
    Write-BridgeError "Usage: .\atm.ps1 <tool_name> ['<json_parameters>'] [-ParametersFile <path>] [-TimeoutSeconds <seconds>]"
}

if ($RemainingArguments -and -not $ParametersFile) {
    $Parameters = (@($Parameters) + @($RemainingArguments)) -join " "
}

try {
    $unityEditor = Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $unityEditor) {
        Write-BridgeError "Unity Editor is not running. Start the project in Unity and retry; do not edit ATM data files directly."
    }

    $projectSettingsDir = Join-Path $PSScriptRoot "ProjectSettings\AwesomeTaskManager"
    if (-not (Test-Path -LiteralPath $projectSettingsDir -PathType Container)) {
        Write-BridgeError "ATM Next data folder was not found. Run this script from the Unity project containing ATM Next."
    }

    $commandPath = Join-Path $projectSettingsDir "external_command.json"
    $resultPath = Join-Path $projectSettingsDir "external_result.json"

    if (Test-Path -LiteralPath $commandPath -PathType Leaf) {
        Write-BridgeError "An ATM command is already pending. Wait for Unity to consume it; do not overwrite it or edit ATM data files."
    }

    if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
        Remove-Item -LiteralPath $resultPath -Force
    }

    if ($Tool.TrimStart().StartsWith("{")) {
        $command = $Tool
        $null = $command | ConvertFrom-Json
    }
    else {
        if ($ParametersFile) {
            if (-not (Test-Path -LiteralPath $ParametersFile -PathType Leaf)) {
                Write-BridgeError "Parameters file not found: $ParametersFile"
            }
            $Parameters = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $ParametersFile).Path, $utf8)
        }

        try {
            $parameterObject = $Parameters | ConvertFrom-Json
        }
        catch {
            Write-BridgeError "Invalid JSON parameters. Use one quoted JSON object or -ParametersFile. If Windows PowerShell split the quoted argument, use: & .\atm.ps1 $Tool -ParametersFile <path>."
        }

        if ($null -eq $parameterObject -or $parameterObject -isnot [System.Management.Automation.PSCustomObject] -and $parameterObject -isnot [hashtable]) {
            Write-BridgeError "Parameters must be a JSON object. Use -ParametersFile for complex input."
        }

        $payload = [ordered]@{
            tool = $Tool
            parameters = $parameterObject
        }
        $command = $payload | ConvertTo-Json -Depth 30 -Compress
    }

    $temporaryCommandPath = Join-Path $projectSettingsDir ("external_command." + [Guid]::NewGuid().ToString("N") + ".tmp")
    try {
        [System.IO.File]::WriteAllText($temporaryCommandPath, $command, $utf8)
        [System.IO.File]::Move($temporaryCommandPath, $commandPath)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryCommandPath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryCommandPath -Force -ErrorAction SilentlyContinue
        }
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        Start-Sleep -Milliseconds 250
        if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
            try {
                $resultJson = [System.IO.File]::ReadAllText($resultPath, $utf8)
                if (-not [string]::IsNullOrWhiteSpace($resultJson)) {
                    $null = $resultJson | ConvertFrom-Json
                    [Console]::Out.WriteLine($resultJson)
                    exit 0
                }
            }
            catch {
                # Unity may still be writing the result; retry until timeout.
            }
        }
    }

    if (Test-Path -LiteralPath $commandPath -PathType Leaf) {
        try {
            $pendingCommand = [System.IO.File]::ReadAllText($commandPath, $utf8)
            if ($pendingCommand -eq $command) {
                Remove-Item -LiteralPath $commandPath -Force
            }
        }
        catch {
            # Preserve the pending command if it cannot be verified as this invocation's command.
        }
    }

    Write-BridgeError "ATM bridge timed out after $TimeoutSeconds seconds. The pending command was removed if Unity had not consumed it. Confirm Unity is open and not compiling, then retry; never edit ATM data files."
}
catch {
    Write-BridgeError "ATM bridge failed: $($_.Exception.Message)"
}