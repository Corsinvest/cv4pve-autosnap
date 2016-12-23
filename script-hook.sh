#!/bin/bash
#
# EnterpriseVE automatic snapshot for Proxmox eve4pve-autosnap hook script.

# Process environment variables as received from and set by eve4pve-autosnap.

hook() {
    echo "EVE4PVE_AUTOSNAP_PHASE:     $EVE4PVE_AUTOSNAP_PHASE" 
    echo "EVE4PVE_AUTOSNAP_VMID:      $EVE4PVE_AUTOSNAP_VMID" 
    echo "EVE4PVE_AUTOSNAP_LABEL:     $EVE4PVE_AUTOSNAP_LABEL" 
    echo "EVE4PVE_AUTOSNAP_KEEP:      $EVE4PVE_AUTOSNAP_KEEP" 
    echo "EVE4PVE_AUTOSNAP_VMSTATE:   $EVE4PVE_AUTOSNAP_VMSTATE" 
    echo "EVE4PVE_AUTOSNAP_SNAP_NAME: $EVE4PVE_AUTOSNAP_SNAP_NAME" 

    case "$EVE4PVE_AUTOSNAP_PHASE" in
        #snap job status
        clean-job-start);;
        clean-job-end);;

        #snap job status
        snap-job-start);;
        snap-job-end);;
        snap-job-abort);;

        #create snapshot
        snap-create-pre);;
        snap-create-post);;
        snap-create-abort);;

        #remove snapshot
        snap-remove-pre);;
        snap-remove-post);;
        snap-remove-aboort);;

        *) echo "unknown phase '$phase'"; return 1;;
    esac
}

hook