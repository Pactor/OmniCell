#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Quests
{
    /// <summary>
    /// The FormatFeedback texts quests send, built the way the live server writes them.
    /// </summary>
    /// <remarks>
    /// "~&amp;", the category and the message id as base-85 numbers, then the arguments: 'i' and a
    /// base-85 int, or 's', one character holding the text's length plus one, and the text. Base-85
    /// digits are characters counted up from '!', five to a number. Both formats are copied from retail
    /// quest sniffs (20260914-120906 #2999, #3140 and #5799). Without the length character the client
    /// misreads the counter and loses what follows it - the corpse of the robot just killed.
    /// </remarks>
    public static class QuestFeedback
    {
        private const int Category = 110;

        /// <summary>
        /// "You have to kill %d more %s": sent for each kill that counts but does not finish a stage.
        /// </summary>
        public static string KillCounter(int remaining, string name)
        {
            name = name ?? string.Empty;
            return "~&" + Base85(Category) + "$nZiA" + "i" + Base85(remaining) + "s" + (char)(name.Length + 1) + name;
        }

        /// <summary>
        /// The experience and credits a finished stage pays.
        /// </summary>
        public static string Reward(int experience, int credits)
        {
            return "~&" + Base85(Category) + "$'O\"u" + "i" + Base85(experience) + "i" + Base85(credits) + "~";
        }

        public static string Base85(int value)
        {
            var digits = new char[5];
            long remaining = unchecked((uint)value);
            for (int i = 4; i >= 0; i--)
            {
                digits[i] = (char)('!' + (int)(remaining % 85));
                remaining /= 85;
            }

            return new string(digits);
        }
    }
}
