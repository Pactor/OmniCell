#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Core.Nanos
{
    #region Usings

    using OmniCell.Interfaces;

    #endregion

    /// <summary>
    /// A nano currently running on a character.
    /// </summary>
    /// <remarks>
    /// IActiveNano existed with nothing implementing it, so ICharacter.ActiveNanos
    /// could never hold anything and SimpleCharFullUpdate always reported a
    /// character as running no nanos - no buff icons, on anyone, ever.
    ///
    /// The fields are the ones the interface names. TickCounter and TickInterval
    /// go straight into the ActiveNano entry of SimpleCharFullUpdate as Time1 and
    /// Time2; captures of the live server show a running buff carrying its
    /// remaining duration there.
    /// </remarks>
    public class ActiveNano : IActiveNano
    {
        #region Public Properties

        /// <summary>
        /// Nano ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Instance
        /// </summary>
        public int Instance { get; set; }

        /// <summary>
        /// Nano type
        /// </summary>
        public int Nanotype { get; set; }

        /// <summary>
        /// Time 1
        /// </summary>
        public int TickCounter { get; set; }

        /// <summary>
        /// Time 2
        /// </summary>
        public int TickInterval { get; set; }

        /// <summary>
        /// unknown
        /// </summary>
        public int Value3 { get; set; }

        #endregion
    }
}
