namespace WebEngine
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Web.Script.Serialization;

    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    /// <summary>
    /// Character and organization lookups for chat bots, from this server's
    /// database.
    /// </summary>
    /// <remarks>
    /// Chat bots such as Tyrbot fill in org rosters and character details from
    /// Funcom's people.anarchy-online.com. Pointed there, they read live Funcom
    /// data: the first org on this server has id 1, and Tyrbot filled its roster
    /// with the members of Funcom's org 1. These answer the same two paths with
    /// the same JSON, built from this server's characters, stats and
    /// organizations, so a bot only needs its lookup URLs pointed here.
    ///
    /// The field names and value types are those of a people.anarchy-online.com
    /// org roster as Tyrbot cached it (data/cache/org_roster/1.5.json), and of
    /// what Tyrbot reads from a character bio (core/lookup/pork_service.py).
    /// Values this server does not keep - profession and defender rank titles,
    /// PvP title - are empty strings, as Tyrbot stores for an unknown character.
    /// A missing character or org is a 404 that is not JSON, which Tyrbot reads
    /// as nothing found.
    /// </remarks>
    public static class People
    {
        private const int CharacterType = 50000;

        private const string Json = "application/json; charset=utf-8";

        private const string Text = "text/plain; charset=utf-8";

        private static readonly string[] Genders = { string.Empty, "Neuter", "Male", "Female" };

        private static readonly string[] Breeds = { string.Empty, "Solitus", "Opifex", "Nanomage", "Atrox" };

        private static readonly string[] Professions =
        {
            string.Empty, "Soldier", "Martial Artist", "Engineer", "Fixer", "Agent", "Adventurer", "Trader",
            "Bureaucrat", "Enforcer", "Doctor", "Nano-Technician", "Meta-Physicist", "Monster", "Keeper", "Shade"
        };

        private static readonly string[] Sides = { "Neutral", "Clan", "Omni" };

        private static readonly string[] GoverningForms =
        {
            "Department", "Faction", "Republic", "Monarchy", "Anarchism", "Feudalism"
        };

        /// <summary>
        /// Rank titles by governing form, as ZoneEngine's OrgClient.GetRank.
        /// </summary>
        private static readonly string[][] Ranks =
        {
            new[] { "President", "General", "Squad Commander", "Unit Commander", "Unit Leader", "Unit Member", "Applicant" },
            new[] { "Director", "Board Member", "Executive", "Member", "Applicant" },
            new[] { "President", "Advisor", "Veteran", "Member", "Applicant" },
            new[] { "Monarch", "Council", "Follower" },
            new[] { "Anarchist" },
            new[] { "Lord", "Knight", "Vassal", "Peasant" }
        };

        /// <summary>
        /// Answers org/stats/d/{dimension}/name/{org id}/basicstats.xml and
        /// character/bio/d/{dimension}/name/{name}/bio.xml. Null for any other path.
        /// </summary>
        /// <param name="path">Request path without its query string.</param>
        public static PageResult Route(string path)
        {
            string[] parts = path.Trim('/').Split('/');
            if (parts.Length != 7 || !Is(parts[2], "d") || !Is(parts[4], "name"))
            {
                return null;
            }

            bool roster = Is(parts[0], "org") && Is(parts[1], "stats") && Is(parts[6], "basicstats.xml");
            bool bio = Is(parts[0], "character") && Is(parts[1], "bio") && Is(parts[6], "bio.xml");
            if (!roster && !bio)
            {
                return null;
            }

            int dimension;
            if (!int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out dimension))
            {
                return NotFound();
            }

            try
            {
                object json;
                if (roster)
                {
                    int orgId;
                    json = int.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out orgId)
                               ? OrgRoster(orgId, dimension)
                               : null;
                }
                else
                {
                    json = CharacterBio(Uri.UnescapeDataString(parts[5]), dimension);
                }

                return json == null
                           ? NotFound()
                           : new PageResult(200, new JavaScriptSerializer().Serialize(json), Json);
            }
            catch (Exception e)
            {
                // Not JSON, so a bot reads it as nothing found rather than half an answer.
                return new PageResult(500, "Lookup failed: " + e.Message, Text);
            }
        }

        private static object OrgRoster(int orgId, int dimension)
        {
            DBOrganization organization = OrganizationDao.Instance.Get(orgId);
            if (organization == null)
            {
                return null;
            }

            var members = new List<Dictionary<string, object>>();
            IEnumerable<int> memberIds =
                StatDao.Instance.GetAll(new { Type = CharacterType, StatId = (int)StatIds.clan, StatValue = orgId })
                    .Select(stat => stat.Instance)
                    .Distinct();
            foreach (int id in memberIds)
            {
                DBCharacter character = CharacterDao.Instance.Get(id);
                if (character == null)
                {
                    continue;
                }

                Dictionary<int, int> stats = StatsOf(id);
                int rank = Stat(stats, StatIds.clanlevel);
                members.Add(
                    new Dictionary<string, object>
                    {
                        { "NAME", character.Name },
                        { "FIRSTNAME", character.FirstName ?? string.Empty },
                        { "LASTNAME", character.LastName ?? string.Empty },
                        { "CHAR_INSTANCE", character.Id },
                        { "CHAR_DIMENSION", dimension },
                        { "LEVELX", Stat(stats, StatIds.level) },
                        { "BREED", Name(Breeds, Stat(stats, StatIds.breed)) },
                        { "SEX", Name(Genders, Stat(stats, StatIds.sex)) },
                        { "PROF", Name(Professions, Stat(stats, StatIds.profession)) },
                        { "PROF_TITLE", string.Empty },
                        { "DEFENDER_RANK_TITLE", string.Empty },
                        { "ALIENLEVEL", Stat(stats, StatIds.alienlevel) },
                        { "PVPRATING", Stat(stats, StatIds.pvp_rating) },
                        { "PVPTITLE", string.Empty },
                        { "HEADID", Stat(stats, StatIds.headmesh) },
                        { "RANK", rank },
                        { "RANK_TITLE", RankTitle(organization.GovernmentForm, rank) }
                    });
            }

            if (members.Count == 0)
            {
                return null;
            }

            int[] levels = members.Select(member => (int)member["LEVELX"]).ToArray();
            int side = Stat(StatsOf(organization.LeaderId), StatIds.side);
            Func<string, string, int> count = (field, value) => members.Count(member => (string)member[field] == value);

            var info = new Dictionary<string, object>
                       {
                           { "ORG_INSTANCE", organization.Id },
                           { "NAME", organization.Name },
                           { "ORG_DIMENSION", dimension },
                           { "GOVERNINGNAME", Name(GoverningForms, organization.GovernmentForm) },
                           { "SIDE", side },
                           { "SIDE_NAME", Name(Sides, side) },
                           { "DESCRIPTION", organization.Description ?? string.Empty },
                           { "OBJECTIVE", organization.Objective ?? string.Empty },
                           { "HISTORY", organization.History ?? string.Empty },
                           { "NUMMEMBERS", members.Count },
                           { "MINLVL", levels.Min() },
                           { "MAXLVL", levels.Max() },
                           { "AVGLVL", levels.Average() },
                           { "MALECOUNT", count("SEX", "Male") },
                           { "FEMALECOUNT", count("SEX", "Female") },
                           { "NEUTERCOUNT", count("SEX", "Neuter") },
                           { "SOLITUSCOUNT", count("BREED", "Solitus") },
                           { "OPIFEXCOUNT", count("BREED", "Opifex") },
                           { "NANORACECOUNT", count("BREED", "Nanomage") },
                           { "ATROXCOUNT", count("BREED", "Atrox") },
                           { "SOLIDERCOUNT", count("PROF", "Soldier") },
                           { "MACOUNT", count("PROF", "Martial Artist") },
                           { "ENGINEEERCOUNT", count("PROF", "Engineer") },
                           { "FIXERCOUNT", count("PROF", "Fixer") },
                           { "AGENTCOUNT", count("PROF", "Agent") },
                           { "ADVENTURERCOUNT", count("PROF", "Adventurer") },
                           { "TRADERCOUNT", count("PROF", "Trader") },
                           { "BTCOUNT", count("PROF", "Bureaucrat") },
                           { "ENFCOUNT", count("PROF", "Enforcer") },
                           { "DOCTORCOUNT", count("PROF", "Doctor") },
                           { "NANOCOUNT", count("PROF", "Nano-Technician") },
                           { "METACOUNT", count("PROF", "Meta-Physicist") },
                           { "MONSTERCOUNT", count("PROF", "Monster") },
                           { "KEEPERCOUNT", count("PROF", "Keeper") },
                           { "SHADECOUNT", count("PROF", "Shade") }
                       };

            return new object[] { info, members, Now() };
        }

        private static object CharacterBio(string name, int dimension)
        {
            DBCharacter character = CharacterDao.Instance.GetByCharName(name);
            if (character == null)
            {
                return null;
            }

            Dictionary<int, int> stats = StatsOf(character.Id);
            var info = new Dictionary<string, object>
                       {
                           { "NAME", character.Name },
                           { "CHAR_INSTANCE", character.Id },
                           { "FIRSTNAME", character.FirstName ?? string.Empty },
                           { "LASTNAME", character.LastName ?? string.Empty },
                           { "LEVELX", Stat(stats, StatIds.level) },
                           { "BREED", Name(Breeds, Stat(stats, StatIds.breed)) },
                           { "CHAR_DIMENSION", dimension },
                           { "SEX", Name(Genders, Stat(stats, StatIds.sex)) },
                           { "SIDE", Name(Sides, Stat(stats, StatIds.side)) },
                           { "PROF", Name(Professions, Stat(stats, StatIds.profession)) },
                           { "PROFNAME", string.Empty },
                           { "RANK_name", string.Empty },
                           { "ALIENLEVEL", Stat(stats, StatIds.alienlevel) },
                           { "PVPRATING", Stat(stats, StatIds.pvp_rating) },
                           { "PVPTITLE", string.Empty },
                           { "HEADID", Stat(stats, StatIds.headmesh) }
                       };

            int orgId = Stat(stats, StatIds.clan);
            DBOrganization organization = orgId == 0 ? null : OrganizationDao.Instance.Get(orgId);
            object org = null;
            if (organization != null)
            {
                int rank = Stat(stats, StatIds.clanlevel);
                org = new Dictionary<string, object>
                      {
                          { "ORG_INSTANCE", organization.Id },
                          { "NAME", organization.Name },
                          { "RANK", rank },
                          { "RANK_TITLE", RankTitle(organization.GovernmentForm, rank) }
                      };
            }

            return new object[] { info, org, Now() };
        }

        private static Dictionary<int, int> StatsOf(int characterId)
        {
            var stats = new Dictionary<int, int>();
            foreach (DBStats stat in StatDao.Instance.GetAll(new { Type = CharacterType, Instance = characterId }))
            {
                stats[stat.StatId] = stat.StatValue;
            }

            return stats;
        }

        private static int Stat(Dictionary<int, int> stats, StatIds id)
        {
            int value;
            return stats.TryGetValue((int)id, out value) ? value : 0;
        }

        private static string Name(string[] names, int index)
        {
            return index >= 0 && index < names.Length ? names[index] : string.Empty;
        }

        private static string RankTitle(int governingForm, int rank)
        {
            return governingForm >= 0 && governingForm < Ranks.Length ? Name(Ranks[governingForm], rank) : string.Empty;
        }

        private static string Now()
        {
            return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static bool Is(string part, string expected)
        {
            return part.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static PageResult NotFound()
        {
            return new PageResult(404, "Not found", Text);
        }
    }
}
