# cv4pve-autosnap

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-autosnap.svg)](https://www.gnu.org/licenses/gpl-3.0.en.html)
[![Gitter](https://badges.gitter.im/Corsinvest/cv4pve-autosnap.svg)](https://gitter.im/Corsinvest/cv4pve-autosnap)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-autosnap.svg)](https://github.com/Corsinvest/cv4pve-autosnap/releases/latest)
![GitHub All Releases](https://img.shields.io/github/downloads/Corsinvest/cv4pve-autosnap/total.svg)

Proxmox VE automatic snapshot tool

[More information about cv4pve-autosnap](http://www.corsinvest.it/continuous-protection-data-proxmox-ve/)

[More information about Qemu guest agent](https://pve.proxmox.com/wiki/Qemu-guest-agent)

```text
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

 Corsinvest for Proxmox VE Auto Snapshot    (Made in Italy)

Automatic snapshot with retention

Usage: cv4pve-autosnap [options] [command]

Options:
  -?|-h|--help  Show help information
  --host        The host name host[:port]
  --username    User name <username>@<relam>
  --password    The password
  --vmid        The id or name VM/CT comma separated (eg. 100,101,102,TestDebian)
                -vmid or -name exclude (e.g. -200, -TestUbuntu),
                'all-???' for all VM/CT in specific host (e.g. all-pve1, all-\$(hostname)),
                'all' for all VM/CT in cluster

Commands:
  clean         Remove auto snapshots
  snap          Will snap one time
  status        Get list of all auto snapshots

Run 'cv4pve-autosnap [command] --help' for more information about a command.

Report bugs to <support@corsinvest.it>
```

## Introduction

Automatic snapshot for Proxmox VE with retention.

In this version the tool works outside the Proxmox VE host using the API. The reasons are:

* if the host does not work the tool does not work
* using the API, future changes are guaranteed
* Root access is not required, a user is required to perform the operation
* use of standard https / https json technology

For the planning process using an external machine:
[Docker crontab](https://hub.docker.com/r/willfarrell/crontab)
[Docker crontab with ui](https://hub.docker.com/r/alseambusher/crontab-ui)

For old version bash inside in Proxmox VE found in src/bash

## Main features

* Completely rewritten in C#
* Use native api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macos)
* Instalation
  * Portable all files request are included
  * Native installation (deb, rpm, zip)
* Not require installation in Proxmox VE
* Execute out side Proxmox VE
* For KVM and LXC
* Work for single node or cluster, automatically resolve VM/CT id/name
* Use id or name with --vmid parameter
* Can keep multiple snapshots --keep
* Clean all snapshots
* Multiple schedule VM/CT using --label (es. daily,monthly)
* Hook script
* Multiple VM/CT (100,102,ubuvm,debvm,pipperovm or all) in a single execution
* Exclusion specific VM/CT using minus e.g --vmid=all,-100
* Exclusion template from snapshot
* Waiting for the snapshot process to finish
* Alerting in QEMU, agent not enabled.

## Configuration and use

E.g. install on debian package

Download last package e.g. Debian cv4pve-autosnap_?.?.?-?_all.deb, on your os and install:

```sh
dpkg -i cv4pve-autosnap_?.?.?-?_all.deb
```

This tool need basically no configuration.

## Snapshot a VM/CT one time

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano snap --vmid=111 --label='daily' --keep=2
```

This command snap VM 111.

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano snap --vmid="all,-111" --label='daily' --keep=2
```

This command snap all VMs except 111.

The --keep tells that it should be kept 2 snapshots, if there are more than 2 snapshots, the 3 one will be erased (sorted by creation time).

## Clean a VM/CT one time

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano clean --vmid=111 --label='daily' --keep=2
----- VM 100 -----
Remove snapshot: auto4hours190617080002
Remove snapshot: auto4hours190617120002
Remove snapshot: auto4hours190617160002
Remove snapshot: auto4hours190617200002
```

## Status snapshots

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano status --vmid=100

NODE          VM TIME              PARENT                    NAME                      DESCRIPTION               RAM
pve1         100 19/06/17 06:00:04 no-parent                 auto4hours190617080002    cv4pve-autosnap
pve1         100 19/06/17 10:00:04 auto4hours190617080002    auto4hours190617120002    cv4pve-autosnap
pve1         100 19/06/17 14:00:04 auto4hours190617120002    auto4hours190617160002    cv4pve-autosnap
pve1         100 19/06/17 18:00:04 auto4hours190617160002    auto4hours190617200002    cv4pve-autosnap
pve1         100 19/06/17 22:00:04 auto4hours190617200002    auto4hours190618000002    cv4pve-autosnap
pve1         100 19/06/18 02:00:04 auto4hours190618000002    auto4hours190618040002    cv4pve-autosnap
pve1         100 19/06/18 06:00:04 auto4hours190618040002    auto4hours190618080002    cv4pve-autosnap
pve1         100 19/06/18 10:00:04 auto4hours190618080002    auto4hours190618120002    cv4pve-autosnap
pve1         100 19/06/18 14:00:04 auto4hours190618120002    auto4hours190618160002    cv4pve-autosnap
pve1         100 19/06/18 18:00:04 auto4hours190618160002    auto4hours190618200002    cv4pve-autosnap
pve1         100 19/06/18 22:00:04 auto4hours190618200002    auto4hours190619000002    cv4pve-autosnap
pve1         100 19/06/19 02:00:03 auto4hours190619000002    auto4hours190619040002    cv4pve-autosnap
pve1         100 19/06/19 06:00:04 auto4hours190619040002    auto4hours190619080002    cv4pve-autosnap
pve1         100 19/06/19 10:00:03 auto4hours190619080002    auto4hours190619120002    cv4pve-autosnap
pve1         100 19/06/19 14:00:04 auto4hours190619120002    auto4hours190619160002    cv4pve-autosnap
pve1         100 19/06/19 18:00:04 auto4hours190619160002    auto4hours190619200002    cv4pve-autosnap
pve1         100 19/06/20 02:00:04 auto4hours190619200002    auto4hours190620040002    cv4pve-autosnap
pve1         100 19/06/20 06:00:04 auto4hours190620040002    auto4hours190620080003    cv4pve-autosnap
pve1         100 19/06/20 10:00:04 auto4hours190620080003    auto4hours190620120002    cv4pve-autosnap
pve1         100 19/06/20 14:00:04 auto4hours190620120002    auto4hours190620160002    cv4pve-autosnap
pve1         100 19/06/20 18:00:04 auto4hours190620160002    auto4hours190620200002    cv4pve-autosnap
pve1         100 19/06/20 22:00:04 auto4hours190620200002    auto4hours190621000002    cv4pve-autosnap
pve1         100 19/06/21 02:00:04 auto4hours190621000002    auto4hours190621040002    cv4pve-autosnap
pve1         100 19/06/21 06:00:04 auto4hours190621040002    auto4hours190621080002    cv4pve-autosnap
```

## Hook script

Before run hook script, programa create enviroment variable:

```sh
CV4PVE_AUTOSNAP_PHASE
CV4PVE_AUTOSNAP_VMID
CV4PVE_AUTOSNAP_VMTYPE
CV4PVE_AUTOSNAP_LABEL
CV4PVE_AUTOSNAP_KEEP
CV4PVE_AUTOSNAP_SNAP_NAME
CV4PVE_AUTOSNAP_VMSTATE
```

## Package API

Contain all funtionality for autosnap, basic commands and shell execution. See source.
