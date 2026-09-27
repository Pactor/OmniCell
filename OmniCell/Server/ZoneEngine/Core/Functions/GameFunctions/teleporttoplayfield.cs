#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Functions.GameFunctions
{
    #region Usings ...

    using MsgPack;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Vector;
    using OmniCell.Enums;
    using OmniCell.Interfaces;

    using PlayfieldLoader = ZoneEngine.Core.Playfields.PlayfieldLoader;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Quaternion = OmniCell.Core.Vector.Quaternion;

    #endregion

    /// <summary>
    /// The second teleport, and the way out of Arete Landing.
    /// </summary>
    /// <remarks>
    /// Function 53142 was not in the enum and nothing ran it, which is why
    /// nobody could leave the tutorial playfield: Arete's exit door is a
    /// statel whose only behaviour is `OnTargetInVicinity: 53142(3337, 38,
    /// 866, 655)`, and an unknown function is a door that does nothing.
    ///
    /// It is named here from what it is called with rather than from a client
    /// string, and the argument list is the same four
    /// <see cref="FunctionType.Teleport"/> takes - three coordinates and a
    /// playfield. Two things say so. The shape: 13 uses across the pack, all
    /// four arguments. And the destination: 3337, 38, 866 in playfield 655 is
    /// in among that playfield's own doors, one of which sits at 3365, 18,
    /// 835 and teleports with the named function to 3351, 36, 866 - fourteen
    /// metres from where Arete's door sends you.
    ///
    /// The other twelve uses are "Exit the Grid" and a repeating animator
    /// script, and every one of them reads (0, 65000, 0, 0). A playfield of
    /// zero means "this one" to the named teleport, which would put a player
    /// at an altitude of 65,000 in the playfield they are standing in - so
    /// those are left alone rather than run. Whatever the Grid's exit does, it
    /// is not this, and guessing at it would be worse than the door that does
    /// nothing today.
    /// </remarks>
    internal class teleporttoplayfield : FunctionPrototype
    {
        public override FunctionType FunctionId
        {
            get
            {
                return FunctionType.TeleportToPlayfield;
            }
        }

        public override bool Execute(
            INamedEntity self,
            IEntity caller,
            IInstancedEntity target,
            MessagePackObject[] arguments)
        {
            var character = self as Character;
            if (character == null || arguments == null || arguments.Length < 4)
            {
                return false;
            }

            int playfield = arguments[3].AsInt32();
            if (playfield == 0 || !PlayfieldLoader.PFData.ContainsKey(playfield))
            {
                // Not a destination this function can be trusted with. See the
                // class remarks: the Grid's copies carry no playfield at all.
                return false;
            }

            if (playfield == character.Playfield.Identity.Instance)
            {
                return false;
            }

            var destination = new Coordinate(
                arguments[0].AsInt32(), arguments[1].AsInt32(), arguments[2].AsInt32());
            IQuaternion heading = new Quaternion(0.0, 0.0, 0.0, 1.0);

            character.Teleport(
                destination,
                heading,
                new Identity { Type = IdentityType.Playfield, Instance = playfield });
            return true;
        }
    }
}
