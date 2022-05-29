# SPDX-License-Identifier: GPL-3.0-only
# SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl

[System.Console]::Clear();

Write-Output "
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

                                    (Made in Italy)

 =========================================================
 == Build System
 ========================================================="

$pathNet = "Bin\Release\net6.0"

Remove-Item -Path ".\$pathNet"  -Recurse -Force

$rids = @("linux-x64", "linux-arm", "linux-arm64", "osx-x64", "win-x86", "win-x64", "win-arm", "win-arm64")
foreach ($rid in $rids) {
    dotnet publish -r $rid -c Release /p:PublishSingleFile=true --self-contained #/p:EnableCompressionInSingleFile=true
    $path = "$pathNet\$rid\publish\"

    $fileName = Get-ChildItem $path -Exclude *.pdb -name
    $fileDest = "$pathNet\$fileName-$rid.zip"
    Remove-Item $fileDest -ErrorAction SilentlyContinue
    Compress-Archive $path\$fileName $fileDest
}