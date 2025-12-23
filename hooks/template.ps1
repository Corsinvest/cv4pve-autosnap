# SPDX-License-Identifier: GPL-3.0-only
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
#
# Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap hook script.
# Process environment variables as received from and set by cv4pve-autosnap.

# Display all environment variables for debugging
Write-Host "----------------------------------------------------------"
Write-Host "CV4PVE_AUTOSNAP_PHASE:         " $env:CV4PVE_AUTOSNAP_PHASE
Write-Host "CV4PVE_AUTOSNAP_VMID:          " $env:CV4PVE_AUTOSNAP_VMID
Write-Host "CV4PVE_AUTOSNAP_VMNAME:        " $env:CV4PVE_AUTOSNAP_VMNAME
Write-Host "CV4PVE_AUTOSNAP_VMTYPE:        " $env:CV4PVE_AUTOSNAP_VMTYPE
Write-Host "CV4PVE_AUTOSNAP_LABEL:         " $env:CV4PVE_AUTOSNAP_LABEL
Write-Host "CV4PVE_AUTOSNAP_KEEP:          " $env:CV4PVE_AUTOSNAP_KEEP
Write-Host "CV4PVE_AUTOSNAP_SNAP_NAME:     " $env:CV4PVE_AUTOSNAP_SNAP_NAME
Write-Host "CV4PVE_AUTOSNAP_VMSTATE:       " $env:CV4PVE_AUTOSNAP_VMSTATE
Write-Host "CV4PVE_AUTOSNAP_DURATION:      " $env:CV4PVE_AUTOSNAP_DURATION
Write-Host "CV4PVE_AUTOSNAP_STATE:         " $env:CV4PVE_AUTOSNAP_STATE
Write-Host "CV4PVE_AUTOSNAP_DEBUG:         " $env:CV4PVE_AUTOSNAP_DEBUG
Write-Host "CV4PVE_AUTOSNAP_DRY_RUN:       " $env:CV4PVE_AUTOSNAP_DRY_RUN
Write-Host "----------------------------------------------------------"

# Hook script phases:
# clean-job-start    - Before starting cleanup operation
# clean-job-end      - After finishing cleanup operation
# snap-job-start     - Before starting snapshot operation
# snap-job-end       - After finishing snapshot operation
# snap-create-pre    - Before creating snapshot
# snap-create-post   - After successfully creating snapshot
# snap-create-abort  - When snapshot creation is aborted
# snap-remove-pre    - Before removing snapshot
# snap-remove-post   - After successfully removing snapshot
# snap-remove-abort  - When snapshot removal is aborted

# Main hook logic based on phase
switch ($env:CV4PVE_AUTOSNAP_PHASE) {
    # Clean job phases
    "clean-job-start" {
        # Add custom logic here for cleanup job start
    }
    "clean-job-end" {
        # Add custom logic here for cleanup job end
    }

    # Snap job phases
    "snap-job-start" {
        # Add custom logic here for snapshot job start
    }
    "snap-job-end" {
        # Add custom logic here for snapshot job end
    }

    # Snapshot creation phases
    "snap-create-pre" {
        # Add custom logic here for before snapshot creation
    }
    "snap-create-post" {
        # Add custom logic here for after successful snapshot creation
    }
    "snap-create-abort" {
        # Add custom logic here for when snapshot creation is aborted
    }

    # Snapshot removal phases
    "snap-remove-pre" {
        # Add custom logic here for before snapshot removal
    }
    "snap-remove-post" {
        # Add custom logic here for after successful snapshot removal
    }
    "snap-remove-abort" {
        # Add custom logic here for when snapshot removal is aborted
    }

    default {
        Write-Error "Unknown phase '$($env:CV4PVE_AUTOSNAP_PHASE)'"
        exit 1
    }
}