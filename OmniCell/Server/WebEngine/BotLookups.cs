namespace WebEngine
{
    using System;

    /// <summary>
    /// The live-server lookups chat bots make besides People: character history,
    /// world boss and Gauntlet buff timers, market (GMI) orders and tower sites.
    /// </summary>
    /// <remarks>
    /// Tyrbot asks community services (history.aobots.org, timers.aobots.org,
    /// gmi.nadybot.org, towers.aobots.org) for these, and they answer with
    /// Funcom's live servers. OmniCell keeps none of this, so each is answered
    /// with an empty result in the shape Tyrbot reads, rather than a live
    /// server's data:
    ///   history      a JSON list of history rows      (core/lookup/character_history_service.py)
    ///   timers       a JSON list of timers            (modules/standard/timers/world_boss_timers_controller.py)
    ///   gmi          {"buy_orders": [], "sell_orders": []}  (modules/standard/items/gmi_controller.py)
    ///   tower sites  a JSON list of sites             (modules/standard/tower/tower_scout_controller.py)
    /// When the server starts tracking any of these, fill in the answer here.
    /// </remarks>
    public static class BotLookups
    {
        private const string Json = "application/json; charset=utf-8";

        public const string History = "/history";

        public const string BossTimers = "/timers/bosses";

        public const string GauntletTimers = "/timers/gaubuffs";

        public const string GmiPrefix = "/gmi/aoid/";

        public const string TowerSites = "/towers/sites";

        /// <summary>
        /// The answer for a request path without its query string, or null for
        /// any other path.
        /// </summary>
        public static PageResult Route(string path)
        {
            string p = "/" + path.Trim('/').ToLowerInvariant();

            if (p == History || p == BossTimers || p == GauntletTimers || p == TowerSites)
            {
                return new PageResult(200, "[]", Json);
            }

            if (p.StartsWith(GmiPrefix, StringComparison.Ordinal))
            {
                return new PageResult(200, "{\"buy_orders\":[],\"sell_orders\":[]}", Json);
            }

            return null;
        }
    }
}
