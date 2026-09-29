/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("daily", "")]
    [InlineData("daily", "yyMMddHHmmss")]
    [InlineData("before-upgrade", "yyMMddHHmm")]
    [InlineData("Daily_2", "yyyyMMdd-HHmm")]
    [InlineData("daily", "yyyyMMdd_HHmmss")]
    [InlineData("daily", "yyMMdd")]
    [InlineData("", "yyMMddHHmmss")]
    public void Valid_label_and_format(string label, string format)
        => Assert.Null(AutoSnapEngine.ValidateLabelAndTimestampFormat(label, format));

    [Theory]
    [InlineData("yyMMddH")]
    [InlineData("yyMMddHmmss")]
    [InlineData("yyMMd")]
    [InlineData("yyMMdd'T'HHmm")]
    public void Format_of_variable_length_is_refused(string format)
        => Assert.Contains("another length", AutoSnapEngine.ValidateLabelAndTimestampFormat("daily", format));

    [Theory]
    [InlineData("ddMMyyHHmm")]
    [InlineData("HHmmyyMMdd")]
    [InlineData("yyMMddhhmm")]
    [InlineData("yyddMMHHmm")]
    public void Format_not_in_time_order_is_refused(string format)
        => Assert.Contains("time order", AutoSnapEngine.ValidateLabelAndTimestampFormat("daily", format));

    [Theory]
    [InlineData("doc.test")]
    [InlineData("doc test")]
    [InlineData("doc/test")]
    public void Label_with_characters_Proxmox_refuses(string label)
        => Assert.Contains("only letters, digits", AutoSnapEngine.ValidateLabelAndTimestampFormat(label, ""));

    [Fact]
    public void Separator_Proxmox_refuses_is_refused()
        => Assert.Contains("only letters, digits", AutoSnapEngine.ValidateLabelAndTimestampFormat("daily", "yyMMdd.HHmm"));

    [Theory]
    [InlineData("abcdefghijklmnopqrstuvwx", "", true)]   // 4 + 24 + 12 = 40
    [InlineData("abcdefghijklmnopqrstuvwxy", "", false)] // 41
    [InlineData("abcdefghijklmnopqrstuvwxyz", "yyMMddHHmm", true)] // 4 + 26 + 10 = 40
    public void Name_is_at_most_40_characters(string label, string format, bool valid)
        => Assert.Equal(valid, AutoSnapEngine.ValidateLabelAndTimestampFormat(label, format) == null);

    [Theory]
    [InlineData(1000, 0.20, 95, true)]
    [InlineData(1000, 0.95, 95, true)]
    [InlineData(1000, 0.96, 95, false)]
    [InlineData(1000, 0.20, 10, false)]
    [InlineData(1000, 0.0, 95, true)]
    [InlineData(0, 0.0, 95, true)]
    [InlineData(0, 0.0, 10, true)]
    public void Storage_threshold_is_a_percentage(double size, double usage, int max, bool expected)
        => Assert.Equal(expected, AutoSnapEngine.IsStorageValid(size, usage, max));

    [Fact]
    public void Snap_fails_when_no_guest_is_found()
        => Assert.False(new ResultSnap { VmsFound = false }.Status);

    [Fact]
    public void Snap_succeeds_when_every_guest_was_skipped()
        => Assert.True(new ResultSnap().Status);

    [Fact]
    public void Snap_fails_when_a_guest_failed()
    {
        var result = new ResultSnap();
        result.Vms.Add(new ResultSnapVm { VmId = 100, Status = true });
        result.Vms.Add(new ResultSnapVm { VmId = 101, Status = false });
        Assert.False(result.Status);
    }
}
