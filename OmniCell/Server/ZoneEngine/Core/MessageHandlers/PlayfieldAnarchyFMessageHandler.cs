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

namespace ZoneEngine.Core.MessageHandlers
{
    #region Usings ...

    using System.Collections.Generic;

    using OmniCell.Core.Components;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Vector;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.Playfields;

    using Vector3 = SmokeLounge.AOtomation.Messaging.GameData.Vector3;

    #endregion

    /// <summary>
    /// </summary>
    [MessageHandler(MessageHandlerDirection.OutboundOnly)]
    public class PlayfieldAnarchyFMessageHandler :
        BaseMessageHandler<PlayfieldAnarchyFMessage, PlayfieldAnarchyFMessageHandler>
    {
        #region Outbound

        /// <summary>
        /// The identity type the template half of an instanced playfield carries.
        /// </summary>
        /// <remarks>
        /// Read off the captures, where ModelId for Arete Landing is this
        /// type and 6553. It is two more than Playfield1, which is what an
        /// ordinary playfield uses, and nothing else in the protocol has been
        /// seen carrying it.
        /// </remarks>
        private const IdentityType PlayfieldTemplate = (IdentityType)0x0000C79E;

        /// <summary>
        /// The identity type of a TemplatePlayfieldGeneratorData_t.
        /// </summary>
        /// <remarks>
        /// 51069. This is the DbObject an instanced playfield ends with: the
        /// message hands the identity to DbObject_t::CreateObject and the object
        /// reads its own body. All three Arete Landing captures carry one and
        /// none of the Rubi-Ka ones do - they send an empty identity instead and
        /// the client stops there.
        /// </remarks>
        private const IdentityType TemplateGenerator = (IdentityType)0x0000C77D;

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        public void Send(ICharacter character)
        {
            this.Send(character, Filler(character));
        }

        /// <summary>
        /// </summary>
        /// <param name="character">
        /// </param>
        /// <returns>
        /// </returns>
        private static MessageDataFiller Filler(ICharacter character)
        {
            return x =>
            {
                int playfield = character.Playfield.Identity.Instance;
                int instance = Playfields.GetClientInstance(playfield);
                bool instanced = Playfields.IsInstanced(playfield);

                x.Identity = new Identity { Type = IdentityType.Playfield2, Instance = instance };
                Coordinate temp = character.Coordinates();
                x.CharacterCoordinates = new Vector3 { X = temp.x, Y = temp.y, Z = temp.z, };

                // An instanced playfield is named to the client as a pair: the
                // template it was made from, and the copy this character is
                // standing in. Rubi-Ka is 655 in both halves and the three
                // fields below are zero; Arete Landing is 6553 in the first, its
                // instance in the second, and the three carry the values the
                // captures show. Both cases are in the captures and they differ
                // in exactly this.
                //
                // Whatever is decided here has to match what SimpleCharFullUpdate
                // puts on every character in the playfield, or the client is
                // handed a world it was never told it was standing in - which is
                // what a half-applied version of this change did: the characters
                // moved to the instance and the playfield stayed behind, and the
                // client sat on the loading screen.
                x.ModelId = new Identity
                                 {
                                     Type = instanced ? PlayfieldTemplate : IdentityType.Playfield1,
                                     Instance = playfield
                                 };
                x.PlayfieldId = new Identity { Type = IdentityType.Playfield2, Instance = instance };
                x.Group = instanced ? 1 : 0;

                if (instanced)
                {
                    // The runs name the playfield file's statels to the client: which
                    // instance each terminal and door goes by. Sent empty, the client had
                    // no ids for them that the server could look up, so nothing it used -
                    // an exit, the shuttle door, the Surgery Clinic - was found. See
                    // StatelRuns and playfieldstatelruns.
                    x.TemplateGenerator = new PlayfieldTemplateGeneratorData
                                          {
                                              Identity =
                                                  new Identity { Type = TemplateGenerator, Instance = 1 },
                                              Revision = 1,
                                              Version = 1,
                                              Runs = StatelRuns.ToWire(playfield)
                                          };
                }

                x.PlayfieldX = Playfields.GetPlayfieldX(character.Playfield.Identity.Instance);
                x.PlayfieldZ = Playfields.GetPlayfieldZ(character.Playfield.Identity.Instance);

                IEnumerable<Vendor> vendors = Pool.Instance.GetAll<Vendor>(
                    character.Playfield.Identity,
                    (int)IdentityType.VendingMachine);

                /*                if (vendors.Any())
                {
                    x.PlayfieldVendorInfo = new PlayfieldVendorInfo()
                                            {
                                                VendorCount = vendors.Count(),
                                                FirstVendorId =
                                                    vendors.ElementAt(0).Identity.Instance
                                            };
                }*/
            };

            // TODO: Add the VendorHandler again
            /* var vendorcount = VendorHandler.GetNumberofVendorsinPlayfield(client.Character.PlayField);
            if (vendorcount > 0)
            {
                var firstVendorId = VendorHandler.GetFirstVendor(client.Character.PlayField);
                message.PlayfieldVendorInfo = new PlayfieldVendorInfo
                                                  {
                                                      VendorCount = vendorcount, 
                                                      FirstVendorId = firstVendorId
                                                  };
            }
            */
        }

        #endregion
    }
}