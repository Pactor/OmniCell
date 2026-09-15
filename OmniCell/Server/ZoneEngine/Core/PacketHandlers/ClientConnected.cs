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

namespace ZoneEngine.Core.PacketHandlers
{
    #region Usings ...

    using System.Linq;
    using System.Text;

    using OmniCell.Core.Entities;
    using OmniCell.Core.Playfields;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using NLog;

    using Utility;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

    using ZoneEngine.Core.InternalMessages;
    using ZoneEngine.Core.Playfields;
    using ZoneEngine.Core.MessageHandlers;
    using ZoneEngine.Core.Quests;
    using ZoneEngine.Core.Packets;
    using ZoneEngine.Script;

    #endregion

    /// <summary>
    /// </summary>
    public class ClientConnected
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="str">
        /// </param>
        /// <returns>
        /// </returns>
        public static byte[] StrToByteArray(string str)
        {
            var encoding = new ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        /// <summary>
        /// </summary>
        /// <param name="charID">
        /// </param>
        /// <param name="client">
        /// </param>
        public void Read(int charID, ZoneClient client)
        {
            // Don't edit anything in this region
            // unless you are 300% sure you know what you're doing

            // Character is created and read when Client connects in Client.cs->CreateCharacter
            // client.CreateCharacter(charID);
            // Before anything is sent, because the character's health goes out
            // in FullCharacterMessage a moment from now.
            FillUpANewCharacter(client.Controller.Character);

            client.Server.Info(
                client,
                "Client connected. ID: {0} IP: {1} Character name: {2}",
                client.Controller.Character.Identity.Instance,
                client.ClientAddress,
                client.Controller.Character.Name);

            // now we have to start sending packets like 
            // character stats, inventory, playfield info
            // and so on. I will put some packets here just 
            // to get us in game. We have to start moving
            // these packets somewhere else and make packet 
            // builders instead of sending (half) hardcoded
            // packets.

            /* send chat server info to client */
            ChatServerInfoMessageHandler.Default.Send(client.Controller.Character);

            /* send playfield info to client */
            PlayfieldAnarchyFMessageHandler.Default.Send(client.Controller.Character);

            // The fixtures of the playfield - terminals, the Cargo Box a quest
            // asks you to open, the Gas Fire another asks you to put out.
            //
            // Here because this is where the live server has them: in a capture
            // of walking into Arete Landing they are packets 3 and 4, straight
            // after the playfield message and before the first character. They
            // used to go out at the end of CharInPlay instead, in reply to the
            // client saying it had finished loading, and they did not arrive -
            // 85 of them sat in the playfield and none reached the client.
            //
            // The client does not draw these from its own data. The live server
            // despawns them as you walk away and sends them again when you come
            // back, which is not something you do to geometry the client owns.
            StaticDynel[] fixtures =
                Pool.Instance.GetAll<StaticDynel>(client.Controller.Character.Playfield.Identity).ToArray();
            foreach (StaticDynel fixture in fixtures)
            {
                // A fixture used and not back yet is not there to be sent.
                if (!fixture.Hidden)
                {
                    SimpleItemFullUpdateMessageHandler.Default.Send(client.Controller.Character, fixture);
                }
            }

            // Logged at info rather than behind the statel debug switch, because
            // "how many fixtures did that client actually get" is the question
            // that went unanswered while the Cargo Box was missing.
            Log.Info(
                "ENTRY character={0} playfield={1} fixtures={2}",
                client.Controller.Character.Identity.Instance,
                client.Controller.Character.Playfield.Identity.Instance,
                fixtures.Length);

            foreach (
Vendor vendor in
Pool.Instance.GetAll<Vendor>(
client.Controller.Character.Playfield.Identity,
(int)IdentityType.VendingMachine))
            {
                VendingMachineFullUpdateMessageHandler.Default.Send(client.Controller.Character, vendor);
            }

            // Doors. The live server reports the state of every door in a
            // playfield on entry - 28 of them on walking into the subway - and
            // OmniCell reported none.
            foreach (Identity door in ((Playfield)client.Playfield).Doors())
            {
                DoorStatusUpdateMessageHandler.Default.Send(client.Controller.Character, door);
            }

            var sendSCFUs = new IMSendPlayerSCFUs { toClient = client };
            ((Playfield)client.Playfield).SendSCFUsToClient(sendSCFUs);

            /* set SocialStatus to 0 */
            client.Controller.Character.Stats[521].BaseValue = 0;

            // Stat.SendDirect(client, 521, 0, false);

            var identity = new Identity { Type = IdentityType.CanbeAffected, Instance = charID };

            // The player's own character, then the weapon in its hands, before
            // the game time and FullCharacter. That is the live order in both
            // captures of an entry: 20260911-171203_s12 seq 16, 17, then 165 and
            // 169; newchar_s9 seq 461, 462, then 466 and 470.
            //
            // Straight down this client's queue, which keeps order. This went out
            // through the playfield, whose bus does not: a probe of the server
            // received GameTime (seq 150) ahead of it (seq 151). Everyone else in
            // the playfield still hears about it through the playfield.
            SimpleCharFullUpdateMessage self = SimpleCharFullUpdate.ConstructMessage(client);
            client.SendCompressed(self);
            client.Controller.Character.Playfield.AnnounceOthers(self, identity);

            // An equipped slot in FullCharacter references a WeaponInstance.
            // Live defines that instance before FullCharacter, never after it.
            // A new session starts with a client that knows no weapons.
            WeaponItemFullUpdateMessageHandler.BeginSession(client.Controller.Character);
            var weaponPage =
                client.Controller.Character.BaseInventory.Pages[(int)IdentityType.WeaponPage];
            foreach (var weapon in weaponPage.List())
            {
                if (!Combat.CombatWeaponProfiles.IsPlayerWeaponSlot(weapon.Key))
                {
                    continue;
                }

                WeaponItemFullUpdateMessageHandler.Default.Send(
                    client.Controller.Character,
                    weapon.Value,
                    weapon.Key);
            }

            // No ChangeAnimationAndStance here. CellAO sent one ("Action 167
            // Animation and Stance Data maybe?") and none of the 59 player entries
            // in the retail captures carries one between the player's own
            // SimpleCharFullUpdate and CharInPlay.

            var gameTimeMessage = new GameTimeMessage
                                  {
                                      Identity = identity,
                                      CurrentGameTime = 30024.0f,
                                      CurrentGameDay = 185408,

                                      // The field was typed float here and is an int32 on the
                                      // wire; this is the same four bytes we were already
                                      // sending, kept so the change is a retype and not a
                                      // behaviour change. What the client wants in it is not
                                      // established - see GameTimeMessage.SystemTimeReference.
                                      SystemTimeReference = 1201445800
                                  };
            client.SendCompressed(gameTimeMessage);


            /* set SocialStatus to 0 */
            // Stat.SendDirect(client, 521, 0, false);

            /* again */
            // Stat.SendDirect(client, 521, 0, false);

            /* inventory, items and all that */
            FullCharacterMessageHandler.Default.Send(client.Controller.Character);

            // The tower and city lists, empty here - 10 of each across the
            // captured sessions, all empty, because the newbie area and the
            // subway have neither. Sending them empty is what the client is
            // waiting for; sending nothing leaves it waiting.
            //
            // They come after the character, not before it. That is the order in
            // the capture, and the order of this whole sequence is the client's
            // business, not ours to improve on.
            // The instance, like PlayfieldAnarchyF and every character in the
            // playfield. These two carried the playfield number instead, which
            // put them in a different playfield from everything else the client
            // had just been sent - the same mistake as the last one, one layer
            // down, and it is why this is worth saying twice: anything that
            // names the playfield to the client names the instance.
            var playfieldIdentity = new Identity
                                    {
                                        Type = IdentityType.Playfield2,
                                        Instance =
                                            Playfields.GetClientInstance(
                                                client.Controller.Character.Playfield.Identity.Instance)
                                    };

            client.SendCompressed(
                new PlayfieldAllTowersMessage
                    {
                        Identity = playfieldIdentity,
                        Unknown = 1,
                        Towers = new TowerProxyBase[0]
                    });

            client.SendCompressed(
                // Houses stays null, which is what a playfield with no city
                // looks like: the two-byte payload length goes out as zero and
                // nothing follows it. Every captured copy is this.
                new PlayfieldAllCitiesMessage { Identity = playfieldIdentity, Unknown = 1 });

            SpecialAttackWeaponMessageHandler.Default.SendLogin(client.Controller.Character);

            // done


            // spawn all active monsters to client
            // TODO: Implement NonPlayerCharacterHandler
            // NonPlayerCharacterHandler.SpawnMonstersInPlayfieldToClient(client, client.Character.PlayField);

            // TODO: Implement VendorHandler
            // if (VendorHandler.GetNumberofVendorsinPlayfield(client.Character.PlayField) > 0)
            // {
            // Shops 
            // VendorHandler.GetVendorsInPF(client);
            // }

            // WeaponItemFullCharUpdate  Maybe the right location , First Check if weapons present usually in equipment
            // Packets.WeaponItemFullUpdate.Send(client, client.Character);

            // TODO: create a better alternative to ProcessTimers
            // client.Character.ProcessTimers(DateTime.Now + TimeSpan.FromMilliseconds(200));
            client.Controller.Character.CalculateSkills();

            AppearanceUpdateMessageHandler.Default.SendEntering(client.Controller.Character);

            // And this is the one that ends the loading screen: the player's
            // own CharInPlay, once, last. The client answers it with the same
            // message and Unknown set to 1, which CharInPlayMessageHandler picks
            // up to announce the new arrival to everyone else.
            //
            // After the appearance, which is where both captures of an entry have
            // it: 20260911-171203_s12 seq 177 then 192, newchar_s9 seq 474 then
            // 476.
            //
            // This used to be announced through the playfield on the theory that
            // the appearance went the same way and one queue keeps order. The
            // playfield bus does not keep order: a probe of the server as Bobo
            // received this (seq 157) before the appearance (seq 158). Both now go
            // straight down this client's own queue, which does, and everyone else
            // is told through the playfield as before.
            // The research list, directly before CharInPlay, as in all 59 retail
            // entries. See ResearchUpdateMessageHandler.SendEntering.
            ResearchUpdateMessageHandler.Default.SendEntering(client.Controller.Character);

            var inPlay = new CharInPlayMessage { Identity = identity, Unknown = 0x00 };
            client.SendCompressed(inPlay);
            client.Controller.Character.Playfield.AnnounceOthers(inPlay, identity);

            // Quests last, because sending the window needs the character to be
            // in the playfield already. Loading the progress also sends it.
            QuestManager.LoadFor(client.Controller.Character);

            // done, so we call a hook.
            // Call all OnConnect script Methods
            ScriptCompiler.Instance.CallMethod("OnConnect", client.Controller.Character);

            // Timers are allowed to update client stats now.
            client.Controller.Character.DoNotDoTimers = false;
        }

        /// <summary>
        /// A character that has never been played starts full.
        /// </summary>
        /// <remarks>
        /// The captures show a level 1 in Arete Landing at 38 of 38 health and
        /// 31 of 31 nano. OmniCell walked them in on one of each, because
        /// character creation wrote a literal 1 and nothing ever raised it - the
        /// first thing a new character saw was a health bar with one point in
        /// it, and the first heartbeat regenerated it by four.
        ///
        /// Creation cannot write the right number: the maximum comes out of the
        /// breed and profession tables against a stat list the login server does
        /// not build. So it writes zero, meaning never set, and this fills it in
        /// here where the formulas can be asked. Setting Value runs it through
        /// the stat's own ceiling, so what lands is exactly the maximum.
        ///
        /// Zero is not a value a played character can be stored with: a
        /// character that dies is put back on its feet before its stats are
        /// written, so nothing else writes it, and a character that has one is
        /// new.
        /// </remarks>
        private static void FillUpANewCharacter(ICharacter character)
        {
            if (character.Stats[StatIds.health].BaseValue == 0)
            {
                character.Stats[StatIds.health].Value = character.Stats[StatIds.life].Value;
            }

            if (character.Stats[StatIds.currentnano].BaseValue == 0)
            {
                character.Stats[StatIds.currentnano].Value = character.Stats[StatIds.maxnanoenergy].Value;
            }
        }

        #endregion
    }
}
