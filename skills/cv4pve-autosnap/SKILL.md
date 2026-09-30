---
name: cv4pve-autosnap
description: List, take and remove automatic snapshots of Proxmox VE VMs and containers with cv4pve-autosnap, with a retention per label (keep the last N). Use it when the user asks which snapshots the tool has taken, or wants to take snapshots or apply a retention. snap and clean change the cluster; status only reads.
---

# cv4pve-autosnap

`cv4pve-autosnap` takes snapshots of the selected guests and removes the old ones of the same label, so that
each guest keeps the last N. `snap` and `clean` **change the cluster**: they create and delete snapshots.
`status` only reads. The connection options (host and API token) are in a file the user names: if you do not
know its path, ask.

## Rules

- Connect only with that file, passed with `@`. Do not print it or copy the token anywhere.
- `status` needs no agreement. Before any `snap` or `clean`, always in this order:
  1. show the user the selection (`--vmid`), the label and `--keep`;
  2. run the same command with `--dry-run` and show its `Create snapshot` / `Remove snapshot` lines;
  3. say that a dry run does not create the new snapshot, so the real `snap` removes one old snapshot more
     than it shows on each guest that already has `--keep` snapshots of that label;
  4. run it for real only after the user agrees to that exact command.
- Never run `clean --keep=0` (every snapshot of the label) unless the user asks for it in those words.
- Add `--state` (saves the VM memory too) only when the user asks for it.
- Use the same `--timestamp-format` for `snap`, `clean` and `status` of a label, or leave the default:
  snapshots with another format are not recognised.
- The tool touches only its own snapshots: description `cv4pve-autosnap`, name `auto` + label + timestamp.
  Manual snapshots are never listed nor removed.
- Exit code 0 means success; 1 means at least one guest failed, or an error with a line starting `ERROR:`.
  `status` exits 0.
- If an option is refused, check `cv4pve-autosnap --help`: this skill can be newer than the tool.

## Commands

The connection options, `--vmid`, `--dry-run` and the other global options go **before** the command; the
command options after it.

```bash
cv4pve-autosnap @<options-file> --vmid=@all status --output Json                  # every snapshot of the tool
cv4pve-autosnap @<options-file> --vmid=@all status --label=daily --output Json    # one label
cv4pve-autosnap @<options-file> --vmid=<sel> --dry-run snap --label=<label> --keep=<n>    # 1. trial
cv4pve-autosnap @<options-file> --vmid=<sel> snap --label=<label> --keep=<n>              # 2. after agreement
cv4pve-autosnap @<options-file> --vmid=<sel> --dry-run clean --label=<label> --keep=<n>   # retention only
```

When the selected guests have no snapshot of the tool, `status` prints only the header of the table
(`[]` with `--output Json`); when `--vmid` selects no guest it prints nothing. Its JSON is an array of rows
with the keys `NODE`, `VM` (a number),
`TIME` (local time, `yy/MM/dd HH:mm:ss`), `PARENT`, `NAME`, `DESCRIPTION` and `VM STATUS` (`X` when the
snapshot holds the memory).

## Selection (`--vmid`)

Comma-separated, no spaces: IDs or exact names (`100,web01`), ranges (`100:107`), `%text%` (name contains),
`text%` (starts with), `%text` (ends with), `@node-<node>`, `@pool-<pool>`, `@tag-<tag>`, `@all`; `-` in front
of any of them excludes (`@all,-105`, `@all,-@tag-test`, `@pool-prod,-200:299`). Check a selection with
`--dry-run`: it prints one `----- VM <id>` line per selected guest. See https://corsinvest.github.io/cv4pve-autosnap/guests/

Documentation: https://corsinvest.github.io/cv4pve-autosnap/
