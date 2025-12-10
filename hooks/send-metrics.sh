#!/bin/bash
# SPDX-License-Identifier: GPL-3.0-only
# SPDX-FileCopyrightText: Copyright Corsinvest Srl
#
# Corsinvest automatic snapshot for Proxmox VE cv4pve-autosnap metric sender.
# Sends snapshot metrics to external monitoring systems.
#
# Usage:
#   This script can be used as a hook script with cv4pve-autosnap to send
#   metrics about snapshot operations to various monitoring systems.
#
# Configuration:
#   The following environment variables can be used to configure the script:
#
#   METRICS_ENDPOINT (default: http://localhost:9091/metrics/job/cv4pve-autosnap)
#     - The endpoint to send metrics to
#
#   METRICS_TYPE (default: prometheus)
#     - Type of metrics format to send (prometheus, influxdb, json, grafana, loki)
#
#   METRICS_USERNAME (optional)
#     - Username for basic authentication
#
#   METRICS_PASSWORD (optional)
#     - Password for basic authentication
#
# Examples:
#   # Using as a script hook for Prometheus Pushgateway
#   cv4pve-autosnap --host=pve.local --api-token=token --vmid=100 \
#                   --script-hook=/path/to/send-metrics.sh snap --label=daily --keep=7
#
#   # With custom endpoint and metrics type
#   METRICS_ENDPOINT="http://prometheus.local:9091/metrics/job/cv4pve-autosnap" \
#   METRICS_TYPE="prometheus" \
#   cv4pve-autosnap --host=pve.local --api-token=token --vmid=100 \
#                   --script-hook=/path/to/send-metrics.sh snap --label=daily --keep=7
#
#   # For InfluxDB
#   METRICS_ENDPOINT="http://influxdb.local:8086/write?db=proxmox" \
#   METRICS_TYPE="influxdb" \
#   cv4pve-autosnap --host=pve.local --api-token=token --vmid=100 \
#                   --script-hook=/path/to/send-metrics.sh snap --label=daily --keep=7
#
#   # For Grafana Loki
#   METRICS_ENDPOINT="http://loki.local:3100/loki/api/v1/push" \
#   METRICS_TYPE="loki" \
#   cv4pve-autosnap --host=pve.local --api-token=token --vmid=100 \
#                   --script-hook=/path/to/send-metrics.sh snap --label=daily --keep=7

# Configuration - can be overridden by environment variables
METRICS_ENDPOINT="${METRICS_ENDPOINT:-http://localhost:9091/metrics/job/cv4pve-autosnap}"
METRICS_TYPE="${METRICS_TYPE:-prometheus}"  # prometheus, influxdb, json, grafana
METRICS_USERNAME="${METRICS_USERNAME:-}"   # Optional basic auth
METRICS_PASSWORD="${METRICS_PASSWORD:-}"   # Optional basic auth

# Function to send metrics via curl with optional authentication
send_via_curl() {
    local payload="$1"
    local content_type="$2"
    local endpoint="$3"

    if [ -n "$METRICS_USERNAME" ] && [ -n "$METRICS_PASSWORD" ]; then
        curl -X POST \
             -u "$METRICS_USERNAME:$METRICS_PASSWORD" \
             -H "Content-Type: $content_type" \
             --data "$payload" \
             "$endpoint" 2>/dev/null
    else
        curl -X POST \
             -H "Content-Type: $content_type" \
             --data "$payload" \
             "$endpoint" 2>/dev/null
    fi
}

# Function to send metrics via wget with optional authentication
send_via_wget() {
    local payload="$1"
    local content_type="$2"
    local endpoint="$3"

    if [ -n "$METRICS_USERNAME" ] && [ -n "$METRICS_PASSWORD" ]; then
        wget --post-data="$payload" \
             --header="Content-Type: $content_type" \
             --user="$METRICS_USERNAME" \
             --password="$METRICS_PASSWORD" \
             "$endpoint" 2>/dev/null
    else
        wget --post-data="$payload" \
             --header="Content-Type: $content_type" \
             "$endpoint" 2>/dev/null
    fi
}

# Function to send metrics using available tool
send_metrics_payload() {
    local payload="$1"
    local content_type="$2"
    local endpoint="$3"

    if command -v curl >/dev/null 2>&1; then
        send_via_curl "$payload" "$content_type" "$endpoint"
    elif command -v wget >/dev/null 2>&1; then
        send_via_wget "$payload" "$content_type" "$endpoint"
    else
        echo "No curl or wget available to send metrics"
        return 1
    fi
}

# Prometheus format
send_prometheus_metrics() {
    local phase="$1"
    local vmid="$2"
    local vmname="$3"
    local vmtype="$4"
    local label="$5"
    local keep="$6"
    local snap_name="$7"
    local vmstate="$8"
    local duration="$9"
    local status="${10}"
    local timestamp="${11}"

    case "$phase" in
        snap-create-post)
            local metrics_payload="cv4pve_snapshot_duration_seconds{vmid=\"$vmid\",vmname=\"$vmname\",vmtype=\"$vmtype\",label=\"$label\",vmstate=\"$vmstate\",status=\"$status\"} $duration
cv4pve_snapshot_created{vmid=\"$vmid\",vmname=\"$vmname\",vmtype=\"$vmtype\",label=\"$label\",snap_name=\"$snap_name\",status=\"$status\"} $timestamp"
            ;;
        clean-job-end)
            metrics_payload="cv4pve_snapshot_cleanup_total{vmid=\"$vmid\",vmname=\"$vmname\",label=\"$label\",status=\"$status\"} $timestamp"
            ;;
        *)
            return 0  # No metrics to send for other phases
            ;;
    esac

    send_metrics_payload "$metrics_payload" "text/plain" "$METRICS_ENDPOINT"
}

# InfluxDB format
send_influxdb_metrics() {
    local phase="$1"
    local vmid="$2"
    local vmname="$3"
    local vmtype="$4"
    local label="$5"
    local keep="$6"
    local snap_name="$7"
    local vmstate="$8"
    local duration="$9"
    local status="${10}"
    local timestamp="${11}"

    case "$phase" in
        snap-create-post)
            local metrics_payload="snapshots,vmid=$vmid,vmname=$vmname,vmtype=$vmtype,label=$label,vmstate=$vmstate,status=$status duration=$duration,$timestamp"
            ;;
        clean-job-end)
            metrics_payload="cleanup,vmid=$vmid,vmname=$vmname,label=$label,status=$status count=1,$timestamp"
            ;;
        *)
            return 0  # No metrics to send for other phases
            ;;
    esac

    send_metrics_payload "$metrics_payload" "text/plain" "$METRICS_ENDPOINT"
}

# Generic JSON format
send_json_metrics() {
    local phase="$1"
    local vmid="$2"
    local vmname="$3"
    local vmtype="$4"
    local label="$5"
    local keep="$6"
    local snap_name="$7"
    local vmstate="$8"
    local duration="$9"
    local status="${10}"
    local debug="${11}"
    local dry_run="${12}"
    local timestamp="${13}"

    local metrics_payload="{\"phase\":\"$phase\",\"vmid\":$vmid,\"vmname\":\"$vmname\",\"vmtype\":\"$vmtype\",\"label\":\"$label\",\"keep\":$keep,\"snap_name\":\"$snap_name\",\"vmstate\":\"$vmstate\",\"duration\":$duration,\"status\":\"$status\",\"debug\":\"$debug\",\"dry_run\":\"$dry_run\",\"timestamp\":$timestamp}"

    send_metrics_payload "$metrics_payload" "application/json" "$METRICS_ENDPOINT"
}

# Grafana Loki format
send_loki_metrics() {
    local phase="$1"
    local vmid="$2"
    local vmname="$3"
    local vmtype="$4"
    local label="$5"
    local keep="$6"
    local snap_name="$7"
    local vmstate="$8"
    local duration="$9"
    local status="${10}"
    local debug="${11}"
    local dry_run="${12}"
    local timestamp="${13}"

    # Convert timestamp to nanoseconds for Loki
    local loki_timestamp=$(($timestamp * 1000000000))

    local metrics_payload="{\"streams\":[{\"stream\":{\"job\":\"cv4pve-autosnap\",\"vmid\":\"$vmid\",\"phase\":\"$phase\"},\"values\":[[\"$loki_timestamp\",\"{\\\"vmid\\\":\\\"$vmid\\\",\\\"vmname\\\":\\\"$vmname\\\",\\\"vmtype\\\":\\\"$vmtype\\\",\\\"label\\\":\\\"$label\\\",\\\"phase\\\":\\\"$phase\\\",\\\"duration\\\":$duration,\\\"status\\\":\\\"$status\\\"}\"]]}]}"

    send_metrics_payload "$metrics_payload" "application/json" "$METRICS_ENDPOINT"
}

# Main dispatcher function
send_metrics() {
    # Extract environment variables
    local phase="$CV4PVE_AUTOSNAP_PHASE"
    local vmid="$CV4PVE_AUTOSNAP_VMID"
    local vmname="$CV4PVE_AUTOSNAP_VMNAME"
    local vmtype="$CV4PVE_AUTOSNAP_VMTYPE"
    local label="$CV4PVE_AUTOSNAP_LABEL"
    local keep="$CV4PVE_AUTOSNAP_KEEP"
    local snap_name="$CV4PVE_AUTOSNAP_SNAP_NAME"
    local vmstate="$CV4PVE_AUTOSNAP_VMSTATE"
    local duration="$CV4PVE_AUTOSNAP_DURATION"
    local status="$CV4PVE_AUTOSNAP_STATE"
    local debug="$CV4PVE_AUTOSNAP_DEBUG"
    local dry_run="$CV4PVE_AUTOSNAP_DRY_RUN"
    local timestamp=$(date -u +%s)

    # Select the appropriate function based on METRICS_TYPE
    case "$METRICS_TYPE" in
        "prometheus")
            send_prometheus_metrics "$phase" "$vmid" "$vmname" "$vmtype" "$label" "$keep" "$snap_name" "$vmstate" "$duration" "$status" "$timestamp"
            ;;
        "influxdb")
            send_influxdb_metrics "$phase" "$vmid" "$vmname" "$vmtype" "$label" "$keep" "$snap_name" "$vmstate" "$duration" "$status" "$timestamp"
            ;;
        "json")
            send_json_metrics "$phase" "$vmid" "$vmname" "$vmtype" "$label" "$keep" "$snap_name" "$vmstate" "$duration" "$status" "$debug" "$dry_run" "$timestamp"
            ;;
        "grafana"|"loki")
            send_loki_metrics "$phase" "$vmid" "$vmname" "$vmtype" "$label" "$keep" "$snap_name" "$vmstate" "$duration" "$status" "$debug" "$dry_run" "$timestamp"
            ;;
        *)
            echo "Unknown METRICS_TYPE: $METRICS_TYPE. Supported types: prometheus, influxdb, json, grafana/loki"
            return 1
            ;;
    esac

    # Log what was sent based on phase
    case "$phase" in
        snap-create-post)
            echo "Sent $METRICS_TYPE metrics for VM $vmid (Label: $label, Duration: $duration)"
            ;;
        clean-job-end)
            echo "Sent $METRICS_TYPE cleanup metrics for VM $vmid (Label: $label)"
            ;;
        *)
            # For other phases, just log if needed
            echo "Phase $phase - metrics sent based on configuration"
            ;;
    esac
}

# Execute the function
send_metrics