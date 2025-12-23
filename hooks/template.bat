@ECHO OFF
REM SPDX-License-Identifier: GPL-3.0-only
REM SPDX-FileCopyrightText: Copyright Corsinvest Srl
REM
REM Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap hook script.
REM Process environment variables as received from and set by cv4pve-autosnap.

ECHO ----------------------------------------------------------
ECHO CV4PVE_AUTOSNAP_PHASE:         %CV4PVE_AUTOSNAP_PHASE%
ECHO CV4PVE_AUTOSNAP_VMID:          %CV4PVE_AUTOSNAP_VMID%
ECHO CV4PVE_AUTOSNAP_VMNAME:        %CV4PVE_AUTOSNAP_VMNAME%
ECHO CV4PVE_AUTOSNAP_VMTYPE:        %CV4PVE_AUTOSNAP_VMTYPE%
ECHO CV4PVE_AUTOSNAP_LABEL:         %CV4PVE_AUTOSNAP_LABEL%
ECHO CV4PVE_AUTOSNAP_KEEP:          %CV4PVE_AUTOSNAP_KEEP%
ECHO CV4PVE_AUTOSNAP_SNAP_NAME:     %CV4PVE_AUTOSNAP_SNAP_NAME%
ECHO CV4PVE_AUTOSNAP_VMSTATE:       %CV4PVE_AUTOSNAP_VMSTATE%
ECHO CV4PVE_AUTOSNAP_DURATION:      %CV4PVE_AUTOSNAP_DURATION%
ECHO CV4PVE_AUTOSNAP_STATE:         %CV4PVE_AUTOSNAP_STATE%
ECHO CV4PVE_AUTOSNAP_DEBUG:         %CV4PVE_AUTOSNAP_DEBUG%
ECHO CV4PVE_AUTOSNAP_DRY_RUN:       %CV4PVE_AUTOSNAP_DRY_RUN%
ECHO ----------------------------------------------------------

REM Hook script phases:
REM clean-job-start    - Before starting cleanup operation
REM clean-job-end      - After finishing cleanup operation
REM snap-job-start     - Before starting snapshot operation
REM snap-job-end       - After finishing snapshot operation
REM snap-create-pre    - Before creating snapshot
REM snap-create-post   - After successfully creating snapshot
REM snap-create-abort  - When snapshot creation is aborted
REM snap-remove-pre    - Before removing snapshot
REM snap-remove-post   - After successfully removing snapshot
REM snap-remove-abort  - When snapshot removal is aborted

IF "%CV4PVE_AUTOSNAP_PHASE%"=="clean-job-start" (
    REM Add custom logic here for cleanup job start
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="clean-job-end" (
    REM Add custom logic here for cleanup job end
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-job-start" (
    REM Add custom logic here for snapshot job start
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-job-end" (
    REM Add custom logic here for snapshot job end
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-pre" (
    REM Add custom logic here for before snapshot creation
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-post" (
    REM Add custom logic here for after successful snapshot creation
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-abort" (
    REM Add custom logic here for when snapshot creation is aborted
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-pre" (
    REM Add custom logic here for before snapshot removal
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-post" (
    REM Add custom logic here for after successful snapshot removal
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-abort" (
    REM Add custom logic here for when snapshot removal is aborted
) ELSE (
    ECHO "unknown phase '%CV4PVE_AUTOSNAP_PHASE%'"
    EXIT 1
)