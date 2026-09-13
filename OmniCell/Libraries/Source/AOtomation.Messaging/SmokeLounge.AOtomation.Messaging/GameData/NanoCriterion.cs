// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoCriterion.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoCriterion type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// One requirement on a nano effect.
    /// </summary>
    /// <remarks>
    /// The same three-part shape the item and nano files use for requirements:
    /// a stat, a value to compare it against, and an operator. A captured cast
    /// of Composite Attribute Boost carries stat 128, value 1639, operator 2.
    ///
    /// Entries with a stat of zero are the and/or joins between the others,
    /// which is how Anarchy Online writes a requirement tree as a flat list -
    /// Or and And are two of the hundred operators the client names, and they
    /// sit in the list beside the comparisons rather than around them.
    /// </remarks>
    public class NanoCriterion
    {
        #region AoMember Properties

        public int Stat { get; set; }

        public int Value { get; set; }

        /// <summary>
        /// What the criterion tests. The client names all hundred of them.
        /// </summary>
        /// <remarks>
        /// Category 2008 of the client's text database is the list, and most of
        /// it is not comparisons: the operator can ask whether the target is
        /// alive, whether it is a pet, whether a nano is already running, and
        /// for those the stat and value beside it are the question's arguments
        /// rather than a left and right side. Six of the hundred occur in the
        /// captures: Larger on 368 criteria, Less on 265, Equal on 132 and
        /// Unequal on 9.
        /// </remarks>
        public NanoCriterionOperator Operator { get; set; }

        #endregion
    }
}
