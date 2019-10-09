# This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
# Copyright (C) 2016 Corsinvest Srl
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <http://www.gnu.org/licenses/>.

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

$rids = @("linux-x64", "linux-arm", "linux-arm64", "osx-x64", "win-x86", "win-x64", "win-arm", "win-arm64")
foreach ($rid in $rids) {
    dotnet publish -r $rid -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
    $path = "bin\Release\netcoreapp3.0\$rid\publish\"
    $fileName = Get-ChildItem $path -Exclude *.pdb -name
    $fileDest = "bin\Release\netcoreapp3.0\$fileName-$rid.zip"   
    Remove-Item $fileDest -ErrorAction SilentlyContinue
    Compress-Archive $path\$fileName $fileDest
}