#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Stats
{
    /// <summary>
    /// The number the stat list uses to mean "nobody has set this".
    /// </summary>
    /// <remarks>
    /// 1234567890, chosen long ago because no real stat would ever hold it, and
    /// perfectly fine while it stays inside the server. It stops being fine the
    /// moment it goes out to a client or into a calculation, and it has done
    /// both: an account flags field sixteen bits wide carried it to every client
    /// as 722, and a swing whose damage came from an unset stat hit for
    /// 1234567890.
    ///
    /// Anywhere a stat is read for something other than another stat, it goes
    /// through here first.
    /// </remarks>
    public static class StatValue
    {
        #region Constants

        /// <summary>
        /// The value a stat holds when nothing has set it.
        /// </summary>
        public const int Unset = 1234567890;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The stat, or the given value if nothing has set it.
        /// </summary>
        public static int Or(int value, int fallback)
        {
            return value == Unset ? fallback : value;
        }

        /// <summary>
        /// The stat, or zero if nothing has set it.
        /// </summary>
        public static int OrZero(int value)
        {
            return Or(value, 0);
        }

        #endregion
    }
}
