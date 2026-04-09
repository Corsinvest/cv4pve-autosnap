# Snapshot Consistency and QEMU Guest Agent

When taking a snapshot of a running VM, it is basically like pulling the power cable from a server. Often this is not catastrophic — the next `fsck` will try to fix filesystem issues — but in the worst case it can leave you with a severely damaged filesystem, or worse, half-written inodes that lead to silent data corruption.

The **qemu-guest-agent** improves filesystem consistency during snapshots: Proxmox VE calls `guest-fsfreeze-freeze` before the snapshot and `guest-fsfreeze-thaw` after, syncing outstanding writes and halting all I/O. However, there may still be issues at the application layer — database processes may have unwritten data in memory. The fsfreeze hook lets you flush those services before the freeze.

Official references:
- [Qemu-guest-agent — Proxmox VE Wiki](https://pve.proxmox.com/wiki/Qemu-guest-agent)
- [VM Backup Consistency — Proxmox VE Wiki](https://pve.proxmox.com/wiki/VM_Backup_Consistency)

---

## 1. Enable QEMU Guest Agent

1. Install the agent inside the VM:
   - Debian/Ubuntu: `apt install qemu-guest-agent`
   - RHEL/Fedora: `dnf install qemu-guest-agent`
   - Windows: install VirtIO drivers package
2. Enable it in the Proxmox VE GUI: VM → Options → QEMU Guest Agent → Enabled
3. Make sure "Freeze/thaw guest filesystems on backup for consistency" is checked

> cv4pve-autosnap warns at runtime if the QEMU Guest Agent is not enabled on a VM.

---

## 2. Configure fsfreeze Hook

On Debian-based Linux guests, set the fsfreeze hook path in `/etc/default/qemu-guest-agent`:

```sh
DAEMON_ARGS="-F/etc/qemu/fsfreeze-hook"
```

Create `/etc/qemu/fsfreeze-hook`:

```sh
#!/bin/sh

# This script is executed when a guest agent receives fsfreeze-freeze and
# fsfreeze-thaw command, if it is specified in --fsfreeze-hook (-F)
# option of qemu-ga or placed in default path (/etc/qemu/fsfreeze-hook).
# When the agent receives fsfreeze-freeze request, this script is issued with
# "freeze" argument before the filesystem is frozen. And for fsfreeze-thaw
# request, it is issued with "thaw" argument after filesystem is thawed.

LOGFILE=/var/log/qga-fsfreeze-hook.log
FSFREEZE_D=$(dirname -- "$0")/fsfreeze-hook.d

is_ignored_file() {
    case "$1" in
        *~ | *.bak | *.orig | *.rpmnew | *.rpmorig | *.rpmsave | *.sample | \
        *.dpkg-old | *.dpkg-new | *.dpkg-tmp | *.dpkg-dist | \
        *.dpkg-bak | *.dpkg-backup | *.dpkg-remove)
            return 0 ;;
    esac
    return 1
}

[ ! -d "$FSFREEZE_D" ] && exit 0
for file in "$FSFREEZE_D"/* ; do
    is_ignored_file "$file" && continue
    [ -x "$file" ] || continue
    printf "$(date): execute $file $@\n" >>$LOGFILE
    "$file" "$@" >>$LOGFILE 2>&1
    STATUS=$?
    printf "$(date): $file finished with status=$STATUS\n" >>$LOGFILE
done

exit 0
```

Make the file executable:

```sh
chmod +x /etc/qemu/fsfreeze-hook
```

---

## 3. Test the Hook

Place this into `/etc/qemu/fsfreeze-hook.d/10-info` to verify the hook is called:

```sh
#!/bin/bash
dt=$(date +%s)

case "$1" in
    freeze)
        echo "frozen on $dt" | tee >(cat >/tmp/fsfreeze)
        ;;
    thaw)
        echo "thawed on $dt" | tee >(cat >>/tmp/fsfreeze)
        ;;
esac
```

---

## 4. Application-level Consistency

Place scripts in `/etc/qemu/fsfreeze-hook.d/` for each service that needs to flush data before a freeze.

### MySQL / MariaDB

Create `/etc/qemu/fsfreeze-hook.d/20-mysql`:

```sh
#!/bin/sh

# Flush MySQL tables to disk before the filesystem is frozen and keep a read
# lock to prevent writes until the filesystem is thawed.

MYSQL="/usr/bin/mysql"
MYSQL_OPTS="-uroot"
FIFO=/var/run/mysql-flush.fifo

[ -x "$MYSQL" ] && "$MYSQL" $MYSQL_OPTS < /dev/null || exit 0

flush_and_wait() {
    printf "FLUSH TABLES WITH READ LOCK \\G\n"
    trap 'printf "$(date): $0 is killed\n">&2' HUP INT QUIT ALRM TERM
    read < $FIFO
    printf "UNLOCK TABLES \\G\n"
    rm -f $FIFO
}

case "$1" in
    freeze)
        mkfifo $FIFO || exit 1
        flush_and_wait | "$MYSQL" $MYSQL_OPTS &
        while [ "$(echo 'SHOW STATUS LIKE "Key_blocks_not_flushed"' |\
                 "$MYSQL" $MYSQL_OPTS | tail -1 | cut -f 2)" -gt 0 ]; do
            sleep 1
        done
        INNODB_STATUS=$(mktemp /tmp/mysql-flush.XXXXXX)
        [ $? -ne 0 ] && exit 2
        trap "rm -f $INNODB_STATUS; exit 1" HUP INT QUIT ALRM TERM
        while :; do
            printf "SHOW ENGINE INNODB STATUS \\G" |\
                "$MYSQL" $MYSQL_OPTS > $INNODB_STATUS
            LOG_CURRENT=$(grep 'Log sequence number' $INNODB_STATUS |\
                          tr -s ' ' | cut -d' ' -f4)
            LOG_FLUSHED=$(grep 'Log flushed up to' $INNODB_STATUS |\
                          tr -s ' ' | cut -d' ' -f5)
            [ "$LOG_CURRENT" = "$LOG_FLUSHED" ] && break
            sleep 1
        done
        rm -f $INNODB_STATUS
        ;;
    thaw)
        [ ! -p $FIFO ] && exit 1
        echo > $FIFO
        ;;
    *)
        exit 1
        ;;
esac
```

### PostgreSQL

Create `/etc/qemu/fsfreeze-hook.d/20-postgresql`:

```sh
#!/bin/sh

case "$1" in
    freeze)
        psql -U postgres -c "CHECKPOINT;" >/dev/null 2>&1
        ;;
    thaw)
        ;;
esac
```

---

## 5. Using cv4pve-autosnap Hooks

You can also use the cv4pve-autosnap `--script-hook` option to run scripts before/after snapshot operations at the Proxmox level (outside the guest). See the [hooks/](../hooks/) folder for ready-to-use templates.
