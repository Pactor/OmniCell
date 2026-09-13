// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAnarchyFMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAnarchyFMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.PlayfieldAnarchyF)]
    public class PlayfieldAnarchyFMessage : N3Message, IPlayfieldFullUpdate
    {
        #region Constructors and Destructors

        public PlayfieldAnarchyFMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldAnarchyF;
            this.Unknown = 0x00;
            this.Version = 0x00000004;
            this.TokenMarker = 0x61;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// A version, 4, and it decides how much of the message exists.
        /// </summary>
        /// <remarks>
        /// This message is n3PlayfieldFullUpdate with two int32s on the end.
        /// Its own reader at Gamecode.dll 0x101258E3 does nothing but call
        /// n3PlayfieldFullUpdateIIR_t::ReadSubClass, exported from N3.dll at
        /// 0x10029C24, and then read those two - which is why PlayfieldX and
        /// PlayfieldZ are last and everything else belongs to the base.
        ///
        /// The base reads this int32 first and gates on it: above 1 a further
        /// field follows, and above 3 a DbObject_t. Every captured copy carries
        /// 4, so both are always present.
        /// </remarks>
        [AoMember(0)]
        public int Version { get; set; }

        [AoMember(1)]
        public Vector3 CharacterCoordinates { get; set; }

        /// <summary>
        /// 0x61, and anything else abandons the message with "Invalid
        /// playfieldproxy version".
        /// </summary>
        /// <remarks>
        /// This byte and the four fields after it are one PlayfieldProxy_t,
        /// read by N3.dll 0x10038402 and present only when Version is above 1.
        /// Twenty five bytes: this marker, an Identity, two int32s and a second
        /// Identity.
        /// </remarks>
        [AoMember(2)]
        public byte TokenMarker { get; set; }

        /// <summary>
        /// Which playfield this is a copy of, and of what kind - this type
        /// picks the factory the client builds the playfield with.
        /// </summary>
        /// <remarks>
        /// n3EngineClientAnarchy_t::GetPlayfieldFactory switches on it at
        /// Gamecode.dll 0x100188B0: 51102 gets an
        /// AnarchyVirtualPlayfieldFactoryClient_t, 51103 and 51105 get a third
        /// one, and anything else - Playfield1, which is every ordinary
        /// playfield - gets the plain AnarchyPlayfieldFactoryClient_t. Across
        /// the captures it is Playfield1/655 on Rubi-Ka, 51102/6553 in Arete
        /// Landing and 51103/14598183 in a mission, which is the same identity
        /// the mission's own generator carries.
        ///
        /// The name is the client's: n3Playfield_t::GetModelID, exported from
        /// N3.dll at 0x1000C2F7, returns the playfield object's +8 - which is
        /// where the constructor at 0x1000E09B copies the proxy, so +8 is the
        /// proxy's first field and this is it.
        /// </remarks>
        [AoMember(3)]
        public Identity ModelId { get; set; }

        /// <summary>
        /// Which group of copies of that model this one belongs to.
        /// </summary>
        /// <remarks>
        /// The client prints the whole proxy when it wants to say which
        /// playfield it means, and the format string at Gamecode 0x1015877C is
        /// the field list:
        ///
        ///   Pf Proxy: Model=%u:%u GS=%u SG=%u R=%u
        ///
        /// The push sequence at 0x10025BFD feeds it the proxy's +0, +4, +8,
        /// +0xC and +0x14, in that order - so the model is the Identity above,
        /// GS is this, SG is the next, and R is the instance half of the
        /// Identity below.
        ///
        /// Which leaves what GS and SG are short for, and the client names
        /// those too. The engine registers the names of the arguments a spell
        /// function takes, and six consecutive ids in that table at 0x10096200
        /// are a playfield proxy spelled out one field at a time:
        ///
        ///   0x5E DestinationProxyModelType      0x61 DestinationProxySubgroup
        ///   0x5F DestinationProxyModelInstance  0x62 DestinationLocalizerType
        ///   0x60 DestinationProxyGroup          0x63 DestinationLocalizerInstance
        ///
        /// Same six int32s, same order, and the first two are the model the
        /// dump already named. So GS is the group and SG the subgroup.
        ///
        /// PlayfieldProxy_t::operator== at N3.dll 0x1000C0B0 compares the model
        /// identity, this and the next, and nothing else - the identity below
        /// is not part of it, because it is what the lookup finds rather than
        /// what it looks for.
        ///
        /// 1 in all nine Arete Landing captures and 0 in the other seven and in
        /// the mission, so an instanced playfield is group 1 and an ordinary one
        /// group 0.
        /// </remarks>
        [AoMember(4)]
        public int Group { get; set; }

        /// <summary>
        /// Which copy within that group. See <see cref="Group"/>.
        /// </summary>
        /// <remarks>
        /// This is the half of the pair the client has a lookup of its own for.
        /// n3Playfield_t::GetPfWithModelAndSG at N3.dll 0x1000CC55 walks the
        /// live playfields and, for each, reads the object's +0x14 - which is
        /// this field, the proxy sitting at +8 - beside the model identity at
        /// +8, which is what says SG is this one and not the one above.
        ///
        /// Zero in all sixteen captured copies. Nothing here has stood in two
        /// copies of one instanced playfield at once, which is the only thing
        /// that would make it anything else.
        /// </remarks>
        [AoMember(5)]
        public int Subgroup { get; set; }

        /// <summary>
        /// The playfield's own identity - the running copy the character is
        /// standing in, and the same identity the message itself carries.
        /// </summary>
        /// <remarks>
        /// The client's name again: n3Playfield_t::GetIdentity, exported from
        /// N3.dll at 0x1000ABEA, returns the playfield object's +0x18, which is
        /// this field. n3Playfield_t::IsBattleStation at 0x1000C5C0 reads the
        /// instance half of it and answers yes between 0x111C and 0x1124, which
        /// are playfield numbers - so the instance half is a playfield id and
        /// not an arbitrary handle.
        ///
        /// It is Playfield2 in every captured copy, with the instance equal to
        /// the message's own. The dump calls it R.
        /// </remarks>
        [AoMember(6)]
        public Identity PlayfieldId { get; set; }

        /// <summary>
        /// The recipe for a generated playfield, when this is a mission.
        /// </summary>
        /// <remarks>
        /// Everything from here on is not a flat set of fields. The base reader
        /// peeks an Identity, stops if it is empty, and otherwise hands it to
        /// DbObject_t::CreateObject and lets the object read its own body
        /// through its vtable. The registration table at Gamecode.dll
        /// 0x10152100 says what gets built: identity type 51103 is an
        /// ACGBuildingGeneratorData_t, which is a mission, and 51069 is a
        /// TemplatePlayfieldGeneratorData_t, which is an ordinary playfield.
        /// At most one of the two is ever set.
        ///
        /// See PlayfieldAnarchyFSerializer - the attribute serializer has no
        /// way to express an object that decides its own type and length.
        /// </remarks>
        [AoMember(7)]
        public BuildingGeneratorData Generator { get; set; }

        /// <summary>
        /// What the playfield is filled with, when it is not a mission.
        /// </summary>
        [AoMember(8)]
        public PlayfieldTemplateGeneratorData TemplateGenerator { get; set; }

        /// <summary>
        /// Read by this message itself rather than by the base, and 0xFFFFFFFF
        /// for a mission - which has no place on the world map.
        /// </summary>
        [AoMember(9)]
        public int PlayfieldX { get; set; }

        [AoMember(10)]
        public int PlayfieldZ { get; set; }

        #endregion
    }
}