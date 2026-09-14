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

        public int Status { get; private set; }

        /// <summary>The response body; HTML for the panels, JSON for People.</summary>
        public string Html { get; private set; }

        public string ContentType { get; private set; }

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
        /// <param name="target">Request target, e.g. "/shop/" or "/showitem?bundle=lvl_25".</param>
        public static PageResult Route(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                target = "/";
            }

            string path = target;
            string query = string.Empty;
            int q = target.IndexOf('?');
            if (q >= 0)
            {
                path = target.Substring(0, q);
                query = target.Substring(q + 1);
            }

            // Chat bot lookups, on people.anarchy-online.com's paths. See People.
            PageResult people = People.Route(path);
            if (people != null)
            {
                return people;
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
