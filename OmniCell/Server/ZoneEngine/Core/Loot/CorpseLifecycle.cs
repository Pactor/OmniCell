namespace ZoneEngine.Core.Loot
{
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    /// <summary>
    /// Owns removal of the transient inventory attached to a corpse.
    /// </summary>
    public static class CorpseLifecycle
    {
        /// <summary>
        /// Removes and disposes the exact corpse inventory if it still exists.
        /// Calling this more than once is safe.
        /// </summary>
        public static bool Expire(Identity playfield, Identity corpse)
        {
            if (corpse.Type != IdentityType.Corpse)
            {
                return false;
            }

            CorpseLoot loot = Pool.Instance.GetObject<CorpseLoot>(playfield, corpse);
            if (loot == null)
            {
                CorpseLootAccess.ForgetCorpse(playfield, corpse);
                return false;
            }

            CorpseLootAccess.ForgetCorpse(playfield, corpse);
            loot.Dispose();
            return true;
        }
    }
}
