[System.Console]::Clear();

Write-Output "
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

 Corsinvest for Proxmox VE Auto Snapshot    (Made in Italy)

 =========================================================
 == Build System
 =========================================================
 "

# function Build($rids, $arch, $exec) {
#     $warpExec = "$HOME\.dotnet\tools\.store\dotnet-warp\1.0.9\dotnet-warp\1.0.9\tools\netcoreapp2.1\any\warp\windows-x64.warp-packer.exe";

#     For ($i = 0; $i -lt $rids.Length; $i++) {
#         $rid = $rids[$i]
#         $binDir = "bin\Upload\$rid"
    
#         "======= Build $rid ======= "
#         "Publishing"
#         dotnet publish -c Release -r $rid -o $binDir | Out-Null

#         & $warpExec --arch $arch --input_dir $binDir --exec $exec --output "bin\Upload\$rid-$exec"
#         Remove-Item -Path bin\upload\$rid -Recurse -Force
#     }   
# }

# #Build @("osx-x64") "macos-x64" "cv4pve-autosnap"
# Build @("linux-x64", "linux-arm", "linux-arm64") "linux-x64" "cv4pve-autosnap"
# #Build @("win-x64", "win-x86", "win-arm", "win-arm64") "windows-x64" "cv4pve-autosnap.exe"

#linux
$rids = @("linux-x64", "linux-arm", "linux-arm64")
For ($i = 0; $i -lt $rids.Length; $i++) {
    $rid = $rids[$i]

    dotnet publish -r $rid -c Release
    dotnet tarball -f netcoreapp2.2 -r $rid -c Release
    dotnet rpm -f netcoreapp2.2 -r $rid -c Release
    dotnet zip -f netcoreapp2.2 -r $rid -c Release
}

#debian 
$rids = @("debian-x64", "debian-arm", "debian-arm64")
For ($i = 0; $i -lt $rids.Length; $i++) {
    $rid = $rids[$i]
    dotnet publish -r $rid -c Release
    dotnet deb -f netcoreapp2.2 -r $rid -c Release
}

#other
$rids = @("osx-x64", "win-x86", "win-x64", "win-arm", "win-arm64")
For ($i = 0; $i -lt $rids.Length; $i++) {
    $rid = $rids[$i]
    dotnet publish -r $rid -c Release
    dotnet zip -f netcoreapp2.2 -r $rid -c Release
}