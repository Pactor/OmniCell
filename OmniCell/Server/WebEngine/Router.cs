namespace WebEngine
{
    using System;

    /// <summary>
    /// Maps a request path to a page.
    /// </summary>
    /// <summary>
    /// A routed response: the page, and the status it should carry.
    /// </summary>
    public class PageResult
    {
        public PageResult(int status, string html)
            : this(status, html, "text/html; charset=utf-8")
        {
        }

        public PageResult(int status, string body, string contentType)
        {
            this.Status = status;
            this.Html = body;
            this.ContentType = contentType;
        }

        /// <summary>
        /// A response whose body is already bytes - an image, or JSON built up front.
        /// </summary>
        /// <param name="cacheSeconds">How long the browser may keep it; 0 sends no-store.</param>
        /// <param name="gzipBody">The same body gzip-compressed, sent when the browser accepts gzip.</param>
        public PageResult(int status, byte[] body, string contentType, int cacheSeconds, byte[] gzipBody)
        {
            this.Status = status;
            this.Body = body;
            this.ContentType = contentType;
            this.CacheSeconds = cacheSeconds;
            this.GzipBody = gzipBody;
        }

        public int Status { get; private set; }

        /// <summary>The response body; HTML for the panels, JSON for People.</summary>
        public string Html { get; private set; }

        /// <summary>A byte body. When set, it is sent instead of <see cref="Html"/>.</summary>
        public byte[] Body { get; private set; }

        public byte[] GzipBody { get; private set; }

        public int CacheSeconds { get; private set; }

        public string ContentType { get; private set; }

        /// <summary>Extra response headers, such as Location and Set-Cookie.</summary>
        public System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>> Headers { get; } =
            new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>();

        public PageResult WithHeader(string name, string value)
        {
            this.Headers.Add(new System.Collections.Generic.KeyValuePair<string, string>(name, value));
            return this;
        }

        /// <summary>A 303 See Other to a same-site path.</summary>
        public static PageResult Redirect(string location)
        {
            return new PageResult(303, string.Empty, "text/plain; charset=utf-8").WithHeader("Location", location);
        }

        /// <summary>True when the client asked for something we do not serve.</summary>
        public bool IsUnmapped
        {
            get { return this.Status == 404; }
        }
    }

    public static class Router
    {
        /// <summary>
        /// Picks the page for a request target.
        /// </summary>
        /// <param name="request">The request; its target is e.g. "/shop/" or "/showitem?bundle=lvl_25".</param>
        public static PageResult Route(HttpRequest request)
        {
            string path = request.Path;
            string query = request.Query;

            // Admin sign-in and sign-out. See AdminAuth.
            PageResult admin = AdminAuth.Route(request);
            if (admin != null)
            {
                return admin;
            }

            // The icon browser and the icon images, for signed-in admins only. See IconPages.
            PageResult icons = IconPages.Route(request);
            if (icons != null)
            {
                return icons;
            }

            // Chat bot lookups, on people.anarchy-online.com's paths. See People.
            PageResult people = People.Route(path);
            if (people != null)
            {
                return people;
            }

            // Chat bot history, timers, market and tower lookups. See BotLookups.
            PageResult lookup = BotLookups.Route(path);
            if (lookup != null)
            {
                return lookup;
            }

            // The client is inconsistent about trailing slashes across panels, so
            // match with and without one rather than trusting it to send exactly
            // what we patched in.
            string normalised = Normalise(path);

            if (normalised == Normalise(Endpoints.Shop))
            {
                return Ok(Pages.Stub("Shop", "In-game store panel."));
            }

            if (normalised == Normalise(Endpoints.Market))
            {
                return Ok(Pages.Stub("Market", "Player market panel."));
            }

            if (normalised == Normalise(Endpoints.Petition))
            {
                return Ok(Pages.Stub("Petition", "Submit a petition to the GM team."));
            }

            if (normalised == Normalise(Endpoints.DailyRewards))
            {
                return Ok(Pages.Stub("Daily Rewards", "Daily login rewards panel."));
            }

            if (normalised == Normalise(Endpoints.ShowItem))
            {
                string bundle = BundleFrom(query);
                string subtitle = string.IsNullOrEmpty(bundle)
                                      ? "Item bundle details."
                                      : "Item bundle details for " + bundle + ".";
                return Ok(Pages.Stub("Item", subtitle));
            }

            if (normalised.Length == 0)
            {
                return Ok(Pages.Index());
            }

            return new PageResult(404, Pages.NotFound(path));
        }

        private static PageResult Ok(string html)
        {
            return new PageResult(200, html);
        }

        /// <summary>
        /// Lower-cases and strips surrounding slashes so "/shop/", "/shop" and
        /// "shop/" all compare equal.
        /// </summary>
        private static string Normalise(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Trim('/').ToLowerInvariant();
        }

        /// <summary>
        /// Pulls the bundle value out of a query string such as "bundle=lvl_25".
        /// </summary>
        private static string BundleFrom(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return string.Empty;
            }

            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0 && pair.Substring(0, eq).Equals("bundle", StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Substring(eq + 1);
                }
            }

            return string.Empty;
        }
    }
}
