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

    using ZoneEngine.Core.MessageHandlers;

    #endregion

    /// <summary>
    /// </summary>
    internal class Function_headmesh : FunctionPrototype
    {
        #region Constants

        /// <summary>
        /// </summary>
        private const FunctionType functionId = FunctionType.HeadMesh;

        /// <summary>
        /// Where a head item is drawn, in front of the head at layer 4.
        /// </summary>
        private const int HeadItemLayer = 2;

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
            // Items carry [override texture, mesh] - 6,271 of them, every one two
            // integers - and an equipment page appends the slot it is in.
            //
            // The mesh goes out at position 0, layer 2, beside the head rather
            // than over it: retail sends 300679 at layer 2 and the head 40109 at
            // layer 4 together, for item 300673 on an Atrox, and the same shape
            // for two other helmets on two other breeds. This used layer 4, which
            // replaced the head, and wrote the override texture into the
            // headmesh stat, which is the head observers are drawn with.
            //
            // The override texture has only ever been seen on the wire as zero,
            // with zero in the item. An item with a non-zero one was captured
            // only under a social hat that hid it.
            var character = (Character)Self;
            int overrideTexture = Arguments[0].AsInt32();
            int mesh = Arguments[1].AsInt32();
            bool social = (Arguments.Length > 2)
                          && (Arguments[Arguments.Length - 1].AsInt32() >= MeshLayers.FirstSocialSlot);

            (social ? character.SocialMeshLayer : character.MeshLayer).AddMesh(0, mesh, overrideTexture, HeadItemLayer);

            character.ChangedAppearance = true;

            return true;
        }

        #endregion
    }
}