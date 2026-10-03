# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.1] - 2026-10-03

### Fixed
- A snapshot request refused by Proxmox VE (a missing privilege, a guest that no longer exists) printed an internal error (`The call is ambiguous between the following methods or properties: ...`) instead of the reason: it now prints what Proxmox VE answered, for example `Permission check failed (/vms/100, VM.Snapshot)`, or the parameter that was refused ([#140](https://github.com/Corsinvest/cv4pve-autosnap/pull/140))
- A node of `--host` that does not answer is given up after 4 seconds instead of the TCP timeout of the system (21 seconds on Windows), through Corsinvest.ProxmoxVE.Api 9.2.5

### Changed
- Updated Corsinvest.ProxmoxVE.Api.Extension and Api.Console to 9.2.5 ([#140](https://github.com/Corsinvest/cv4pve-autosnap/pull/140))
- Documentation: page titles that say what the page is about in search results ([#137](https://github.com/Corsinvest/cv4pve-autosnap/pull/137), [#139](https://github.com/Corsinvest/cv4pve-autosnap/pull/139)), theme 2.6.2 ([#138](https://github.com/Corsinvest/cv4pve-autosnap/pull/138))

## [2.2.0] - 2026-09-30

### Added
- Documentation site: https://corsinvest.github.io/cv4pve-autosnap/, replaces `docs/snapshot-consistency.md` ([#128](https://github.com/Corsinvest/cv4pve-autosnap/pull/128))
- Skill `skills/cv4pve-autosnap` and an AI assistants page, to let an assistant such as Claude Code list, take and clean snapshots with a token limited to the guests concerned ([#133](https://github.com/Corsinvest/cv4pve-autosnap/pull/133))
- Label and `--timestamp-format` are checked at start: a name Proxmox VE would refuse (characters other than letters, digits, `-` and `_`, more than 40 characters) or a format the tool could not read back (variable length, not in time order) stops with `ERROR:` before any snapshot ([#132](https://github.com/Corsinvest/cv4pve-autosnap/pull/132))
- Test project `Corsinvest.ProxmoxVE.AutoSnap.Api.Tests` ([#132](https://github.com/Corsinvest/cv4pve-autosnap/pull/132))
- API: `ResultSnap.VmsFound`, `PhaseEventArgs.Out`, `AutoSnapEngine.ValidateLabelAndTimestampFormat` ([#132](https://github.com/Corsinvest/cv4pve-autosnap/pull/132))

### Changed (behaviour)
- Guests Proxmox VE cannot snapshot (a container with a bind mount or a device mount point, a VM with a physical disk) are skipped with a message instead of failing the run, also when the mount point has `backup=0` ([#124](https://github.com/Corsinvest/cv4pve-autosnap/issues/124))
- Guests on an offline node (status unknown) are listed and skipped, and the run exits with 1; they were left out without a message
- `snap` and `clean` exit with code 1 when `--vmid` selects no guest (was 0)
- `--timeout` accepts 1 to 86400 seconds; 0 and negative values made the tool stop waiting for the tasks
- `status`: `TIME` in local time, as the timestamp in the snapshot name (was UTC); numbers right-aligned

### Fixed
- `--max-perc-storage` never skipped a guest: the storage usage (a fraction) was compared with the percentage
- A storage reporting no usage or no size skipped its guests whatever `--max-perc-storage` ([#108](https://github.com/Corsinvest/cv4pve-autosnap/issues/108)): an empty storage is now valid, one with no size shows `n/a` and is not checked
- VM/CT selection, through Corsinvest.ProxmoxVE.Api 9.2.4: an excluded range (`-200:299`) no longer adds guests, pools combine with exclusions and duplicates (`@all,-@pool-test`, `@pool-prod,-100`), `text%` matches names starting with the text and `%text` names ending with it (they were inverted)
- Pool selection on Proxmox VE 9: `Pool.Audit` replaces `Pool.Allocate` in the declared privileges, and a pool that selects nothing names the privilege ([#122](https://github.com/Corsinvest/cv4pve-autosnap/issues/122))
- The exit code of the hook script was never read: a non-zero code is now printed as `Script return code: N`
- A request refused by Proxmox VE ended in `Object reference not set to an instance of an object`
- Output of hooks, task errors and removals stays in the block of its guest, also with `--max-parallel`
- `clean`: missing line break before `Timestamp format`, template message without the guest
- No `Datastore.Audit` warning when no guest is found
- Library: snapshot names and `CV4PVE_AUTOSNAP_DURATION` use the invariant culture: with a Thai, Persian or Hijri calendar the timestamp had another year
- Hook script comments: `--script`, not `--script-hook`, after the command

### Changed
- Updated Corsinvest.ProxmoxVE.Api.Extension and Api.Console to 9.2.4
- README shortened, with links to the documentation
- Product icon (Lucide `camera`) and Windows executable icon
- NuGet package description
- Project metadata, symbols (Source Link, `.snupkg`) and code style aligned with the other cv4pve tools
- CI and publishing through the shared cv4pve-tools workflows

## [2.1.1] - 2026-04-20

### Fixed
- `snap` command no longer returns exit code 1 when the target set includes template VMs or skipped VMs via `--only-running` ([#119](https://github.com/Corsinvest/cv4pve-autosnap/issues/119))

### Changed
- Updated NuGet packages to 9.1.15

## [2.1.0] - 2026-04-14

### Added
- `--max-parallel` option for `snap` command: snapshot multiple VMs at the same time to significantly reduce total run time on large clusters (default: 1, sequential); output remains grouped per VM even when running in parallel

## [2.0.1] - 2026-04-09

### Added
- Documentation for snapshot consistency with QEMU Guest Agent (`docs/snapshot-consistency.md`)
- AUR installation support (Arch Linux)

### Changed
- Update README: AUR badge, Arch Linux / Debian / RHEL / macOS Homebrew installation, hook templates reference, QEMU Guest Agent feature note
- Update NuGet packages to 9.1.11

## [2.0.0] - 2026-03-21

### Breaking Changes (NuGet API)
- `Application` class renamed to `AutoSnapEngine`
- `PhaseEventArgs`: `Vm` and `SnapName` are now nullable
- `Phases` property type changed from `Dictionary` to `IReadOnlyDictionary`

### Changed
- Internal code improvements and cleanup

### Fixed
- Clean operation now correctly reports failure status when a snapshot removal fails
- Snapshot removal errors are now properly captured and reported

## [1.21.0] - 2026-02-20

### Fixed
- Fix `snap --dry-run` non-zero exit code: `inError` was initialized to `true` and never reset when `--dry-run` skipped snapshot creation ([#114](https://github.com/Corsinvest/cv4pve-autosnap/issues/114))

### Changed
- Update NuGet packages to 9.1.4

## [1.20.0] - 2026-02-16

### Changed
- Implement async event pattern for `PhaseEvent` ([#113](https://github.com/Corsinvest/cv4pve-autosnap/issues/113))
- Remove local WinGet manifests (now published in official repository)
- Add WinGet installation instructions to README

## [1.19.0] - 2025-12-24

### Fixed
- Fix storage check to ignore bind mounts in LXC containers ([#112](https://github.com/Corsinvest/cv4pve-autosnap/issues/112))

## [1.18.0] - 2025-12-23

### Fixed
- Fix `NullReferenceException` in `PhaseEventArgs.Environments` property ([#110](https://github.com/Corsinvest/cv4pve-autosnap/issues/110))

### Changed
- Migrate to centralized GitHub workflow
- Add WinGet manifests

## [1.17.0] - 2025-12-10

### Added
- Add CI/CD workflows and modernize project infrastructure ([#106](https://github.com/Corsinvest/cv4pve-autosnap/issues/106))

### Changed
- Configure embedded debug symbols for single-file executables

## [1.16.0] - 2025-03-21

### Changed
- Add support for .NET 7/8/9 multi-targeting ([#101](https://github.com/Corsinvest/cv4pve-autosnap/issues/101))

## [1.15.2] - 2025-01-29

### Fixed
- Fix [#99](https://github.com/Corsinvest/cv4pve-autosnap/issues/99)

## [1.15.1] - 2025-01-03

### Fixed
- Fix [#97](https://github.com/Corsinvest/cv4pve-autosnap/issues/97)

## [1.15.0] - 2024-12-19

### Fixed
- Fix [#93](https://github.com/Corsinvest/cv4pve-autosnap/issues/93)
