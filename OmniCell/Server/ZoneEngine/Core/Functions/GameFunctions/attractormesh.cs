#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using OmniCell.Core.Entities;
    using OmniCell.Core.Textures;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using MsgPack;

    #endregion

    /// <summary>
    /// A mesh attached at the head, such as the glasses every new character is
    /// given.
    /// </summary>
    /// <remarks>
    /// There was no handler for this, so it did nothing. Razor's Polarized Specs
    /// (292235) - starting equipment - draw with this and nothing else, and
    /// seven captured characters of all four breeds wear them: each goes out with
    /// the mesh their breed and sex select at position 0, layer 0, override zero,
    /// in front of the head at layer 4. 853 items use it, every one with a single
    /// integer argument; an equipment page appends the slot.
    /// </remarks>
    internal class Function_attractormesh : FunctionPrototype
    {
        #region Constants

        /// <summary>
        /// </summary>
        private const FunctionType functionId = FunctionType.AttractorMesh;

        /// <summary>
        /// </summary>
        private const int AttractorLayer = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public override FunctionType FunctionId
        {
            get
            {
                return functionId;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="self">
        /// </param>
        /// <param name="caller">
        /// </param>
        /// <param name="target">
        /// </param>
        /// <param name="arguments">
        /// </param>
        /// <returns>
        /// </returns>
        public override bool Execute(
            INamedEntity self,
            IEntity caller,
            IInstancedEntity target,
            MessagePackObject[] arguments)
        {
            lock (target)
            {
                var character = self as Character;
                if (character == null || arguments.Length == 0)
                {
                    return false;
                }

                int mesh = arguments[0].AsInt32();
                bool social = (arguments.Length > 1)
                              && (arguments[arguments.Length - 1].AsInt32() >= MeshLayers.FirstSocialSlot);

                (social ? character.SocialMeshLayer : character.MeshLayer).AddMesh(0, mesh, 0, AttractorLayer);
                character.ChangedAppearance = true;
                return true;
            }
        }

        #endregion
    }
}
