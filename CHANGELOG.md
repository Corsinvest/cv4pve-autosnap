# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
