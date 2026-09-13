namespace WebEngine
{
    using System.Collections.Generic;

    /// <summary>
    /// The in-game browser endpoints, and the paths OmniCell serves them on.
    /// </summary>
    /// <remarks>
    /// The Anarchy Online client embeds Awesomium and navigates to URLs that are
    /// compiled into GUI.dll. The launcher rewrites those strings in memory at
    /// launch so they point here instead. See OmniCell-Launcher's WebUrlPatch.
    ///
    /// Two of the originals cannot keep their exact paths. Funcom served each
    /// endpoint from its own hostname, so aopetition had no path at all and
    /// dailyrewards used a bare "/". Pointed at a single host both would collide
    /// on the root, so they get short distinct paths here. The others keep the
    /// trailing-slash form the client already used.
    ///
    /// Paths are deliberately short. The launcher patches these strings in place
    /// and cannot grow them, so a replacement must fit inside the original.
    /// </remarks>
    public static class Endpoints
    {
        public const string Shop = "/shop/";

        public const string Market = "/market/";

        public const string Petition = "/p/";

        public const string DailyRewards = "/daily/";

        public const string ShowItem = "/showitem";

        /// <summary>
        /// Original client URL -> path we serve it on. The launcher uses this to
        /// build its replacement strings, so the two stay in step.
        /// </summary>
        public static readonly IList<UrlMapping> Mappings = new List<UrlMapping>
        {
            new UrlMapping("http://aoshop.funcom.com/shop/", Shop),
            new UrlMapping("http://aomarket.funcom.com/market/", Market),
            new UrlMapping("http://aopetition.funcom.com", Petition),
            new UrlMapping("http://dailyrewards.anarchy-online.com/", DailyRewards),

            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_25", ShowItem + "?bundle=lvl_25"),
            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_60", ShowItem + "?bundle=lvl_60"),
            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_100", ShowItem + "?bundle=lvl_100"),
            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_150", ShowItem + "?bundle=lvl_150"),
            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_200", ShowItem + "?bundle=lvl_200"),
            new UrlMapping("http://aoshop.funcom.com/showitem?bundle=lvl_215", ShowItem + "?bundle=lvl_215"),

            // Awesomium navigation allow list. If these are not patched too, the
            // embedded browser refuses to load the rewritten URLs above.
            new UrlMapping("http://aoshop.funcom.com/*", "/*"),
            new UrlMapping("http://aomarket.funcom.com/*", "/*"),
            new UrlMapping("http://aopetition.funcom.com/*", "/*"),
            new UrlMapping("http://dailyrewards.anarchy-online.com/*", "/*")
        };
    }

    /// <summary>
    /// One client URL and the path OmniCell answers it on.
    /// </summary>
    public class UrlMapping
    {
        public UrlMapping(string original, string path)
        {
            this.Original = original;
            this.Path = path;
        }

        /// <summary>The string as it appears in the client's GUI.dll.</summary>
        public string Original { get; private set; }

        /// <summary>The path appended to our base URL.</summary>
        public string Path { get; private set; }
    }
}
