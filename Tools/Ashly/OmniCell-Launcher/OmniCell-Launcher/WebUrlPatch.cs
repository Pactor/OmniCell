namespace OmniCell_Launcher
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Repoints the client's in-game browser at an OmniCell WebEngine.
    /// </summary>
    /// <remarks>
    /// The Anarchy Online client embeds Awesomium and navigates to URLs compiled
    /// into GUI.dll. Rather than editing that file on disk, which the patcher
    /// would overwrite and which needs the game directory to be writable, this
    /// rewrites the strings in the running process. The launcher already does
    /// the same for the Diffie-Hellman key.
    ///
    /// Two rules govern the replacements, and breaking either corrupts the
    /// client:
    ///
    /// 1. A replacement can never be longer than the original. The strings sit
    ///    in .rdata with only a few NUL bytes of slack, so overflowing one runs
    ///    into the next. Injector.ReplacePadded refuses rather than overflow.
    ///
    /// 2. The navigation URL and its "/*" allow-list twin must both be patched.
    ///    Awesomium checks the target against that list, so patching only the
    ///    URL leaves the browser refusing to load it.
    ///
    /// Paths here must match WebEngine's Endpoints class.
    /// </remarks>
    public static class WebUrlPatch
    {
        /// <summary>
        /// Original client string, and the path to append to our base URL.
        /// A null path means the base URL alone.
        /// </summary>
        private static readonly List<KeyValuePair<string, string>> Urls =
            new List<KeyValuePair<string, string>>
            {
                // Navigation targets.
                new KeyValuePair<string, string>("http://aoshop.funcom.com/shop/", "/shop/"),
                new KeyValuePair<string, string>("http://aomarket.funcom.com/market/", "/market/"),
                new KeyValuePair<string, string>("http://aopetition.funcom.com", "/p/"),
                new KeyValuePair<string, string>("http://dailyrewards.anarchy-online.com/", "/daily/"),

                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_25", "/showitem?bundle=lvl_25"),
                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_60", "/showitem?bundle=lvl_60"),
                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_100", "/showitem?bundle=lvl_100"),
                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_150", "/showitem?bundle=lvl_150"),
                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_200", "/showitem?bundle=lvl_200"),
                new KeyValuePair<string, string>(
                    "http://aoshop.funcom.com/showitem?bundle=lvl_215", "/showitem?bundle=lvl_215"),

                // Awesomium allow list.
                new KeyValuePair<string, string>("http://aoshop.funcom.com/*", "/*"),
                new KeyValuePair<string, string>("http://aomarket.funcom.com/*", "/*"),
                new KeyValuePair<string, string>("http://aopetition.funcom.com/*", "/*"),
                new KeyValuePair<string, string>("http://dailyrewards.anarchy-online.com/*", "/*")
            };

        /// <summary>
        /// Rewrites the client's browser URLs to point at <paramref name="baseUrl"/>.
        /// </summary>
        /// <param name="processId">The running client.</param>
        /// <param name="processHandle">Handle to the same process.</param>
        /// <param name="baseUrl">Scheme and host, no trailing slash, e.g. "http://10.0.0.5".</param>
        /// <param name="report">Human readable outcome, listing anything skipped.</param>
        /// <returns>True if every string was either patched or already absent.</returns>
        public static bool Apply(int processId, IntPtr processHandle, string baseUrl, out string report)
        {
            StringBuilder problems = new StringBuilder();
            int patched = 0;
            int missing = 0;

            baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');

            foreach (KeyValuePair<string, string> entry in Urls)
            {
                string original = entry.Key;
                string replacement = baseUrl + entry.Value;

                if (replacement.Length > original.Length)
                {
                    problems.AppendLine(
                        "  too long (" + replacement.Length + " > " + original.Length + "): " + replacement);
                    continue;
                }

                int count = Injector.ReplacePadded(processId, processHandle, original, replacement);
                if (count > 0)
                {
                    patched += count;
                }
                else
                {
                    missing++;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Patched " + patched + " URL string(s).");
            if (missing > 0)
            {
                sb.AppendLine(
                    missing + " of " + Urls.Count + " were not found in memory. That is expected if "
                    + "GUI.dll has not loaded yet, or if this client build uses different URLs.");
            }

            if (problems.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Skipped, because the replacement will not fit inside the original:");
                sb.Append(problems);
                sb.AppendLine();
                sb.AppendLine(
                    "Use a shorter address. Serving on port 80 removes the \":port\" suffix and "
                    + "usually saves enough characters.");
            }

            report = sb.ToString();
            return problems.Length == 0;
        }

        /// <summary>
        /// Longest base URL that every replacement can accommodate. Useful for
        /// warning before launch rather than after.
        /// </summary>
        public static int LongestUsableBaseUrl()
        {
            int limit = int.MaxValue;
            foreach (KeyValuePair<string, string> entry in Urls)
            {
                int allowed = entry.Key.Length - entry.Value.Length;
                if (allowed < limit)
                {
                    limit = allowed;
                }
            }

            return limit;
        }
    }
}
