@ECHO OFF
REM  This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
REm
REM  This source file is available under two different licenses:
REM  - GNU General Public License version 3 (GPLv3)
REM  - Corsinvest Enterprise License (CEL)
REM  Full copyright and license information is available in
REM  LICENSE.md which is distributed with this source code.
REM
REM  Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL

REM Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap hook script.
REM Process environment variables as received from and set by cv4pve-autosnap.

ECHO ----------------------------------------------------------
ECHO CV4PVE_AUTOSNAP_PHASE:         %CV4PVE_AUTOSNAP_PHASE%
ECHO CV4PVE_AUTOSNAP_VMID:          %CV4PVE_AUTOSNAP_VMID%
ECHO CV4PVE_AUTOSNAP_VMNAME:        %CV4PVE_AUTOSNAP_VMNAME%
ECHO CV4PVE_AUTOSNAP_VMTECHNOLOGY:  %CV4PVE_AUTOSNAP_VMTECHNOLOGY%
ECHO CV4PVE_AUTOSNAP_LABEL:         %CV4PVE_AUTOSNAP_LABEL%
ECHO CV4PVE_AUTOSNAP_KEEP:          %CV4PVE_AUTOSNAP_KEEP%
ECHO CV4PVE_AUTOSNAP_VMSTATE:       %CV4PVE_AUTOSNAP_VMSTATE%
ECHO CV4PVE_AUTOSNAP_SNAP_NAME:     %CV4PVE_AUTOSNAP_SNAP_NAME%
ECHO CV4PVE_AUTOSNAP_DEBUG:         %CV4PVE_AUTOSNAP_DEBUG%
ECHO CV4PVE_AUTOSNAP_DRY_RUN:       %CV4PVE_AUTOSNAP_DRYRUN%
ECHO CV4PVE_AUTOSNAP_DURATION       %CV4PVE_AUTOSNAP_DURATION%
ECHO CV4PVE_AUTOSNAP_STATE          %CV4PVE_AUTOSNAP_STATE%

IF "%CV4PVE_AUTOSNAP_PHASE%"=="clean-job-start" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="clean-job-end" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-job-start" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-job-end" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-pre" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-post" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-create-abort" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-pre" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-post" (
    REM
) ELSE IF "%CV4PVE_AUTOSNAP_PHASE%"=="snap-remove-abort" (
    REM
) ELSE (
    ECHO "unknown phase '$CV4PVE_BARC_PHASE'"
    EXIT 1
)