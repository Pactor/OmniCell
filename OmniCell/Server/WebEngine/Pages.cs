namespace WebEngine
{
    using System;

    /// <summary>
    /// The stub pages. Every endpoint the client asks for answers with one of
    /// these until the real content exists.
    /// </summary>
    public static class Pages
    {
        private const string Style =
            "<style>"
            + "html,body{margin:0;padding:0;height:100%;"
            + "background:#0d1117;color:#c9d1d9;"
            + "font-family:'Segoe UI',Tahoma,sans-serif;}"
            + ".wrap{display:flex;flex-direction:column;align-items:center;"
            + "justify-content:center;height:100%;text-align:center;padding:24px;"
            + "box-sizing:border-box;}"
            + "h1{margin:0 0 8px;font-size:24px;font-weight:600;color:#e6edf3;}"
            + ".sub{margin:0 0 24px;font-size:13px;color:#8b949e;}"
            + ".soon{display:inline-block;padding:10px 20px;border:1px solid #30363d;"
            + "border-radius:6px;background:#161b22;font-size:15px;color:#58a6ff;}"
            + ".foot{margin-top:28px;font-size:11px;color:#484f58;}"
            + "</style>";

        /// <summary>
        /// Builds a stub page.
        /// </summary>
        /// <param name="title">Heading shown to the player.</param>
        /// <param name="subtitle">One line explaining what this panel will be.</param>
        public static string Stub(string title, string subtitle)
        {
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
                   + "<title>" + Escape(title) + "</title>" + Style + "</head><body>"
                   + "<div class=\"wrap\">"
                   + "<h1>" + Escape(title) + "</h1>"
                   + "<p class=\"sub\">" + Escape(subtitle) + "</p>"
                   + "<div class=\"soon\">More to come</div>"
                   + "<p class=\"foot\">OmniCell</p>"
                   + "</div></body></html>";
        }

        /// <summary>
        /// Root page. Not requested by the client, but useful when the operator
        /// opens the server in a normal browser to check it is up.
        /// </summary>
        public static string Index()
        {
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
                   + "<title>OmniCell WebEngine</title>" + Style + "</head><body>"
                   + "<div class=\"wrap\">"
                   + "<h1>OmniCell WebEngine</h1>"
                   + "<p class=\"sub\">Serving the in-game browser panels.</p>"
                   + "<div class=\"soon\">"
                   + "<div>" + Endpoints.Shop + "</div>"
                   + "<div>" + Endpoints.Market + "</div>"
                   + "<div>" + Endpoints.Petition + "</div>"
                   + "<div>" + Endpoints.DailyRewards + "</div>"
                   + "<div>" + Endpoints.ShowItem + "</div>"
                   + "</div>"
                   + "<p class=\"sub\" style=\"margin-top:20px\"><a href=\"" + IconPages.Page
                   + "\" style=\"color:#58a6ff\">Browse item icons</a></p>"
                   + "<p class=\"foot\">Running since " + DateTime.Now.ToString("u") + "</p>"
                   + "</div></body></html>";
        }

        /// <summary>
        /// Shown for a path the client asked for that we do not recognise. Worth
        /// noticing rather than hiding: it means the client has an endpoint the
        /// mapping table has missed.
        /// </summary>
        public static string NotFound(string path)
        {
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">"
                   + "<title>Not mapped</title>" + Style + "</head><body>"
                   + "<div class=\"wrap\">"
                   + "<h1>Not mapped</h1>"
                   + "<p class=\"sub\">The client asked for a path OmniCell does not serve yet.</p>"
                   + "<div class=\"soon\">" + Escape(path) + "</div>"
                   + "<p class=\"foot\">Add it to Endpoints.Mappings</p>"
                   + "</div></body></html>";
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
        }
    }
}
