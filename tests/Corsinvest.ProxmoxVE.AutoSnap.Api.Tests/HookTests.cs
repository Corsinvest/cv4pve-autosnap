/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

public class HookTests
{
    [Fact]
    public void Every_phase_has_its_name()
    {
        Assert.Equal(Enum.GetValues<HookPhase>().Length, AutoSnapEngine.Phases.Count);
        foreach (var phase in Enum.GetValues<HookPhase>()) { Assert.NotNull(AutoSnapEngine.PhaseEnumToStr(phase)); }
    }

    [Theory]
    [InlineData(HookPhase.SnapJobStart, "snap-job-start")]
    [InlineData(HookPhase.SnapCreatePre, "snap-create-pre")]
    [InlineData(HookPhase.SnapRemoveAbort, "snap-remove-abort")]
    [InlineData(HookPhase.CleanJobEnd, "clean-job-end")]
    public void Phase_names(HookPhase phase, string expected)
        => Assert.Equal(expected, AutoSnapEngine.PhaseEnumToStr(phase));

    [Fact]
    public void Guest_phase_passes_the_guest_and_the_snapshot()
    {
        var vm = new ClusterResource { VmId = 100, Name = "web-01", Type = "lxc" };
        var env = new PhaseEventArgs(HookPhase.SnapCreatePost, vm, "daily", 7, "autodaily260929020000", true, 1.5, true).Environments;

        Assert.Equal("snap-create-post", env["CV4PVE_AUTOSNAP_PHASE"]);
        Assert.Equal("100", env["CV4PVE_AUTOSNAP_VMID"]);
        Assert.Equal("web-01", env["CV4PVE_AUTOSNAP_VMNAME"]);
        Assert.Equal("lxc", env["CV4PVE_AUTOSNAP_VMTYPE"]);
        Assert.Equal("daily", env["CV4PVE_AUTOSNAP_LABEL"]);
        Assert.Equal("7", env["CV4PVE_AUTOSNAP_KEEP"]);
        Assert.Equal("autodaily260929020000", env["CV4PVE_AUTOSNAP_SNAP_NAME"]);
        Assert.Equal("1", env["CV4PVE_AUTOSNAP_VMSTATE"]);
        Assert.Equal("1.5", env["CV4PVE_AUTOSNAP_DURATION"]);
        Assert.Equal("1", env["CV4PVE_AUTOSNAP_STATE"]);
    }

    [Fact]
    public void Job_phase_leaves_the_guest_empty()
    {
        var env = new PhaseEventArgs(HookPhase.SnapJobStart, null, "daily", 7, null, false, 0, false).Environments;

        Assert.Equal("", env["CV4PVE_AUTOSNAP_VMID"]);
        Assert.Equal("", env["CV4PVE_AUTOSNAP_VMNAME"]);
        Assert.Equal("", env["CV4PVE_AUTOSNAP_VMTYPE"]);
        Assert.Equal("", env["CV4PVE_AUTOSNAP_SNAP_NAME"]);
        Assert.Equal("0", env["CV4PVE_AUTOSNAP_VMSTATE"]);
        Assert.Equal("0", env["CV4PVE_AUTOSNAP_STATE"]);
    }
}
