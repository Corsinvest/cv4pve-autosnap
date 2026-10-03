/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Net;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Corsinvest.ProxmoxVE.AutoSnap.Api.Tests;

/// <summary>
/// A snapshot request refused by Proxmox VE starts no task: the reason of the refusal is reported,
/// not an error raised while looking for a task that does not exist.
/// </summary>
public class RefusedRequestTests
{
    private const string Upid = "UPID:pve01:0012A3F4:05C1B2D3:6720F1A0:qmsnapshot:100:root@pam:";

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json, string? reason = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (reason != null) { response.ReasonPhrase = reason; }
        return response;
    }

    private static async Task<(bool InError, string Output)> CheckAsync(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var client = new PveClient("pve01", 8006, new HttpClient(new Handler(respond)));
        var engine = new AutoSnapEngine(client, NullLoggerFactory.Instance, TextWriter.Null, false);
        var writer = new StringWriter();

        var result = await client.CreateAsync("/nodes/pve01/qemu/100/snapshot");

        return (await engine.CheckResultAsync(result, writer), writer.ToString());
    }

    [Fact]
    public async Task Request_refused_for_a_missing_privilege_reports_the_reason()
    {
        var (inError, output) = await CheckAsync(_ => Json(HttpStatusCode.Forbidden,
                                                           """{"data":null}""",
                                                           "Permission check failed (/vms/100, VM.Snapshot)"));

        Assert.True(inError);
        Assert.Contains("Permission check failed (/vms/100, VM.Snapshot)", output);
    }

    [Fact]
    public async Task Request_refused_for_a_parameter_reports_the_parameter()
    {
        var (inError, output) = await CheckAsync(_ => Json(HttpStatusCode.BadRequest,
                                                           """{"data":null,"errors":{"snapname":"invalid format"}}""",
                                                           "Parameter verification failed."));

        Assert.True(inError);
        Assert.Contains("snapname : invalid format", output);
    }

    [Fact]
    public async Task Task_ended_ok_is_not_an_error()
    {
        var (inError, output) = await CheckAsync(request => request.Method == HttpMethod.Post
            ? Json(HttpStatusCode.OK, """{"data":"UPID"}""".Replace("UPID", Upid))
            : Json(HttpStatusCode.OK, """{"data":{"status":"stopped","exitstatus":"OK"}}"""));

        Assert.False(inError);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task Task_ended_with_an_error_reports_it()
    {
        var (inError, output) = await CheckAsync(request => request.Method == HttpMethod.Post
            ? Json(HttpStatusCode.OK, """{"data":"UPID"}""".Replace("UPID", Upid))
            : Json(HttpStatusCode.OK, """{"data":{"status":"stopped","exitstatus":"snapshot name 'auto' already used"}}"""));

        Assert.True(inError);
        Assert.Contains("snapshot name 'auto' already used", output);
    }

    [Fact]
    public async Task Snapshot_refused_for_a_missing_privilege_reports_the_reason()
    {
        // the path of the engine: the snapshot helper of the library, then the check of its result
        var client = new PveClient("pve01", 8006, new HttpClient(new Handler(_ => Json(HttpStatusCode.Forbidden,
                                                                                         """{"data":null}""",
                                                                                         "Permission check failed (/vms/100, VM.Snapshot)"))));
        var engine = new AutoSnapEngine(client, NullLoggerFactory.Instance, TextWriter.Null, false);
        var writer = new StringWriter();

        var result = await SnapshotHelper.CreateSnapshotAsync(client, "pve01", VmType.Qemu, 100, "auto", "cv4pve-autosnap", false, 1000);

        Assert.True(await engine.CheckResultAsync(result, writer));
        Assert.Contains("Permission check failed (/vms/100, VM.Snapshot)", writer.ToString());
    }
}
