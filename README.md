# cv4pve-autosnap

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

> **Automatic snapshot management for Proxmox VE** — create, retain and clean snapshots of VM/CT with a single command.

---

## Quick Start

```bash
wget https://github.com/Corsinvest/cv4pve-autosnap/releases/download/VERSION/cv4pve-autosnap-linux-x64.zip
unzip cv4pve-autosnap-linux-x64.zip
./cv4pve-autosnap --host=YOUR_HOST --api-token=user@realm!token=uuid --vmid=100 snap --label=daily --keep=7
```

---

## Installation

| Platform           | Command                                                                                                             |
| ------------------ | ------------------------------------------------------------------------------------------------------------------- |
| **Linux**          | `wget .../cv4pve-autosnap-linux-x64.zip && unzip cv4pve-autosnap-linux-x64.zip && chmod +x cv4pve-autosnap`        |
| **Windows WinGet** | `winget install Corsinvest.cv4pve.autosnap`                                                                         |
| **Windows manual** | Download `cv4pve-autosnap-win-x64.zip` from [Releases](https://github.com/Corsinvest/cv4pve-autosnap/releases)     |
| **Arch Linux**     | `yay -S cv4pve-autosnap`                                                                                            |
| **Debian/Ubuntu**  | `sudo dpkg -i cv4pve-autosnap-VERSION-ARCH.deb`                                                                     |
| **RHEL/Fedora**    | `sudo rpm -i cv4pve-autosnap-VERSION-ARCH.rpm`                                                                      |
| **macOS**          | Homebrew: `brew tap Corsinvest/homebrew-tap && brew install cv4pve-autosnap` ([tap repo](https://github.com/Corsinvest/homebrew-tap))<br/>Manual: `wget .../cv4pve-autosnap-osx-x64.zip && unzip cv4pve-autosnap-osx-x64.zip && chmod +x cv4pve-autosnap` |

All binaries on the [Releases page](https://github.com/Corsinvest/cv4pve-autosnap/releases).

---

## Features

- **Self-contained binary** — no runtime to install, copy and run
- **Cross-platform** — Windows, Linux, macOS
- **API-based** — no root or SSH access required
- **Cluster-aware** — works across all nodes automatically
- **High availability** — multiple host support for automatic failover
- **Flexible targeting** — select VMs by ID, name, pool, tag, node or pattern
- **Retention policies** — configurable keep count per label
- **Multiple schedules** — use labels (hourly, daily, weekly, monthly)
- **Memory state** — optional RAM state preservation with `--state`
- **Hook scripts** — custom automation before/after each phase
- **Storage monitoring** — skip snapshot if storage above threshold
- **QEMU Guest Agent** — warns if not enabled on a VM; recommended for consistent snapshots ([setup guide](docs/snapshot-consistency.md))
- **API token** support (Proxmox VE 6.2+)
- **Dry-run** mode — test without making changes

---

<details>
<summary><strong>Security &amp; Permissions</strong></summary>

### Required Permissions

| Permission          | Purpose                            | Scope            |
| ------------------- | ---------------------------------- | ---------------- |
| **VM.Audit**        | Read VM/CT configuration and status | Virtual machines |
| **VM.Snapshot**     | Create and delete snapshots        | Virtual machines |
| **Datastore.Audit** | Check storage capacity             | Storage systems  |
| **Pool.Allocate**   | Access pool information            | Resource pools   |

</details>

---

## Usage

```bash
# Create snapshots with retention
cv4pve-autosnap --host=pve.local --api-token=user@realm!token=uuid --vmid=100 snap --label=daily --keep=7

# Clean old snapshots
cv4pve-autosnap --host=pve.local --api-token=user@realm!token=uuid --vmid=100 clean --label=daily --keep=7

# View snapshot status
cv4pve-autosnap --host=pve.local --api-token=user@realm!token=uuid --vmid=100 status

# Dry-run (no changes)
cv4pve-autosnap --host=pve.local --api-token=user@realm!token=uuid --vmid=100 --dry-run snap --label=daily --keep=7

# Parameter file (recommended for complex setups)
cv4pve-autosnap @/etc/cv4pve/production.conf snap --label=daily --keep=14
```

---

## VM/CT Selection

The `--vmid` parameter supports powerful pattern matching:

| Pattern              | Example                        | Description                      |
| -------------------- | ------------------------------ | -------------------------------- |
| Single ID            | `--vmid=100`                   | Specific VM/CT by ID             |
| Single name          | `--vmid=web-server`            | Specific VM/CT by name           |
| Multiple             | `--vmid=100,101,web-server`    | Comma-separated list             |
| Range                | `--vmid=100:110`               | Range of IDs (inclusive)         |
| Wildcard             | `--vmid=%web%`                 | Contains pattern                 |
| All VMs              | `--vmid=@all`                  | All VMs in cluster               |
| Pool                 | `--vmid=@pool-production`      | All VMs in specific pool         |
| Tag                  | `--vmid=@tag-backup`           | All VMs with specific tag        |
| Node                 | `--vmid=@node-pve1`            | All VMs on specific node         |
| Exclusion            | `--vmid=@all,-100,-test-vm`    | Exclude specific VMs             |
| Tag exclusion        | `--vmid=@all,-@tag-test`       | Exclude VMs with tag             |

---

## Scheduling with Cron

```bash
# Daily snapshot at 2 AM (keep 7 days)
0 2 * * * /usr/local/bin/cv4pve-autosnap --host=pve.local --api-token=backup@pve!daily=uuid --vmid=@tag-production snap --label=daily --keep=7

# Weekly snapshot on Sunday at 3 AM (keep 4 weeks)
0 3 * * 0 /usr/local/bin/cv4pve-autosnap --host=pve.local --api-token=backup@pve!weekly=uuid --vmid=@all snap --label=weekly --keep=4

# Monthly cleanup on 1st at 4 AM
0 4 1 * * /usr/local/bin/cv4pve-autosnap --host=pve.local --api-token=backup@pve!clean=uuid --vmid=@all clean --label=monthly --keep=12
```

---

## Hook Scripts

Hook scripts receive environment variables for custom automation:

```bash
CV4PVE_AUTOSNAP_PHASE        # snap-create-pre, snap-create-post, snap-remove-pre, snap-remove-post, ...
CV4PVE_AUTOSNAP_VMID         # VM/CT ID
CV4PVE_AUTOSNAP_VMNAME       # VM/CT name
CV4PVE_AUTOSNAP_VMTYPE       # qemu or lxc
CV4PVE_AUTOSNAP_LABEL        # Snapshot label
CV4PVE_AUTOSNAP_KEEP         # Retention count
CV4PVE_AUTOSNAP_SNAP_NAME    # Snapshot name
CV4PVE_AUTOSNAP_VMSTATE      # Memory state included (1/0)
CV4PVE_AUTOSNAP_DURATION     # Operation duration in seconds
CV4PVE_AUTOSNAP_STATE        # Operation status (1/0)
CV4PVE_AUTOSNAP_DEBUG        # Debug mode (1/0)
CV4PVE_AUTOSNAP_DRY_RUN      # Dry-run mode (1/0)
```

<details>
<summary><strong>Available phases</strong></summary>

| Phase                | Description                            |
| -------------------- | -------------------------------------- |
| `snap-job-start`     | Before the entire snap job starts      |
| `snap-job-end`       | After the entire snap job ends         |
| `snap-create-pre`    | Before creating a snapshot for a VM    |
| `snap-create-post`   | After creating a snapshot for a VM     |
| `snap-create-abort`  | When snapshot creation fails           |
| `snap-remove-pre`    | Before removing an old snapshot        |
| `snap-remove-post`   | After removing an old snapshot         |
| `snap-remove-abort`  | When snapshot removal fails            |
| `clean-job-start`    | Before the entire clean job starts     |
| `clean-job-end`      | After the entire clean job ends        |

</details>

#### Example Hook Script

```bash
#!/bin/bash
case $CV4PVE_AUTOSNAP_PHASE in
    "snap-create-pre")
        echo "Starting snapshot for VM $CV4PVE_AUTOSNAP_VMID"
        ;;
    "snap-create-post")
        echo "Completed snapshot $CV4PVE_AUTOSNAP_SNAP_NAME"
        ;;
esac
```

#### Using Hook Scripts

```bash
cv4pve-autosnap --host=pve.local --api-token=token --vmid=100 snap --label=daily --keep=7 --script-hook=/opt/scripts/hook.sh
```

Ready-to-use templates (Bash, PowerShell, Windows Batch) are available in the [hooks/](hooks/) folder.

> For snapshot consistency with running databases (MySQL, PostgreSQL) and QEMU Guest Agent setup, see [docs/snapshot-consistency.md](docs/snapshot-consistency.md).

---

## Resources

[![cv4pve-autosnap Tutorial](http://img.youtube.com/vi/kM5KhD9seT4/maxresdefault.jpg)](https://www.youtube.com/watch?v=kM5KhD9seT4)

**Web GUI version:** [cv4pve-admin](https://github.com/Corsinvest/cv4pve-admin)

---

## Support

Professional support and consulting available through [Corsinvest](https://www.corsinvest.it/cv4pve).

---

Part of [cv4pve](https://www.corsinvest.it/cv4pve) suite | Made with ❤️ in Italy by [Corsinvest](https://www.corsinvest.it)

Copyright © Corsinvest Srl
