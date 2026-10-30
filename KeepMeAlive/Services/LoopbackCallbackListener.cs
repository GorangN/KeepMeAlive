using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Accepts browser callbacks on explicitly registered IPv4 loopback addresses.</summary>
public sealed class LoopbackCallbackListener : ILoopbackCallbackListener
{
    /// <inheritdoc/>
    public async Task<Uri> ListenAsync(Func<Uri, Task> launch, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        TcpListener? listener = null;
        var port = 0;
        foreach (var candidate in new[] { 49152, 49153, 49154 })
        {
            var attempt = new TcpListener(IPAddress.Loopback, candidate);
            try { attempt.Start(4); listener = attempt; port = candidate; break; }
            catch (SocketException) { attempt.Stop(); }
        }
        if (listener is null) { throw new IOException("The sign-in callback ports are busy. Close the other sign-in window and retry."); }
        try
        {
            var redirect = new Uri($"http://127.0.0.1:{port}/callback/");
            await launch(redirect).ConfigureAwait(false);
            while (true)
            {
                using var client = await listener.AcceptTcpClientAsync(timeout.Token).ConfigureAwait(false);
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                readTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var stream = client.GetStream();
                var bytes = new byte[8192];
                var length = 0;
                while (length < bytes.Length)
                {
                    var read = await stream.ReadAsync(bytes.AsMemory(length), readTimeout.Token).ConfigureAwait(false);
                    if (read == 0) { break; }
                    length += read;
                    if (Encoding.ASCII.GetString(bytes, 0, length).Contains("\r\n\r\n", StringComparison.Ordinal)) { break; }
                }
                var request = Encoding.ASCII.GetString(bytes, 0, length);
                var firstLine = request.Split("\r\n", 2)[0].Split(' ');
                if (firstLine.Length != 3 || firstLine[0] != "GET" || !firstLine[1].StartsWith("/callback/?", StringComparison.Ordinal) ||
                    !request.Split("\r\n").Any(line => string.Equals(line, $"Host: 127.0.0.1:{port}", StringComparison.OrdinalIgnoreCase)))
                {
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"), timeout.Token).ConfigureAwait(false);
                    continue;
                }
                const string body = "Sign-in response received. You can close this tab and return to KeepMeAlive.";
                var response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\n\r\n{body}";
                await stream.WriteAsync(Encoding.UTF8.GetBytes(response), timeout.Token).ConfigureAwait(false);
                return new Uri(redirect, firstLine[1]);
            }
        }
        finally { listener.Stop(); }
    }
}
