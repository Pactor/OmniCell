namespace ZoneEngine.Core.Playfields
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    /// <summary>
    /// The client instance ids of an instanced playfield's statels, and the way back from one of those
    /// ids to the statel in the playfield file.
    /// </summary>
    /// <remarks>
    /// The playfield file lists its statels with ids of its own (Arete Landing's Surgery Clinic is
    /// 0xC00E1999), but in an instanced playfield the client calls them by the ids the server's
    /// PlayfieldAnarchyF hands out, in runs over the file's order: the clinic is Terminal:1477021820.
    /// This server used to send an empty run table and then look statels up by the file's ids, so
    /// nothing the client used in Arete Landing - exits, the shuttle door, the clinic - was ever found.
    /// The runs now come from the playfieldstatelruns table, the same numbers both ways.
    /// </remarks>
    public static class StatelRuns
    {
        /// <summary>What every run retail sends carries in its second field.</summary>
        private const int RunUnknown = 1;

        private static readonly ConcurrentDictionary<int, List<DBPlayfieldStatelRun>> Cache =
            new ConcurrentDictionary<int, List<DBPlayfieldStatelRun>>();

        /// <summary>The runs of a playfield, in the order they are sent; none for most playfields.</summary>
        public static IList<DBPlayfieldStatelRun> For(int playfield)
        {
            return Cache.GetOrAdd(playfield, Load);
        }

        /// <summary>The runs as PlayfieldAnarchyF carries them.</summary>
        public static PlayfieldDynelRun[] ToWire(int playfield)
        {
            return For(playfield)
                .Select(
                    run => new PlayfieldDynelRun
                               {
                                   Type = (IdentityType)run.Type,
                                   Unknown = RunUnknown,
                                   StartIndex = run.StartIndex,
                                   Count = run.Count,
                                   FirstInstance = run.FirstInstance
                               })
                .ToArray();
        }

        /// <summary>The statel a client identity names in this playfield, or null.</summary>
        public static StatelData Resolve(int playfield, Identity identity)
        {
            PlayfieldData data;
            return PlayfieldLoader.PFData.TryGetValue(playfield, out data)
                       ? Resolve(data.Statels, For(playfield), identity)
                       : null;
        }

        /// <summary>
        /// The statel an identity names, through the runs first and then by the file's own identity,
        /// which is what a playfield without runs (Rubi-Ka) is called by.
        /// </summary>
        public static StatelData Resolve(IList<StatelData> statels, IEnumerable<DBPlayfieldStatelRun> runs, Identity identity)
        {
            if (statels == null)
            {
                return null;
            }

            foreach (DBPlayfieldStatelRun run in runs ?? Enumerable.Empty<DBPlayfieldStatelRun>())
            {
                if (run.Type != (int)identity.Type)
                {
                    continue;
                }

                long offset = (long)identity.Instance - run.FirstInstance;
                if ((offset < 0) || (offset >= run.Count))
                {
                    continue;
                }

                int position = run.StartIndex + (int)offset;
                if ((position < 0) || (position >= statels.Count))
                {
                    return null;
                }

                StatelData statel = statels[position];
                return statel.Identity.Type == identity.Type ? statel : null;
            }

            return statels.FirstOrDefault(
                s => (s.Identity.Type == identity.Type) && (s.Identity.Instance == identity.Instance));
        }

        private static List<DBPlayfieldStatelRun> Load(int playfield)
        {
            try
            {
                return PlayfieldStatelRunDao.Instance.GetWhere(new { Playfield = playfield })
                    .OrderBy(run => run.Ordinal)
                    .ToList();
            }
            catch (Exception exception)
            {
                // No table yet (a database made before it existed): no runs, as before.
                LogUtil.ErrorException(exception);
                return new List<DBPlayfieldStatelRun>();
            }
        }
    }
}
