# SPDX-FileCopyrightText: Copyright Corsinvest Srl
# SPDX-License-Identifier: GPL-3.0-only

..\..\..\cmds\Set-ApiKey.ps1

#Read project version
$xml = [xml](Get-Content .\src\Corsinvest.ProxmoxVE.AutoSnap.Api\Corsinvest.ProxmoxVE.AutoSnap.Api.csproj)
$version = $xml.Project.PropertyGroup.Version
Write-Host "Project version: $version"

dotnet publish -c Release

dotnet nuget push ..\.nupkgs\Corsinvest.ProxmoxVE.AutoSnap.Api.$version.nupkg --api-key $ENV:NugetApiKey --source https://api.nuget.org/v3/index.json