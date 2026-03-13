using AlloyClient.State;
using AlloyClient.Utils;
using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AlloyClient.AppEngine;

public static class AppEngineClient {
    private static readonly Logger Log = new("AppEngine");

    private static readonly HttpClient Client;

    static AppEngineClient() {

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip
        };

        Client = new HttpClient(handler);
        Client.BaseAddress = new Uri(Settings.AppEngineUrl);

        Client.DefaultRequestHeaders.Add("Pragma", "no-cache");
        Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows; U; en-US) AppleWebKit/533.19.4 (KHTML, like Gecko) AdobeAIR/50.0");
        Client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
        Client.DefaultRequestHeaders.Add("Referer", "app:/Main.dll");
        Client.DefaultRequestHeaders.Add("x-flash-version", "50,0,1,2");
    }

    public static async Task<string> SendRequest(string endpoint, Dictionary<string, string> data = null, int retries = 0) {
        return await SendClientRequest(Client, endpoint, data, retries);
    }

    public static async Task<byte[]> SendRequestRaw(string endpoint, Dictionary<string, string> data = null, int retries = 0)
    {
        var content = data == null ? null : new FormUrlEncodedContent(data);
        var cts = new CancellationTokenSource(Settings.AppEngineTimeout);

        for (var i = 0; i <= retries; i++)
        {
            Log.Trace($"[Raw] Sending request to {endpoint}." + (i == 0 ? "" : $" Attempt {i + 1} of {retries + 1}."));
            try
            {
                using var response = await Client.PostAsync(endpoint, content, cts.Token);
                return await response.Content.ReadAsByteArrayAsync(cts.Token);
            }
            catch (Exception e)
            {
                Log.Error($"[Raw] Attempt {i + 1} failed: {e.Message}");
                if (i == retries)
                {
                    Log.Error("[Raw] All attempts failed.");
                    return null;
                }
            }
        }

        return null;
    }

    private static async Task<string> SendClientRequest(HttpClient client, string endpoint, Dictionary<string, string> data = null, int retries = 0) {
        var cancellationTokenSource = new CancellationTokenSource(Settings.AppEngineTimeout);
        var content = data == null ? null : new FormUrlEncodedContent(data);

        for (var i = 0; i <= retries; i++) {
            Log.Trace($"Sending request to {endpoint}." + (i == 0 ? "" : $" Attempt {i + 1} of {retries + 1}."));
           
            try {
                var response = await client.PostAsync(endpoint, content, cancellationTokenSource.Token);
                return await response.Content.ReadAsStringAsync(cancellationTokenSource.Token);
            } catch (Exception e) {
                Log.Error($"Attempt {i + 1} failed: {e}");

                if (i != retries) {
                    continue;
                }

                Log.Error("All attempts failed.");

                return null;
            }
        }

        return null;
    }

    private static byte[] DecompressZLib(byte[] compressed)
    {
        try
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception e)
        {
            Log.Warn($"ZLib decompression failed ({e.Message}), trying raw deflate...");
            return DecompressRawDeflate(compressed);
        }
    }

    private static byte[] DecompressRawDeflate(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DecompressGZip(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gz.CopyTo(output);
        return output.ToArray();
    }

}

