namespace ZoneEngine.Core.Quests
{
    using System;

    /// <summary>
    /// Pure count and client-slot rules used by quest item hand-ins.
    /// </summary>
    public static class QuestTradeRules
    {
        public static int SlotCount(int remainingItems)
        {
            return Math.Max(1, Math.Min(6, remainingItems));
        }

        public static int AmountToConsume(int remaining, int alreadyAccepted, int available)
        {
            if (remaining < 1 || alreadyAccepted < 0 || available < 1 || alreadyAccepted >= remaining)
            {
                return 0;
            }

            return Math.Min(remaining - alreadyAccepted, available);
        }
    }
}
