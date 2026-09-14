#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace OmniCell.Stats.SpecialStats
{
    #region Usings ...

    using System;

    using OmniCell.Enums;

    #endregion

    /// <summary>
    /// </summary>
    public class StatLife : Stat
    {
        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="statList">
        /// </param>
        /// <param name="number">
        /// </param>
        /// <param name="defaultValue">
        /// </param>
        /// <param name="sendBaseValue">
        /// </param>
        /// <param name="dontWrite">
        /// </param>
        /// <param name="announceToPlayfield">
        /// </param>
        public StatLife(
            Stats statList,
            int number,
            uint defaultValue,
            bool sendBaseValue,
            bool dontWrite,
            bool announceToPlayfield)
            : base(statList, number, defaultValue, sendBaseValue, dontWrite, announceToPlayfield)
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public override uint GetBaseValue
        {
            get
            {
                // Hitpoints by Profession and TitleLevel
                int[,] tableProfessionHitPoints =
                {
                    { 6, 6, 6, 6, 6, 6, 6, 6, 7, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5 },
                    { 7, 7, 6, 7, 7, 7, 6, 7, 8, 6, 6, 6, 7, 7, 5, 5, 5, 5, 5 },
                    { 8, 7, 6, 7, 7, 8, 7, 7, 9, 6, 6, 6, 8, 7, 5, 5, 5, 5, 5 },
                    { 9, 8, 6, 8, 8, 8, 7, 7, 10, 6, 6, 6, 9, 8, 5, 5, 5, 5, 5 },
                    { 10, 9, 6, 9, 8, 9, 8, 8, 11, 6, 6, 6, 10, 9, 5, 5, 5, 5, 5 },
                    { 11, 12, 6, 10, 9, 9, 9, 9, 12, 6, 6, 6, 11, 10, 5, 5, 5, 5, 5 },
                    {
                        12, 13, 7, 11, 10, 10, 10, 10, 13, 7, 7, 7, 12, 11, 5, 5, 5, 5, 5
                    },
                };

                int[] breedMultiplicatorHitPoints = { 3, 3, 2, 4, 8, 8, 10, 8, 8, 8, 8, 8, 8 };
                int[] breedModificatorHitPoints = { 0, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                uint breed = this.Stats[StatIds.breed].BaseValue;
                uint profession = this.Stats[StatIds.profession].BaseValue;

                uint titleLevel = (uint)this.Stats[StatIds.titlelevel].Value;
                uint level = (uint)this.Stats[StatIds.level].Value;
                int[] breedBaseHitPoints = { 10, 15, 10, 25, 30, 30, 30 };

                // This formula is for players. Everything in it is indexed one
                // based - breedBaseHitPoints[breed - 1],
                // tableProfessionHitPoints[titleLevel - 1, profession - 1] - so
                // a character with any of the three unset indexes at minus one
                // and takes the whole zone down with it.
                //
                // Spawned characters are exactly that case. A Cleaning Robot has
                // a breed and a level because the capture said so, and no
                // profession, because a cleaning robot does not have one. The
                // first heartbeat after one spawned crashed the ZoneEngine on
                // health regeneration.
                //
                // Where the formula does not apply, the stored value stands.
                // That is what was set from the spawn's own stats, which for a
                // mob is the health the live server gave it, and is a better
                // answer than a player hit point table would have been anyway.
                if (breed < 1 || breed > breedBaseHitPoints.Length
                    || profession < 1 || profession > tableProfessionHitPoints.GetLength(1)
                    || titleLevel < 1 || titleLevel > tableProfessionHitPoints.GetLength(0))
                {
                    return base.GetBaseValue;
                }

                int baseValue = breedBaseHitPoints[breed - 1];
                int beforeModifiers =
                    (int)
                        (baseValue
                         + (level
                            * (tableProfessionHitPoints[titleLevel - 1, profession - 1]
                               + breedModificatorHitPoints[breed - 1]))
                         + (this.Stats[StatIds.bodydevelopment].Value * breedMultiplicatorHitPoints[breed - 1]));
                return (uint)beforeModifiers;
            }
        }

        /// <summary>
        /// </summary>
        public override int GetValue
        {
            get
            {
                // See Stat.GetValue: the long result is truncated to 32 bits rather than going through a
                // double, whose out-of-range cast to int differs between .NET Framework and .NET 9+.
                return unchecked((int)((this.BaseValue + this.Modifier + this.Trickle) * this.PercentageModifier / 100));
            }
        }

        #endregion
    }
}