namespace ZoneEngine.Core.Quests
{
    using System;

    /// <summary>
    /// Pure matching rules for items supplied to quest hand-in windows.
    /// </summary>
    public static class QuestItemRules
    {
        public static bool Matches(
            int expectedLowId,
            int expectedHighId,
            int expectedQuality,
            string expectedName,
            int actualLowId,
            int actualHighId,
            int actualQuality,
            string actualName)
        {
            if (expectedLowId > 0 || expectedHighId > 0 || expectedQuality > 0)
            {
                if (expectedLowId < 1 || expectedHighId < 1 || expectedQuality < 1
                    || actualQuality != expectedQuality || actualLowId != expectedLowId)
                {
                    return false;
                }

                // Older OmniCell item construction collapses a range to its
                // QL-specific low endpoint. Accept that representation as well
                // as the full captured low/high pair, but no other template.
                return actualHighId == expectedHighId || actualHighId == expectedLowId;
            }

            return !string.IsNullOrEmpty(expectedName)
                   && string.Equals(expectedName, actualName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
