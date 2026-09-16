namespace ZoneEngine.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using Utility;

    /// <summary>
    /// What a kind of creature looks like beyond its MonsterData and scale: the textures laid over its
    /// model, the second monster scale and the glow of a dynamic creature. By name, from the tables
    /// npctextures, npcappearance and npcactivenanos.
    /// </summary>
    /// <remarks>
    /// Retail against OmniCell, the same creatures in Arete Landing:
    /// <list type="bullet">
    /// <item>Garbage Flea: extended textures [Material #9 95883 alpha 1]; OmniCell sent none, and the
    /// client drew no flea.</item>
    /// <item>Waste Collector: [Material #22 17318 alpha 1]; OmniCell none, no model.</item>
    /// <item>Cleanmeister Intelligence Robot: [mdrone1 96068, mdrone2 96069], second scale 40, and three
    /// active nanos (162268, 160070, 160085, 10000/10000) - its glow; OmniCell sent none of it.</item>
    /// </list>
    /// A corpse's meshes are the same list as the creature's textures (Garbage Flea, Cleaning Robot,
    /// Cleanmeister, Supreme Collector of Waste, Rollerrat and 32-V Docker all match), so the corpse
    /// message reads them from here too.
    /// </remarks>
    public static class NpcAppearance
    {
        private static readonly object Sync = new object();

        private static Dictionary<string, CharacterTexture[]> textures;

        private static Dictionary<string, ActiveNano[]> nanos;

        private static Dictionary<string, byte> secondScales;

        /// <summary>
        /// Reads the three tables. Safe to call again to pick up edits.
        /// </summary>
        public static int Load()
        {
            var loadedTextures = new Dictionary<string, CharacterTexture[]>(StringComparer.Ordinal);
            var loadedNanos = new Dictionary<string, ActiveNano[]>(StringComparer.Ordinal);
            var loadedScales = new Dictionary<string, byte>(StringComparer.Ordinal);
            try
            {
                foreach (IGrouping<string, DBNpcTexture> npc in NpcTextureDao.Instance.GetAll().GroupBy(t => t.NpcName))
                {
                    loadedTextures[npc.Key] = npc.OrderBy(t => t.Ordinal)
                        .Select(
                            t => new CharacterTexture
                                 {
                                     Name = MaterialName(t.MaterialName),
                                     TextureId = t.TextureId,
                                     OverlayId = t.OverlayId,
                                     AlphaMode = t.AlphaMode
                                 })
                        .ToArray();
                }

                foreach (IGrouping<string, DBNpcActiveNano> npc in NpcActiveNanoDao.Instance.GetAll().GroupBy(n => n.NpcName))
                {
                    loadedNanos[npc.Key] = npc.OrderBy(n => n.Ordinal)
                        .Select(
                            n => new ActiveNano
                                 {
                                     NanoId = n.NanoType,
                                     NanoInstance = n.NanoId,
                                     Unknown = 0,
                                     Time1 = n.Time1,
                                     Time2 = n.Time2
                                 })
                        .ToArray();
                }

                foreach (DBNpcAppearance npc in NpcAppearanceDao.Instance.GetAll())
                {
                    if (npc.SecondMonsterScale >= 0 && npc.SecondMonsterScale <= byte.MaxValue)
                    {
                        loadedScales[npc.NpcName] = (byte)npc.SecondMonsterScale;
                    }
                }
            }
            catch (Exception exception)
            {
                // A database without the tables yet draws creatures as before rather than not at all.
                LogUtil.ErrorException(exception, "NPC appearance could not be read");
            }

            lock (Sync)
            {
                textures = loadedTextures;
                nanos = loadedNanos;
                secondScales = loadedScales;
            }

            return loadedTextures.Count;
        }

        /// <summary>
        /// Adds a spawned character's textures, second scale and standing nanos to its update.
        /// </summary>
        public static void Apply(SimpleCharFullUpdateMessage message, string npcName)
        {
            if (message == null || npcName == null)
            {
                return;
            }

            EnsureLoaded();

            CharacterTexture[] worn;
            if (textures.TryGetValue(npcName, out worn))
            {
                message.ExtendedTextures = worn;
            }

            byte scale;
            if (secondScales.TryGetValue(npcName, out scale))
            {
                message.UnknownData3 = scale;
            }

            ActiveNano[] glow;
            if (nanos.TryGetValue(npcName, out glow) && (message.ActiveNanos == null || message.ActiveNanos.Length == 0))
            {
                message.ActiveNanos = glow;
            }
        }

        /// <summary>
        /// The meshes the corpse of a kind of creature is drawn with; empty when it has none (every
        /// humanoid corpse in the recordings).
        /// </summary>
        public static CorpseMesh[] CorpseMeshesOf(string npcName)
        {
            EnsureLoaded();
            CharacterTexture[] worn;
            if (npcName == null || !textures.TryGetValue(npcName, out worn))
            {
                return new CorpseMesh[0];
            }

            return worn.Select(
                    t => new CorpseMesh
                         {
                             Name = Encoding.ASCII.GetString(t.Name).TrimEnd('\0'),
                             Id = t.TextureId,
                             OverlayId = t.OverlayId,
                             AlphaMode = t.AlphaMode
                         })
                .ToArray();
        }

        private static void EnsureLoaded()
        {
            if (textures == null)
            {
                lock (Sync)
                {
                    if (textures == null)
                    {
                        Load();
                    }
                }
            }
        }

        private static byte[] MaterialName(string name)
        {
            var bytes = new byte[CorpseMesh.NameLength];
            byte[] text = Encoding.ASCII.GetBytes(name ?? string.Empty);
            Array.Copy(text, bytes, Math.Min(text.Length, bytes.Length));
            return bytes;
        }
    }
}
