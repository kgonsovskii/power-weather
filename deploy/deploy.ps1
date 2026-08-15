#Requires -Version 5.1
<#
.SYNOPSIS
  SSH на Ubuntu: копирует remote-install.sh и запускает его (clone → install.sh).

.NOTES
  Пароль SSH — User env SERVER_PASSWORD.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# --- SSH target (hardcoded) ---
$ServerHost = "109.207.174.9"
$ServerUser = "root"

$RemoteInstallPath = Join-Path $PSScriptRoot "remote-install.sh"

function Ensure-PoshSsh {
    if (Get-Module -ListAvailable -Name Posh-SSH) {
        Import-Module Posh-SSH -ErrorAction Stop
        return
    }

    Write-Host "Installing Posh-SSH module (CurrentUser)..."
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope CurrentUser | Out-Null
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Install-Module -Name Posh-SSH -Scope CurrentUser -Force -AllowClobber
    Import-Module Posh-SSH -ErrorAction Stop
}

function Get-ServerPassword {
    $password = [Environment]::GetEnvironmentVariable("SERVER_PASSWORD", "User")
    if ([string]::IsNullOrWhiteSpace($password)) {
        $password = $env:SERVER_PASSWORD
    }

    if ([string]::IsNullOrWhiteSpace($password)) {
        throw "SERVER_PASSWORD is not set."
    }

    return $password
}

if (-not (Test-Path $RemoteInstallPath)) {
    throw "Missing script: $RemoteInstallPath"
}

Ensure-PoshSsh

$password = Get-ServerPassword
$secure = ConvertTo-SecureString $password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential ($ServerUser, $secure)

Write-Host "Connecting to ${ServerUser}@${ServerHost}..."
$session = New-SSHSession -ComputerName $ServerHost -Credential $credential -AcceptKey -Force -ConnectionTimeout 60
if (-not $session) {
    throw "SSH session was not created."
}

try {
    Write-Host "Uploading remote-install.sh..."
    Set-SCPItem -ComputerName $ServerHost -Credential $credential -Path $RemoteInstallPath -Destination "/tmp" -AcceptKey -Force

    $name = Split-Path $RemoteInstallPath -Leaf
    $remoteCommand = "chmod +x /tmp/$name && /tmp/$name $ServerHost"

    Write-Host "Running /tmp/$name $ServerHost ..."
    $result = Invoke-SSHCommand -SessionId $session.SessionId -Command $remoteCommand -TimeOut 1200
    if ($result.Output) {
        Write-Host $result.Output
    }
    if ($result.Error) {
        Write-Host $result.Error
    }
    if ($result.ExitStatus -ne 0) {
        throw "Server install failed with exit code $($result.ExitStatus)"
    }

    Write-Host "OK: http://${ServerHost}/"
}
finally {
    Remove-SSHSession -SessionId $session.SessionId | Out-Null
}
