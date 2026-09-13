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

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using System;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Textures;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using MsgPack;

    #endregion

    /// <summary>
    /// </summary>
    internal class backmesh : FunctionPrototype
    {
        #region Constants

        /// <summary>
        /// </summary>
        private const FunctionType functionId = FunctionType.BackMesh;

        /// <summary>
        /// </summary>
        private const int BackPosition = 5;

        /// <summary>
        /// </summary>
        private const int BackLayer = 0;

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
                return this.FunctionExecute(self, caller, target, arguments);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="Self">
        /// </param>
        /// <param name="Caller">
        /// </param>
        /// <param name="Target">
        /// </param>
        /// <param name="Arguments">
        /// </param>
        /// <returns>
        /// </returns>
        public bool FunctionExecute(
            INamedEntity Self,
            IEntity Caller,
            IInstancedEntity Target,
            MessagePackObject[] Arguments)
        {
            // Items carry [mesh] - 3,391 of them, every one a single integer - and
            // an equipment page appends the slot. This read the pair the other
            // way round, so a back item went out as mesh 19 (its slot) with the
            // real mesh as its override texture.
            //
            // Position 5, layer 0: item 300690 is 300678 there in retail, and
            // 300671 on a different breed. The override texture is zero for that
            // item. Two other back items carry a non-zero one in retail (268223,
            // 302715) that is not in their converted data under any stat, so
            // where it comes from is not known and zero is sent.
            var character = (Character)Self;
            int mesh = Arguments[0].AsInt32();
            bool social = (Arguments.Length > 1)
                          && (Arguments[Arguments.Length - 1].AsInt32() >= MeshLayers.FirstSocialSlot);

            // No stat is written. This used to set stat 38, backmesh, which
            // nothing cleared when the item came off, so the zone saved it and
            // the character kept a back mesh it was no longer wearing. No retail
            // FullCharacter carries stat 38 in any capture; the mesh travels in
            // AppearanceUpdate and nowhere else.
            (social ? character.SocialMeshLayer : character.MeshLayer).AddMesh(BackPosition, mesh, 0, BackLayer);

            character.ChangedAppearance = true;
            return true;
        }

        #endregion
    }
}