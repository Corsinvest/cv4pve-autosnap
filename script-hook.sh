#!/bin/bash
# This file is part of the cv4pve-autosnap https://github.com/Corsinvest/cv4pve-autosnap,
#
# This source file is available under two different licenses:
# - GNU General Public License version 3 (GPLv3)
# - Corsinvest Enterprise License (CEL)
# Full copyright and license information is available in
# LICENSE.md which is distributed with this source code.
#
# Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL

# Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap hook script.
# Process environment variables as received from and set by cv4pve-autosnap.

hook() {
    echo "CV4PVE_AUTOSNAP_PHASE:         $CV4PVE_AUTOSNAP_PHASE"
    echo "CV4PVE_AUTOSNAP_VMID:          $CV4PVE_AUTOSNAP_VMID"
    echo "CV4PVE_AUTOSNAP_VMTECHNOLOGY:  $CV4PVE_AUTOSNAP_VMTECHNOLOGY"
    echo "CV4PVE_AUTOSNAP_LABEL:         $CV4PVE_AUTOSNAP_LABEL"
    echo "CV4PVE_AUTOSNAP_KEEP:          $CV4PVE_AUTOSNAP_KEEP"
    echo "CV4PVE_AUTOSNAP_VMSTATE:       $CV4PVE_AUTOSNAP_VMSTATE"
    echo "CV4PVE_AUTOSNAP_SNAP_NAME:     $CV4PVE_AUTOSNAP_SNAP_NAME"
    echo "CV4PVE_AUTOSNAP_DEBUG:         $CV4PVE_AUTOSNAP_DEBUG"
    echo "CV4PVE_AUTOSNAP_DRY_RUN:       $CV4PVE_AUTOSNAP_DRY_RUN"

    case "$CV4PVE_AUTOSNAP_PHASE" in
        #clean job status
        clean-job-start);;
        clean-job-end);;

        #snap job status
        snap-job-start);;
        snap-job-end);;

        #create snapshot
        snap-create-pre);;
        snap-create-post);;
        snap-create-abort);;

        #remove snapshot
        snap-remove-pre);;
        snap-remove-post);;
        snap-remove-abort);;

        *) echo "unknown phase '$CV4PVE_BARC_PHASE'"; return 1;;
    esac
}

hook