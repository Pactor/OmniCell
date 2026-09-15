namespace WebEngine
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// One parsed request: what the router and the admin pages need to know about it.
    /// </summary>
    public class HttpRequest
    {
        private readonly IDictionary<string, string> headers;

        private Dictionary<string, string> form;

        public HttpRequest(string method, string target, IDictionary<string, string> headers, string body, IPAddress remoteAddress)
        {
            this.Method = (method ?? string.Empty).ToUpperInvariant();
            this.Target = string.IsNullOrEmpty(target) ? "/" : target;
            this.headers = headers;
            this.Body = body ?? string.Empty;
            this.RemoteAddress = remoteAddress ?? IPAddress.None;

            int q = this.Target.IndexOf('?');
            this.Path = q >= 0 ? this.Target.Substring(0, q) : this.Target;
            this.Query = q >= 0 ? this.Target.Substring(q + 1) : string.Empty;
        }

        public string Method { get; private set; }

        /// <summary>The request target as sent, e.g. "/showitem?bundle=lvl_25".</summary>
        public string Target { get; private set; }

        public string Path { get; private set; }

        public string Query { get; private set; }

        public string Body { get; private set; }

        public IPAddress RemoteAddress { get; private set; }

        public bool AcceptsGzip
        {
            get { return this.Header("Accept-Encoding").IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0; }
        }

        public string Header(string name)
        {
            string value;
            return this.headers.TryGetValue(name, out value) ? value : string.Empty;
        }

        public string Cookie(string name)
        {
            foreach (string part in this.Header("Cookie").Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0 && part.Substring(0, eq).Trim() == name)
                {
                    return part.Substring(eq + 1).Trim();
                }
            }

            return string.Empty;
        }

        public string QueryValue(string name)
        {
            string value;
            return ParseUrlEncoded(this.Query).TryGetValue(name, out value) ? value : string.Empty;
        }

        /// <summary>A field from an application/x-www-form-urlencoded POST body.</summary>
        public string FormValue(string name)
        {
            if (this.form == null)
            {
                this.form = ParseUrlEncoded(this.Body);
            }

            string value;
            return this.form.TryGetValue(name, out value) ? value : string.Empty;
        }

        private static Dictionary<string, string> ParseUrlEncoded(string text)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text))
            {
                return values;
            }

            foreach (string pair in text.Split('&'))
            {
                int eq = pair.IndexOf('=');
                string key = WebUtility.UrlDecode(eq >= 0 ? pair.Substring(0, eq) : pair);
                string value = eq >= 0 ? WebUtility.UrlDecode(pair.Substring(eq + 1)) : string.Empty;
                if (!values.ContainsKey(key))
                {
                    values.Add(key, value);
                }
            }

            return values;
        }
    }
}
