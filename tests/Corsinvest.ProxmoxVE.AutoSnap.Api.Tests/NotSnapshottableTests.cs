/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Newtonsoft.Json;
using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

/// <summary>
/// Issue #124: Proxmox VE refuses the snapshot of a guest with a bind mount or a device, whatever its
/// 'backup' flag (AbstractConfig::snapshot_create checks every volume unless the snapshot is 'vzdump').
/// </summary>
public class NotSnapshottableTests
{
    private static string[] Lxc(string json)
        => [.. AutoSnapEngine.GetNotSnapshottableDisks(JsonConvert.DeserializeObject<VmConfigLxc>(json)!.Disks).Select(a => a.Id)];

    private static string[] Qemu(string json)
        => [.. AutoSnapEngine.GetNotSnapshottableDisks(JsonConvert.DeserializeObject<VmConfigQemu>(json)!.Disks).Select(a => a.Id)];

    [Fact]
    public void Container_with_volumes_only_can_be_snapshotted()
        => Assert.Empty(Lxc("""{"rootfs":"local-zfs:subvol-100-disk-0,size=8G","mp0":"local-zfs:subvol-100-disk-1,mp=/data,backup=1,size=32G"}"""));

    [Fact]
    public void Container_with_a_bind_mount_cannot()
        => Assert.Equal(["mp1"], Lxc("""{"rootfs":"local-zfs:subvol-100-disk-0,size=8G","mp1":"/storage1,mp=/storage1"}"""));

    [Fact]
    public void Bind_mount_blocks_also_without_backup()
        => Assert.Equal(["mp1"], Lxc("""{"rootfs":"local-zfs:subvol-100-disk-0,size=8G","mp1":"/storage1,mp=/storage1,backup=0"}"""));

    [Fact]
    public void Container_with_a_device_mount_cannot()
        => Assert.Equal(["mp0"], Lxc("""{"rootfs":"local-zfs:subvol-100-disk-0,size=8G","mp0":"/dev/sdb1,mp=/mnt/disk"}"""));

    [Fact]
    public void Unused_volume_does_not_block()
        => Assert.Empty(Lxc("""{"rootfs":"local-zfs:subvol-100-disk-0,size=8G","unused0":"local-zfs:subvol-100-disk-2"}"""));

    [Fact]
    public void Vm_with_volumes_and_a_cdrom_can_be_snapshotted()
        => Assert.Empty(Qemu("""{"scsi0":"local-zfs:vm-100-disk-0,size=32G","ide2":"local:iso/debian.iso,media=cdrom","agent":"1"}"""));

    [Fact]
    public void Vm_with_a_physical_disk_cannot()
        => Assert.Equal(["scsi1"], Qemu("""{"scsi0":"local-zfs:vm-100-disk-0,size=32G","scsi1":"/dev/disk/by-id/ata-WDC_WD40EFRX-68N32N0_WD-XXXX,size=3907018584K"}"""));
}
