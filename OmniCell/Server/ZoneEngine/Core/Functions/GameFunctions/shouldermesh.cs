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
    /// A shoulder pad.
    /// </summary>
    /// <remarks>
    /// There was no handler for this, so no shoulder pad was ever drawn. 1,908
    /// items use it with a single integer argument, the mesh, and nothing in it
    /// says which shoulder: that is the slot. In the one retail capture of a
    /// character wearing two, the pad in armor slot 20 went out at position 3 and
    /// the pad in slot 22 at position 4, both at layer 0 with override zero.
    ///
    /// Social slots 52 and 54 are assumed to be the same two shoulders, by the
    /// same offset from the social page as 20 and 22 from the armor page. No
    /// capture has a social shoulder pad in it.
    /// </remarks>
    internal class Function_shouldermesh : FunctionPrototype
    {
        #region Constants

        /// <summary>
        /// </summary>
        private const FunctionType functionId = FunctionType.Shouldermesh;

        /// <summary>
        /// </summary>
        private const int PadLayer = 0;

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

                // Without the slot there is no knowing which shoulder.
                if (character == null || arguments.Length < 2)
                {
                    return false;
                }

                int mesh = arguments[0].AsInt32();
                int slot = arguments[arguments.Length - 1].AsInt32();

                switch (slot)
                {
                    case 20:
                        character.MeshLayer.AddMesh(MeshLayers.RightPadPosition, mesh, 0, PadLayer);
                        break;
                    case 22:
                        character.MeshLayer.AddMesh(MeshLayers.LeftPadPosition, mesh, 0, PadLayer);
                        break;
                    case 52:
                        character.SocialMeshLayer.AddMesh(MeshLayers.RightPadPosition, mesh, 0, PadLayer);
                        break;
                    case 54:
                        character.SocialMeshLayer.AddMesh(MeshLayers.LeftPadPosition, mesh, 0, PadLayer);
                        break;
                    default:
                        return false;
                }

                character.ChangedAppearance = true;
                return true;
            }
        }

        #endregion
    }
}
