/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport.Channels;

internal class LocalUtilsChannel : Channel<LocalUtils>
{
    public LocalUtilsChannel(string guid, Connection connection, LocalUtils owner) : base(guid, connection, owner)
    {
    }

    internal Task ZipAsync(string zipFile, List<NameValue> entries, string mode, string stacksId, bool includeSources) =>
        Connection.SendMessageToServerAsync(Object, "zip", new Dictionary<string, object>
        {
                { "zipFile", zipFile },
                { "entries", entries },
                { "mode", mode },
                { "stacksId", stacksId },
                { "includeSources", includeSources },
        });

    internal async Task<(string HarId, string Error)> HarOpenAsync(string file)
    {
        var response = await Connection.SendMessageToServerAsync(Object, "harOpen", new Dictionary<string, object>
            {
                  { "file", file },
            }).ConfigureAwait(false);
        return (response.GetString("harId", true), response.GetString("error", true));
    }

    internal async Task<LocalUtilsHarLookupResult> HarLookupAsync(
        string harId,
        string url,
        string method,
        List<Header> headers,
        byte[] postData,
        bool isNavigationRequest)
    {
        var response = await Connection.SendMessageToServerAsync<LocalUtilsHarLookupResult>(Object, "harLookup", new Dictionary<string, object>
            {
                { "harId", harId },
                { "url", url },
                { "method", method },
                { "headers", headers },
                { "postData", postData != null ? Convert.ToBase64String(postData) : null },
                { "isNavigationRequest", isNavigationRequest },
            }).ConfigureAwait(false);
        return response;
    }

    internal Task HarCloseAsync(string harId) =>
        Connection.SendMessageToServerAsync(Object, "HarCloseAsync", new Dictionary<string, object>
        {
                  { "harId", harId },
        });

    internal Task HarUnzipAsync(string zipFile, string harFile) =>
        Connection.SendMessageToServerAsync(Object, "harUnzip", new Dictionary<string, object>
        {
                  { "zipFile", zipFile },
                  { "harFile", harFile },
        });

    internal async Task<JsonPipe> ConnectAsync(string wsEndpoint, IEnumerable<KeyValuePair<string, string>> headers, float? slowMo, float? timeout, string exposeNetwork)
    {
        var args = new Dictionary<string, object>
            {
                { "wsEndpoint", wsEndpoint },
                { "headers", headers },
                { "slowMo", slowMo },
                { "timeout", timeout },
                { "exposeNetwork", exposeNetwork },
            };
        return (await Connection.SendMessageToServerAsync(Object, "connect", args).ConfigureAwait(false)).Value.GetObject<JsonPipe>("pipe", Connection);
    }

    internal void AddStackToTracingNoReply(List<StackFrame> frames, int id)
        => Connection.SendMessageToServerAsync(Object, "addStackToTracingNoReply", new Dictionary<string, object>
        {
            {
                "callData", new ClientSideCallMetadata()
                {
                    Id = id,
                    Stack = frames,
                }
            },
        }).IgnoreException();

    internal Task TraceDiscardedAsync(string stacksId)
        => Connection.SendMessageToServerAsync(Object, "traceDiscarded", new Dictionary<string, object>
        {
            { "stacksId", stacksId },
        });

    internal async Task<string> TracingStartedAsync(string tracesDir, string traceName)
    {
        var response = await Connection.SendMessageToServerAsync(Object, "tracingStarted", new Dictionary<string, object>
        {
            { "tracesDir", tracesDir },
            { "traceName", traceName },
        }).ConfigureAwait(false);
        return response.GetString("stacksId", true);
    }
}
