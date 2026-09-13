// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Appearance.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the Appearance type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class Appearance
    {
        #region Fields

        private Breed breed;

        private Fatness fatness;

        private Gender gender;

        private uint race;

        private uint currentState;

        private Side side;

        private uint value;

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public uint Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.value = value;
                this.UpdateStats();
            }
        }

        #endregion

        #region Public Properties

        public Breed Breed
        {
            get
            {
                return this.breed;
            }

            set
            {
                this.breed = value;
                this.UpdateValue();
            }
        }

        public Fatness Fatness
        {
            get
            {
                return this.fatness;
            }

            set
            {
                this.fatness = value;
                this.UpdateValue();
            }
        }

        public Gender Gender
        {
            get
            {
                return this.gender;
            }

            set
            {
                this.gender = value;
                this.UpdateValue();
            }
        }

        public uint Race
        {
            get
            {
                return this.race;
            }

            set
            {
                this.race = value;
                this.UpdateValue();
            }
        }

        public Side Side
        {
            get
            {
                return this.side;
            }

            set
            {
                this.side = value;
                this.UpdateValue();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Bits 12 to 16 of the packed word - stat 423, currentstate.
        /// </summary>
        /// <remarks>
        /// Five bits, so 0 to 31. The reader splits them out at 0x10079251 and
        /// stores them at the message's + 0x90 as a full dword, which is what
        /// first showed they are not part of <see cref="Race"/>.
        ///
        /// Named on 2026-09-11. What reads + 0x90 back is not the dispatcher
        /// but the ribosome - SimpleCharFullUpdate implements
        /// n3DynelRibosome_i as a second base at its own + 0x18, which is why
        /// the dispatcher looks like it never touches the field. The ribosome's
        /// fill method at 0x1007803B takes this + 0x78 - the same + 0x90 - and
        /// pushes it into the new character's stat table with 0x1A7 at
        /// 0x100780F7. That is 423, currentstate, in both OmniCell.Enums
        /// StatIds and CharacterStat.
        ///
        /// Zero in 9,298 of the 9,357 captured copies. The 59 that are not are
        /// 7, 10 and 11, and all but four of those carry flag 0x1 - so it is a
        /// field a spawn sets and a walking player does not.
        /// </remarks>
        public uint CurrentState
        {
            get
            {
                return this.currentState;
            }

            set
            {
                this.currentState = value;
                this.UpdateValue();
            }
        }

        private void UpdateStats()
        {
            var sideValue = this.value & 7;
            this.side = (Side)sideValue;
            var fatnessValue = (this.value & 31) >> 3;
            this.fatness = (Fatness)fatnessValue;
            var breedValue = (this.value & 255) >> 5;
            this.breed = (Breed)breedValue;
            var genderValue = (this.value & 1023) >> 8;
            this.gender = (Gender)genderValue;
            // Race is two bits, not everything above bit 9. The client
            // splits this word at 0x1007921B onward and takes (value >> 10)
            // & 3 into the byte at + 0x89, which the dispatcher then pushes
            // with 0x59 - stat 89, race - at 0x10078549. Bits 12 to 16 are a
            // field of their own, into + 0x90.
            //
            // This used to be value >> 10, which swept the higher field into
            // race and made both wrong: reading gave a race far larger than
            // the client's, and writing a race put it at bit 10 with nothing
            // masking it, so it overwrote everything above. The packed word
            // still round trips either way - it is one uint32 on the wire -
            // which is why no capture ever objected.
            this.race = (this.value >> 10) & 3;
            this.currentState = (this.value >> 12) & 0x1F;
        }

        private void UpdateValue()
        {
            var sideValue = (uint)this.side;
            var fatnessValue = (uint)this.fatness << 3;
            var breedValue = (uint)this.breed << 5;
            var genderValue = (uint)this.gender << 8;
            var raceValue = (this.race & 3) << 10;
            var stateValue = (this.currentState & 0x1F) << 12;
            this.value = sideValue + fatnessValue + breedValue + genderValue + raceValue
                         + stateValue;
        }

        #endregion
    }
}