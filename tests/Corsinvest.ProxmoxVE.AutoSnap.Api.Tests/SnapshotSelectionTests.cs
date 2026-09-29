/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

public class SnapshotSelectionTests
{
    private const string Format = "yyMMddHHmmss";

    private static VmSnapshot Snap(string name, string description = "cv4pve-autosnap")
        => new() { Name = name, Description = description };

    private static readonly VmSnapshot[] Snapshots =
    [
        Snap("autodaily260926020000"),
        Snap("autodaily260927020000"),
        Snap("autodaily260928020000"),
        Snap("autodaily260929020000"),
        Snap("autodaily2260929020000"),
        Snap("autohourly260929020000"),
        Snap("autodaily260925020000", "taken by hand"),
        Snap("before-upgrade", ""),
    ];

    private static string[] Names(IEnumerable<VmSnapshot> snapshots) => [.. snapshots.Select(a => a.Name)];

    [Fact]
    public void Only_snapshots_with_the_tool_description_are_its_own()
        => Assert.DoesNotContain(AutoSnapEngine.FilterApp(Snapshots), a => a.Name is "autodaily260925020000" or "before-upgrade");

    [Fact]
    public void Description_with_a_trailing_newline_is_recognised()
        => Assert.Single(AutoSnapEngine.FilterApp([Snap("autodaily260929020000", "cv4pve-autosnap\n")]));

    [Fact]
    public void Label_selects_only_its_snapshots()
        => Assert.Equal(["autodaily260926020000", "autodaily260927020000", "autodaily260928020000", "autodaily260929020000"],
                        Names(AutoSnapEngine.FilterLabel(Snapshots, "daily", Format)));

    [Fact]
    public void Label_that_prefixes_another_is_kept_apart()
        => Assert.Equal(["autodaily2260929020000"], Names(AutoSnapEngine.FilterLabel(Snapshots, "daily2", Format)));

    [Fact]
    public void Snapshot_of_another_timestamp_length_is_not_recognised()
        => Assert.Empty(AutoSnapEngine.FilterLabel([Snap("autodaily2609290200")], "daily", Format));

    [Theory]
    [InlineData(2, new[] { "autodaily260926020000", "autodaily260927020000" })]
    [InlineData(3, new[] { "autodaily260926020000" })]
    [InlineData(0, new[] { "autodaily260926020000", "autodaily260927020000", "autodaily260928020000", "autodaily260929020000" })]
    [InlineData(4, new string[0])]
    [InlineData(10, new string[0])]
    public void Retention_removes_the_oldest_beyond_keep(int keep, string[] expected)
        => Assert.Equal(expected, Names(AutoSnapEngine.GetSnapshotsToRemove(Snapshots, "daily", keep, Format)));

    [Fact]
    public void Retention_sorts_by_name_not_by_list_order()
        => Assert.Equal(["autodaily260926020000"],
                        Names(AutoSnapEngine.GetSnapshotsToRemove([.. Snapshots.Reverse()], "daily", 3, Format)));

    [Fact]
    public void Retention_uses_the_default_format_when_none_is_given()
        => Assert.Equal(["autodaily260926020000"], Names(AutoSnapEngine.GetSnapshotsToRemove(Snapshots, "daily", 3, "")));

    [Theory]
    [InlineData("autodaily260929020000", Format, "daily")]
    [InlineData("auto2hourly260929020000", Format, "2hourly")]
    [InlineData("autodaily20260929-0200", "yyyyMMdd-HHmm", "daily")]
    [InlineData("autodaily260929020000", "", "daily")]
    [InlineData("auto260929020000", Format, "")]
    public void Label_is_read_back_from_the_name(string name, string format, string expected)
        => Assert.Equal(expected, AutoSnapEngine.GetLabelFromName(name, format));
}
