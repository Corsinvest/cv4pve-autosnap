#!/bin/bash
clear

cat << EOF
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

 Corsinvest for Proxmox VE Auto Snapshot    (Made in Italy)

=========================================================
== Build System
=========================================================
EOF

dotnet restore

#linux
rids="linux-x64"

for rid in $rids; do
   dotnet publish -r $rid -c Release
   dotnet tarball -f netcoreapp2.2 -r $rid -c Release
   dotnet rpm -f netcoreapp2.2 -r $rid -c Release
   dotnet zip -f netcoreapp2.2 -r $rid -c Release
done

#debian 
rids="debian-x64 debian-arm debian-arm64"

for rid in $rids; do
   dotnet publish -r $rid -c Release
   dotnet deb -f netcoreapp2.2 -r $rid -c Release
done

rids="win-x64 win-x86 osx-x64"

for rid in $rids; do
   dotnet publish -r $rid -c Release
   dotnet zip -f netcoreapp2.2 -r $rid -c Release
done