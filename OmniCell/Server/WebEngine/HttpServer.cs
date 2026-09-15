namespace WebEngine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// A small HTTP/1.1 server for the in-game browser panels.
    /// </summary>
    /// <remarks>
    /// Deliberately built on <see cref="TcpListener"/> rather than HttpListener.
    /// HttpListener sits on http.sys, which refuses any prefix broader than
    /// localhost unless the URL is reserved with netsh or the process is
    /// elevated. A raw socket has no such requirement, so the operator can run
    /// this as an ordinary user on any port.
    ///
    /// Only what the client needs is implemented: GET, a request line, headers
    /// we skip, and a response with an explicit Content-Length.
    /// </remarks>
    public class HttpServer
    {
        private const int HeaderReadTimeoutMs = 10000;

        private const int MaxRequestLineBytes = 8192;

        private const int MaxHeaders = 100;

        private const int MaxBodyBytes = 16 * 1024;

        /// <summary>
        /// Connections served at once. Each one has its own thread, and a scanner opening connections
        /// faster than they time out used to get a thread for every one of them.
        /// </summary>
        private const int MaxConnections = 64;

        private readonly SemaphoreSlim connectionSlots = new SemaphoreSlim(MaxConnections, MaxConnections);

        private readonly IPAddress bindAddress;

        private readonly int port;

        private TcpListener listener;

        private volatile bool running;

        public HttpServer(IPAddress bindAddress, int port)
        {
            this.bindAddress = bindAddress;
            this.port = port;
        }

        /// <summary>
        /// Raised for every request, so the console can log it.
        /// </summary>
        public event Action<string, string, string> RequestHandled;

        /// <summary>
        /// Raised when a connection fails. Kept separate from request logging so
        /// a scanner hitting the port does not look like a client problem.
        /// </summary>
        public event Action<string> Failed;

        public void Start()
        {
            this.listener = new TcpListener(this.bindAddress, this.port);
            this.listener.Start();
            this.running = true;

            Thread accept = new Thread(this.AcceptLoop);
            accept.IsBackground = true;
            accept.Name = "WebEngine accept";
            accept.Start();
        }

        public void Stop()
        {
            this.running = false;
            if (this.listener != null)
            {
                this.listener.Stop();
            }
        }

        private void AcceptLoop()
        {
            while (this.running)
            {
                TcpClient client;
                try
                {
                    client = this.listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    // Stop() closes the listener out from under us. Expected.
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }

                if (!this.connectionSlots.Wait(0))
                {
                    client.Close();
                    continue;
                }

                Thread worker = new Thread(
                    () =>
                    {
                        try
                        {
                            this.Handle(client);
                        }
                        finally
                        {
                            this.connectionSlots.Release();
                        }
                    });
                worker.IsBackground = true;
                worker.Start();
            }
        }

        private void Handle(TcpClient client)
        {
            string remote = "?";
            try
            {
                if (client.Client.RemoteEndPoint != null)
                {
                    remote = client.Client.RemoteEndPoint.ToString();
                }

                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = HeaderReadTimeoutMs;
                    stream.WriteTimeout = HeaderReadTimeoutMs;

                    string requestLine = ReadLine(stream);
                    if (string.IsNullOrEmpty(requestLine))
                    {
                        return;
                    }

                    // Read every header; the client will not consider the exchange complete until
                    // they have been read. Cookie, Content-Length and Accept-Encoding are used.
                    Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int count = 0; count < MaxHeaders; count++)
                    {
                        string header = ReadLine(stream);
                        if (string.IsNullOrEmpty(header))
                        {
                            break;
                        }

                        int colon = header.IndexOf(':');
                        if (colon > 0)
                        {
                            headers[header.Substring(0, colon).Trim()] = header.Substring(colon + 1).Trim();
                        }
                    }

                    string[] parts = requestLine.Split(' ');
                    string method = parts.Length > 0 ? parts[0] : string.Empty;
                    string target = parts.Length > 1 ? parts[1] : "/";

                    // Only the admin sign-in form sends a body, and it is small.
                    string body = string.Empty;
                    string lengthHeader;
                    int length;
                    if (headers.TryGetValue("Content-Length", out lengthHeader) && int.TryParse(lengthHeader, out length) && length > 0)
                    {
                        if (length > MaxBodyBytes)
                        {
                            Respond(stream, new PageResult(413, "Request body too large", "text/plain; charset=utf-8"), false);
                            return;
                        }

                        byte[] buffer = new byte[length];
                        int read = 0;
                        while (read < length)
                        {
                            int n = stream.Read(buffer, read, length - read);
                            if (n <= 0)
                            {
                                throw new IOException("Body ended early");
                            }

                            read += n;
                        }

                        body = Encoding.UTF8.GetString(buffer);
                    }

                    IPEndPoint endPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    HttpRequest request = new HttpRequest(method, target, headers, body, endPoint != null ? endPoint.Address : null);

                    PageResult page = Router.Route(request);
                    this.Announce(remote, method, target + (page.IsUnmapped ? "   <-- NOT MAPPED" : string.Empty));
                    Respond(stream, page, request.AcceptsGzip);
                }
            }
            catch (IOException)
            {
                // Client hung up or timed out mid-request. Not worth a stack trace.
                this.Announce(remote, "-", "(connection dropped)");
            }
            catch (Exception ex)
            {
                Action<string> failed = this.Failed;
                if (failed != null)
                {
                    failed(remote + " " + ex.Message);
                }
            }
        }

        private void Announce(string remote, string method, string target)
        {
            Action<string, string, string> handler = this.RequestHandled;
            if (handler != null)
            {
                handler(remote, method, target);
            }
        }

        /// <summary>
        /// Reads one CRLF-terminated line without over-reading into the body.
        /// </summary>
        private static string ReadLine(Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                if (b == '\n')
                {
                    break;
                }

                if (b != '\r')
                {
                    sb.Append((char)b);
                }

                if (sb.Length > MaxRequestLineBytes)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        private static void Respond(Stream stream, PageResult page, bool acceptsGzip)
        {
            byte[] payload = page.Body ?? Encoding.UTF8.GetBytes(page.Html ?? string.Empty);
            bool gzip = acceptsGzip && page.GzipBody != null;
            if (gzip)
            {
                payload = page.GzipBody;
            }

            int status = page.Status;
            string reason;
            switch (status)
            {
                case 200: reason = " OK"; break;
                case 303: reason = " See Other"; break;
                case 401: reason = " Unauthorized"; break;
                case 403: reason = " Forbidden"; break;
                case 404: reason = " Not Found"; break;
                case 405: reason = " Method Not Allowed"; break;
                case 413: reason = " Payload Too Large"; break;
                case 429: reason = " Too Many Requests"; break;
                case 503: reason = " Service Unavailable"; break;
                default: reason = " Internal Server Error"; break;
            }

            StringBuilder head = new StringBuilder();
            head.Append("HTTP/1.1 ").Append(status).Append(reason).Append("\r\n");
            head.Append("Content-Type: ").Append(page.ContentType).Append("\r\n");
            head.Append("Content-Length: ").Append(payload.Length).Append("\r\n");
            if (gzip)
            {
                head.Append("Content-Encoding: gzip\r\n");
            }

            // Admin-only images are cached by this browser only ("private"), never by a proxy.
            head.Append(page.CacheSeconds > 0
                            ? "Cache-Control: private, max-age=" + page.CacheSeconds + "\r\n"
                            : "Cache-Control: no-cache, no-store\r\n");
            foreach (var header in page.Headers)
            {
                head.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
            }
            head.Append("Connection: close\r\n");
            head.Append("\r\n");

            byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
            stream.Write(headBytes, 0, headBytes.Length);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }
    }
}
