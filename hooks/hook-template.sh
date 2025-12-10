#!/bin/bash
# SPDX-License-Identifier: GPL-3.0-only
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
#
# Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap hook script.
# Process environment variables as received from and set by cv4pve-autosnap.

hook() {
    echo "----------------------------------------------------------"
    echo "CV4PVE_AUTOSNAP_PHASE:         $CV4PVE_AUTOSNAP_PHASE"
    echo "CV4PVE_AUTOSNAP_VMID:          $CV4PVE_AUTOSNAP_VMID"
    echo "CV4PVE_AUTOSNAP_VMNAME:        $CV4PVE_AUTOSNAP_VMNAME"
    echo "CV4PVE_AUTOSNAP_VMTYPE:        $CV4PVE_AUTOSNAP_VMTYPE"
    echo "CV4PVE_AUTOSNAP_LABEL:         $CV4PVE_AUTOSNAP_LABEL"
    echo "CV4PVE_AUTOSNAP_KEEP:          $CV4PVE_AUTOSNAP_KEEP"
    echo "CV4PVE_AUTOSNAP_SNAP_NAME:     $CV4PVE_AUTOSNAP_SNAP_NAME"
    echo "CV4PVE_AUTOSNAP_VMSTATE:       $CV4PVE_AUTOSNAP_VMSTATE"
    echo "CV4PVE_AUTOSNAP_DURATION:      $CV4PVE_AUTOSNAP_DURATION"
    echo "CV4PVE_AUTOSNAP_STATE:         $CV4PVE_AUTOSNAP_STATE"
    echo "CV4PVE_AUTOSNAP_DEBUG:         $CV4PVE_AUTOSNAP_DEBUG"
    echo "CV4PVE_AUTOSNAP_DRY_RUN:       $CV4PVE_AUTOSNAP_DRY_RUN"
    echo "----------------------------------------------------------"

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

    case "$CV4PVE_AUTOSNAP_PHASE" in
        # Clean job phases
        clean-job-start)
            # Add custom logic here for cleanup job start
            ;;
        clean-job-end)
            # Add custom logic here for cleanup job end
            ;;

        # Snap job phases
        snap-job-start)
            # Add custom logic here for snapshot job start
            ;;
        snap-job-end)
            # Add custom logic here for snapshot job end
            ;;

        # Snapshot creation phases
        snap-create-pre)
            # Add custom logic here for before snapshot creation
            ;;
        snap-create-post)
            # Add custom logic here for after successful snapshot creation
            ;;
        snap-create-abort)
            # Add custom logic here for when snapshot creation is aborted
            ;;

        # Snapshot removal phases
        snap-remove-pre)
            # Add custom logic here for before snapshot removal
            ;;
        snap-remove-post)
            # Add custom logic here for after successful snapshot removal
            ;;
        snap-remove-abort)
            # Add custom logic here for when snapshot removal is aborted
            ;;

        *)
            echo "unknown phase '$CV4PVE_AUTOSNAP_PHASE'"
            return 1
            ;;
    esac
}

hook