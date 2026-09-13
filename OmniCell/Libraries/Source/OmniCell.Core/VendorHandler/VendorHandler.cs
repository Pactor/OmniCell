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

namespace OmniCell.Core.VendorHandler
{
    #region Usings ...

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Playfields;
    using OmniCell.Core.Statels;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;

    using Quaternion = OmniCell.Core.Vector.Quaternion;

    #endregion

    public static class VendorHandler
    {
        public static void SpawnVendorFromDatabaseTemplate(DBVendor vendor, IPlayfield playfield)
        {
            Identity pfIdentity = new Identity() { Type = IdentityType.Playfield, Instance = vendor.Playfield };
            Identity freeIdentity = new Identity()
                                    {
                                        Type = IdentityType.VendingMachine,
                                        Instance =
                                            Pool.Instance.GetFreeInstance<Vendor>(
                                                0x70000000,
                                                IdentityType.VendingMachine)
                                    };

            Vendor v = new Vendor(pfIdentity, freeIdentity, vendor.Hash);

            v.RawCoordinates = new Vector3(vendor.X, vendor.Y, vendor.Z);
            v.Heading = new Quaternion(vendor.HeadingX, vendor.HeadingY, vendor.HeadingZ, vendor.HeadingW);
            v.Playfield = playfield;
        }

        /// <summary>
        /// The stock a character carries, rather than a machine standing in the
        /// world.
        /// </summary>
        public static void SpawnShopkeeper(DBVendor row, IPlayfield playfield)
        {
            Identity pfIdentity = new Identity()
                                  {
                                      Type = IdentityType.Playfield,
                                      Instance = playfield.Identity.Instance
                                  };

            // Its own identity is the one the live server used, kept because
            // the client trades with the shop by that number and it is what the
            // captures show it being told.
            Identity shop = new Identity() { Type = IdentityType.VendingMachine, Instance = row.Id };

            Vendor v = new Vendor(pfIdentity, shop, row.Hash);
            v.NpcIdentity = new Identity() { Type = IdentityType.CanbeAffected, Instance = row.Npc };
            v.RawCoordinates = new Vector3(row.X, row.Y, row.Z);
            v.Heading = new Quaternion(row.HeadingX, row.HeadingY, row.HeadingZ, row.HeadingW);
            v.Playfield = playfield;
        }

        public static void SpawnEmptyVendorFromTemplate(StatelData statelData, IPlayfield playfield, int instance)
        {
            Identity pfIdentity = new Identity() { Type = IdentityType.Playfield, Instance = statelData.PlayfieldId };
            Identity freeIdentity = new Identity()
                                    {
                                        Type = IdentityType.VendingMachine,
                                        Instance =
                                            Pool.Instance.GetFreeInstance<Vendor>(
                                                0x70000000,
                                                IdentityType.VendingMachine)
                                    };
            Vendor v = new Vendor(pfIdentity, freeIdentity, statelData.TemplateId);
            v.OriginalIdentity = statelData.Identity;
            v.RawCoordinates = new Vector3(statelData.X, statelData.Y, statelData.Z);
            v.Heading = new Quaternion(
                statelData.HeadingX,
                statelData.HeadingY,
                statelData.HeadingZ,
                statelData.HeadingW);
            v.Playfield = playfield;
        }

        public static void SpawnVendorsForPlayfield(IPlayfield playfield, StatelData[] rdbVendors)
        {
            IEnumerable<DBVendor> vendors = VendorDao.Instance.GetWhere(new { Playfield = playfield.Identity.Instance });

            // Shopkeepers first, because they are not statels and the loop
            // below only walks statels. A character's stock has no place of its
            // own - it is wherever the character stands - so it is given the
            // character's position and the character's identity, and the client
            // opens it when that character is clicked.
            foreach (DBVendor keeper in vendors.Where(v => v.Npc != 0))
            {
                SpawnShopkeeper(keeper, playfield);
            }

            foreach (StatelData sd in rdbVendors)
            {
                int id = (((sd.Identity.Instance) >> 16) & 0xff | (playfield.Identity.Instance << 16));

                DBVendor vendor = vendors.FirstOrDefault(x => x.Id == id);
                if (vendor != null)
                {
                    LogUtil.Debug(DebugInfoDetail.Statel, sd.Identity.ToString(true) + " - DB " + vendor.TemplateId);
                    SpawnVendorFromDatabaseTemplate(vendor, playfield);
                }
                else
                {
                    LogUtil.Debug(DebugInfoDetail.Statel, sd.Identity.ToString(true) + " -    " + sd.TemplateId);
                    SpawnEmptyVendorFromTemplate(sd, playfield, id);
                }
            }
        }
    }
}