# cv4pve-autosnap

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-autosnap.svg)](LICENSE.md) [![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-autosnap.svg)](https://github.com/Corsinvest/cv4pve-autosnap/releases/latest) ![GitHub All Releases](https://img.shields.io/github/downloads/Corsinvest/cv4pve-autosnap/total.svg) [![AppVeyor branch](https://img.shields.io/appveyor/ci/franklupo/cv4pve-autosnap/master.svg)](https://ci.appveyor.com/project/franklupo/cv4pve-autosnap)

Proxmox VE automatic snapshot tool

[More information about cv4pve-autosnap](http://www.corsinvest.it/continuous-protection-data-proxmox-ve/)

[More information about Qemu guest agent](https://pve.proxmox.com/wiki/Qemu-guest-agent)

## The old bash version inside Proxmox VE is no longer supported because the Proxmox VE developers continue to change output. The risk of incompatibility is high. With the new version that uses native APIs, the problem no longer exists

```text
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

 Corsinvest for Proxmox VE Auto Snapshot    (Made in Italy)

Usage: cv4pve-autosnap [options] [command]

Options:
  -?|-h|--help      Show help information
  --version         Show version information
  --host            The host name host[:port],host1[:port],host2[:port]
  --username        User name <username>@<realm>
  --password        The password. Specify 'file:path_file' to store password in file.
  --vmid            The id or name VM/CT comma separated (eg. 100,101,102,TestDebian)
                    -vmid or -name exclude (e.g. -200,-TestUbuntu)
                    'all-???' for all VM/CT in specific host (e.g. all-pve1, all-\$(hostname)),
                    'all' for all VM/CT in cluster
  --timeout         Timeout operation in seconds

Commands:
  app-check-update  Check update application
  app-upgrade       Upgrade application
  clean             Remove auto snapshots
  snap              Will snap one time
  status            Get list of all auto snapshots

Run 'cv4pve-autosnap [command] --help' for more information about a command.

cv4pve-autosnap is a part of suite cv4pve-tools.
For more information visit https://www.cv4pve-tools.com
```

## Copyright and License

Copyright: Corsinvest Srl
For licensing details please visit [LICENSE.md](LICENSE.md)

## Commercial Support

This software is part of a suite of tools called cv4pve-tools. If you want commercial support, visit the [site](https://www.cv4pve-tools.com)

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

## Main features

* Completely written in C#
* Use native Api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macosx)
* Installation rapid, unzip file extract binary
* Not require installation in Proxmox VE
* Execute out side Proxmox VE
* For KVM and LXC
* Work for single node or cluster, automatically resolve VM/CT id/name
* Use id or name with --vmid parameter
* Can keep multiple snapshots --keep
* Clean all snapshots
* Multiple schedule VM/CT using --label (es. daily,monthly)
* Hook script
* Multiple VM/CT (100,102,ubuVm,debVm,pipperoVm,fagianoVm or all) in a single execution
* Exclusion specific VM/CT using minus e.g --vmid=all,-100
* Exclusion template from snapshot
* Waiting for the snapshot process to finish
* Alerting in QEMU, agent not enabled.
* Save memory VM Qemu in snap using parameter --state.
* No stop on error
* Script hook for [metrics](https://github.com/Corsinvest/cv4pve-metrics)
* Timeout remove/create snapshot in --timeout parameter specify in second
* Support multiple host for HA in --host parameter es. host[:port],host1[:port],host2[:port]
* Multiple output text,unicode,unicodeAlt,markdown,html
* Check-Update and Upgrade application

## Configuration and use

E.g. install on linux 64

Download last package e.g. Debian cv4pve-autosnap-linux-x64.zip, on your os and install:

```sh
root@debian:~# unzip cv4pve-autosnap-linux-x64.zip
```

This tool need basically no configuration.

## Snapshot a VM/CT one time

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano --vmid=111 snap --label='daily' --keep=2
```

This command snap VM 111.

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano --vmid="all,-111" snap --label='daily' --keep=2
```

This command snap all VMs except 111.

The --keep tells that it should be kept 2 snapshots, if there are more than 2 snapshots, the 3 one will be erased (sorted by creation time).
The --state save memory VM.
The --timeout specify the timeout remove/creation snapshot.

When create a snapshot the software add "auto" prefix of the name the snapshot.

## Clean a VM/CT one time

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano --vmid=111 clean --label='4hours' --keep=2
----- VM 100 -----
Remove snapshot: auto4hours190617080002
Remove snapshot: auto4hours190617120002
Remove snapshot: auto4hours190617160002
Remove snapshot: auto4hours190617200002
```

## Status snapshots

```sh
root@debian:~# cv4pve-autosnap --host=192.168.0.100 --username=root@pam --password=fagiano --vmid=100 status

┌──────┬─────┬───────────────────┬──────────────────────────────┬──────────────────────────────┬─────────────────┬─────┐
│ NODE │ VM  │ TIME              │ PARENT                       │ NAME                         │ DESCRIPTION     │ RAM │
├──────┼─────┼───────────────────┼──────────────────────────────┼──────────────────────────────┼─────────────────┼─────┤
│ pve1 │ 100 │ 19/08/28 09:21:35 │ no-parent                    │ auto4hours190828112133       │ cv4pve-autosnap │     │
│ pve1 │ 100 │ 19/08/28 12:23:09 │ auto4hours190828112133       │ auto4hours190828142307       │ cv4pve-autosnap │     │
│ pve1 │ 100 │ 19/08/29 06:50:23 │ auto4hours190828142307       │ auto4hours190829085021       │ cv4pve-autosnap │     │
│ pve1 │ 100 │ 19/08/29 07:32:15 │ auto4hours190829085021       │ auto4hours190829093214       │ cv4pve-autosnap │     │
└──────┴─────┴───────────────────┴──────────────────────────────┴──────────────────────────────┴─────────────────┴─────┘
```

Output type:

```Text
-o|--output   Type output (default: text) Text,Json,JsonPretty

```

## Hook script

Before run hook script, program create environment variable:

```sh
CV4PVE_AUTOSNAP_PHASE
CV4PVE_AUTOSNAP_VMID
CV4PVE_AUTOSNAP_VMTYPE
CV4PVE_AUTOSNAP_LABEL
CV4PVE_AUTOSNAP_KEEP
CV4PVE_AUTOSNAP_SNAP_NAME
CV4PVE_AUTOSNAP_VMSTATE     #1/0
CV4PVE_AUTOSNAP_DEBUG       #1/0
CV4PVE_AUTOSNAP_DRY_RUN     #1/0
```

See example hook file script-hook.bat, script-hook.sh
