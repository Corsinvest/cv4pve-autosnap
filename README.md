# <img src="icon.png" alt="" height="36" align="top"> cv4pve-autosnap

```
     ______                _                      __
    / ____/___  __________(_)___ _   _____  _____/ /_
   / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
  / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
  \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Automatic Snapshot Tool for Proxmox VE (Made in Italy)
```

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-autosnap.svg?style=flat-square)](LICENSE.md)
[![Release](https://img.shields.io/github/release/Corsinvest/cv4pve-autosnap.svg?style=flat-square)](https://github.com/Corsinvest/cv4pve-autosnap/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Corsinvest/cv4pve-autosnap/total.svg?style=flat-square&logo=download)](https://github.com/Corsinvest/cv4pve-autosnap/releases)
[![NuGet](https://img.shields.io/nuget/v/Corsinvest.ProxmoxVE.AutoSnap.Api.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Corsinvest.ProxmoxVE.AutoSnap.Api/)
[![WinGet](https://img.shields.io/winget/v/Corsinvest.cv4pve.autosnap?style=flat-square&logo=windows)](https://winstall.app/apps/Corsinvest.cv4pve.autosnap)
[![AUR](https://img.shields.io/aur/version/cv4pve-autosnap?style=flat-square&logo=archlinux)](https://aur.archlinux.org/packages/cv4pve-autosnap)

> **Automatic snapshots of Proxmox VE VMs and containers, with retention**: one command takes a snapshot of the guests you choose and removes the oldest ones, so every label keeps exactly the number you set.
>
> **[Documentation](https://corsinvest.github.io/cv4pve-autosnap/)**
>
> Prefer a web interface with schedules, run history and webhooks? cv4pve-autosnap also runs inside [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin), as its [AutoSnap](https://corsinvest.github.io/cv4pve-admin/modules/autosnap/) module.

---

## Why

A snapshot is the quickest way back after a broken upgrade or a bad change inside a guest. Proxmox VE takes one in a click, but only when someone remembers to click, and it never removes the old ones.

cv4pve-autosnap takes the snapshot of every guest you select and removes the oldest ones of the same label: *every two hours, keep 10* stays at ten snapshots per guest. Run it from cron or the Task Scheduler, or right before an upgrade.

It **runs outside the nodes and uses only the Proxmox VE API**: nothing to install on the cluster, no SSH, no root shell.

> **A snapshot is not a backup.** It lives on the same storage as the disk: if the storage is lost, so are its snapshots. Keep your [Proxmox VE backups](https://pve.proxmox.com/wiki/Backup_and_Restore).

---

## What it looks like

```
$ cv4pve-autosnap --host=pve01 --api-token='autosnap@pve!snap=…' --vmid=@all status
+-------+------+-------------------+-------------------------+-------------------------+-----------------+-----------+
| NODE  |   VM | TIME              | PARENT                  | NAME                    | DESCRIPTION     | VM STATUS |
+-------+------+-------------------+-------------------------+-------------------------+-----------------+-----------+
| pve01 |  105 | 26/09/28 07:00:02 | before-upgrade          | auto2hourly260928070002 | cv4pve-autosnap |           |
| pve01 |  105 | 26/09/28 09:00:04 | auto2hourly260928070002 | auto2hourly260928090004 | cv4pve-autosnap |           |
| pve01 | 1000 | 26/09/28 07:00:03 | no-parent               | auto2hourly260928070002 | cv4pve-autosnap |           |
| pve02 |  203 | 26/09/28 07:00:57 | no-parent               | auto2hourly260928070002 | cv4pve-autosnap |           |
+-------+------+-------------------+-------------------------+-------------------------+-----------------+-----------+
```

---

## Features

- **Retention per label**: `hourly`, `daily`, `weekly` or any name: each label keeps its own number of snapshots per guest.
- **Choose the guests**: by ID, name, range, node, pool or tag, with exclusions; resolved at every run, so a migrated or newly tagged guest is picked up with no change.
- **Storage guard**: a guest is skipped when a storage holding its disks is used above 95% (or your threshold).
- **Hook scripts**: your script runs at every phase, with guest, label and result in environment variables; ready-made templates and a metrics sender in [hooks/](hooks/).
- **Consistent snapshots**: optional RAM state with `--state`; warns when a VM has the QEMU guest agent off.
- **Parallel**: several guests at the same time with `--max-parallel`.
- **Dry run**: `--dry-run` shows what would be created and removed.
- **Keeps running with a node down**: give it more than one host and it uses the first that answers.

---

## Quick start

```bash
# Windows
winget install Corsinvest.cv4pve.autosnap

# Linux (other platforms and packages: see the documentation)
wget https://github.com/Corsinvest/cv4pve-autosnap/releases/latest/download/cv4pve-autosnap-linux-x64.zip
unzip cv4pve-autosnap-linux-x64.zip && chmod +x cv4pve-autosnap

# See what would happen, then take the snapshots
./cv4pve-autosnap --host=pve1.local --api-token='autosnap@pve!snap=<uuid>' --vmid=@all --dry-run snap --label=daily --keep=7
./cv4pve-autosnap --host=pve1.local --api-token='autosnap@pve!snap=<uuid>' --vmid=@all snap --label=daily --keep=7
```

Global options (`--host`, `--vmid`, `--max-parallel`, …) go **before** the command, command options after it. The API token needs the four privileges listed in [Permissions](https://corsinvest.github.io/cv4pve-autosnap/permissions/).

---

## Documentation

| | |
|---|---|
| [Getting started](https://corsinvest.github.io/cv4pve-autosnap/getting-started/) | Install, connect, first run |
| [Permissions](https://corsinvest.github.io/cv4pve-autosnap/permissions/) | Creating the user and API token, required privileges |
| [Choosing guests](https://corsinvest.github.io/cv4pve-autosnap/guests/) | IDs, names, pools, tags, nodes, exclusions |
| [Labels and retention](https://corsinvest.github.io/cv4pve-autosnap/retention/) | Snapshot names, `--keep`, `clean`, `status` |
| [Scheduling](https://corsinvest.github.io/cv4pve-autosnap/scheduling/) | cron, Task Scheduler, parameter files |
| [Snapshot consistency](https://corsinvest.github.io/cv4pve-autosnap/consistency/) | `--state`, QEMU guest agent, fsfreeze hooks for databases |
| [Hook scripts](https://corsinvest.github.io/cv4pve-autosnap/hooks/) | Phases, variables, ready-made scripts |
| [Commands](https://corsinvest.github.io/cv4pve-autosnap/commands/) | Every option, defaults, exit codes |
| [AI assistants](https://corsinvest.github.io/cv4pve-autosnap/ai-agents/) | Claude Code, Codex, the `cv4pve-autosnap` skill |
| [Troubleshooting](https://corsinvest.github.io/cv4pve-autosnap/troubleshooting/) | What the messages mean, debug output |

---

## Related tools

Use `cv4pve-autosnap` for recent restore points of the guests, [cv4pve-node-protect](https://github.com/Corsinvest/cv4pve-node-protect) to save the configuration of the nodes. The whole suite: [corsinvest.it/cv4pve](https://www.corsinvest.it/en/cv4pve/).

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/en/cv4pve/).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
