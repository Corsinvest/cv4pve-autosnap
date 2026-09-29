/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Newtonsoft.Json;
using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

/// <summary>
/// The library runs with the culture of its host (the CLI forces the invariant culture, cv4pve-admin does
/// not): names, hook variables and checks must not depend on it.
/// </summary>
public class CultureTests
{
    public static TheoryData<string> Cultures => ["", "en-US", "en-GB", "de-DE", "it-IT", "fr-FR", "th-TH", "ar-SA", "fa-IR"];

    private static T InCulture<T>(string culture, Func<T> action)
    {
        var current = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return action();
        }
        finally { CultureInfo.CurrentCulture = current; }
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Hook_duration_uses_a_dot(string culture)
    {
        var env = InCulture(culture, () => new PhaseEventArgs(HookPhase.SnapJobEnd, null, "daily", 7, null, false, 1234.5, true).Environments);
        Assert.Equal("1234.5", env["CV4PVE_AUTOSNAP_DURATION"]);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Snapshot_name_timestamp_is_the_same_in_every_culture(string culture)
    {
        var date = new DateTime(2026, 9, 29, 2, 0, 0);
        Assert.Equal("autodaily260929020000", InCulture(culture, () => AutoSnapEngine.GetSnapName("daily", "yyMMddHHmmss", date)));
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Validation_does_not_depend_on_the_culture(string culture)
    {
        Assert.Null(InCulture(culture, () => AutoSnapEngine.ValidateLabelAndTimestampFormat("daily", "yyyyMMdd-HHmm")));
        Assert.NotNull(InCulture(culture, () => AutoSnapEngine.ValidateLabelAndTimestampFormat("daily", "ddMMyyHHmm")));
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Guest_config_with_decimals_is_read(string culture)
    {
        //issue #97: a network rate limit with decimals stopped the snapshot
        var config = InCulture(culture, () => JsonConvert.DeserializeObject<VmConfigQemu>(
            """{"net0":"virtio=BC:24:11:00:00:01,bridge=vmbr0,rate=0.13","scsi0":"local-zfs:vm-100-disk-0,size=32G","agent":"1"}""")!);

        Assert.Equal(0.13, InCulture(culture, () => Assert.Single(config.Networks).Rate));
        Assert.Equal("local-zfs", InCulture(culture, () => Assert.Single(config.Disks).Storage));
        Assert.True(config.AgentEnabled);
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void Storage_threshold_does_not_depend_on_the_culture(string culture)
    {
        Assert.False(InCulture(culture, () => AutoSnapEngine.IsStorageValid(1000, 0.2, 10)));
        Assert.True(InCulture(culture, () => AutoSnapEngine.IsStorageValid(1000, 0.2, 95)));
    }
}
