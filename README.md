# cv4pve-autosnap

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-autosnap.svg)](https://www.gnu.org/licenses/gpl-3.0.en.html)
[![Gitter](https://badges.gitter.im/Corsinvest/cv4pve-autosnap.svg)](https://gitter.im/Corsinvest/cv4pve-autosnap)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-autosnap.svg)](https://github.com/Corsinvest/cv4pve-autosnap/releases/latest)
![GitHub All Releases](https://img.shields.io/github/downloads/Corsinvest/cv4pve-autosnap/total.svg) [![Donate to this project using Paypal](https://img.shields.io/badge/paypal-donate-yellow.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=PPM9JHLQLRV2S&item_name=Open+Source+Project&currency_code=EUR&source=url)

Proxmox VE automatic snapshot tool

[More information about cv4pve-autosnap](http://www.corsinvest.it/continuous-protection-data-proxmox-ve/)

[More information about Qemu guest agent](https://pve.proxmox.com/wiki/Qemu-guest-agent)

[Nuget Api](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.AutoSnap.Api)

## The old bash version inside Proxmox VE is no longer supported because the Proxmox VE developers continue to change output. The risk of incompatibility is high. With the new version that uses native APIs, the problem no longer exists

# **Donations**

If you like my work and want to support it, then please consider to deposit a donation through **Paypal** by clicking on the next button:

[![paypal](https://www.paypalobjects.com/en_US/IT/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=PPM9JHLQLRV2S&item_name=Open+Source+Project&currency_code=EUR&source=url)

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
  --username    User name <username>@<realm>
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

## Main features

* Completely rewritten in C#
* Use native api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macosx)
* Installation
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
* Multiple VM/CT (100,102,ubuvm,debvm,pipperovm,fagianovm or all) in a single execution
* Exclusion specific VM/CT using minus e.g --vmid=all,-100
* Exclusion template from snapshot
* Waiting for the snapshot process to finish
* Alerting in QEMU, agent not enabled.

## Configuration and use

E.g. install on debian package

Download last package e.g. Debian cv4pve-autosnap_?.?.?-?_all.deb, on your os and install:

```sh
root@debian:~# dpkg -i cv4pve-autosnap_?.?.?-?_all.deb
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

## Hook script

Before run hook script, program create environment variable:

```sh
CV4PVE_AUTOSNAP_PHASE
CV4PVE_AUTOSNAP_VMID
CV4PVE_AUTOSNAP_VMTYPE
CV4PVE_AUTOSNAP_LABEL
CV4PVE_AUTOSNAP_KEEP
CV4PVE_AUTOSNAP_SNAP_NAME
CV4PVE_AUTOSNAP_VMSTATE
```
