# eve4pve-autosnap
Proxmox automatic snapshot tool
```
    ______      __                       _              _    ________
   / ____/___  / /____  _________  _____(_)_______     | |  / / ____/
  / __/ / __ \/ __/ _ \/ ___/ __ \/ ___/ / ___/ _ \    | | / / __/
 / /___/ / / / /_/  __/ /  / /_/ / /  / (__  )  __/    | |/ / /___
/_____/_/ /_/\__/\___/_/  / .___/_/  /_/____/\___/     |___/_____/
                         /_/
                         
EnterpriseVE automatic snapshot for Proxmox         (Made in Italy)

Usage:
    eve4pve-autosnap <COMMAND> [ARGS] [OPTIONS]
    eve4pve-autosnap help
    eve4pve-autosnap version

    eve4pve-autosnap create  --vmid=<integer> --label=<string> --keep=<integer>
                             --vmstate --script=<string> --syslog 
    eve4pve-autosnap destroy --vmid=<integer> --label=<string>
    eve4pve-autosnap enable  --vmid=<integer> --label=<string>
    eve4pve-autosnap disable --vmid=<integer> --label=<string>

    eve4pve-autosnap status
    eve4pve-autosnap clean   --vmid=<integer> --label=<string> --keep=<integer>

    eve4pve-autosnap snap    --vmid=<integer> --label=<string> --keep=<integer>
                             --vmstate --script=<string> --syslog 

Commands:
    version                  Show version program.
    help                     Show help program.
    create                   Create snap job from scheduler.
    destroy                  Remove snap job from scheduler.
    enable                   Enable snap job from scheduler.
    disable                  Disable snap job from scheduler.
    status                   Get list of all auto snapshots.
    clean                    Remove all auto snapshots.
    snap                     Will snap one time.

Options:
    --vmid=integer           The ID of the VM.
    --vmstate                Save the vmstate only qemu.
    --label=string           Is usually 'hourly', 'daily', 'weekly', or 'monthly'.
    --keep=integer           Specify the number of snapshots which should will keep, 
                             anything longer will be removed. Default 1.
                             (-1 will disable removing snapshots)
    --script=string          Use specified hook script.
                             Es. /usr/share/doc/eve4pve-autosnap/examples/script-hook.sh
    --syslog                 Write messages into the system log.

Report bugs to <support@enterpriseve.com>.
```

# Introduction
Automatic snapshot for Proxmox with retention.

# Main features
* Can keep multiple snapshots
* Syslog integration
* Clean all snapshots
* Multiple schedule VM using --label (es. daily,monthly)
* Hook script

# Configuration and use
Download package eve4pve-autosnap_?.?.?-?_all.deb, on your Proxmox VE host and install:
```
wget https://github.com/EnterpriseVE/pve-autosnap/?????
dpkg -i eve4pve-autosnap_?.?.?-?_all.deb
```
This tool need basically no configuration.

## Snap a VM one time

```
root@pve1:~# eve4pve-autosnap snap --vmid=111 --label='daily' --keep=5
```
This command snap VM 111. The --keep tells that it should be kept 2 snapshots, if there are more than 2 snapshots, the 3 one will be erased (sorted by creation time).
## Create a recurring snap job
```
root@pve1:~# eve4pve-autosnap create --vmid=111 --label='daily' --keep=5
```

## Delete a recurring snap job
```
root@pve1:~# eve4pve-autosnap destroy --vmid=111 --label='daily' --keep=5
```

## Pause a snap job
```
root@pve1:~# eve4pve-autosnap disable --vmid=111 --label='daily' --keep=5
```

## Reactivate a snap job
```
root@pve1:~# eve4pve-autosnap enable --vmid=111 --label='daily' --keep=5
```

## Changing parameters
You can edit the configuration in /etc/cron.d/eve4pve-autosnap or destroy the job and create it new.

