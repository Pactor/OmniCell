#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.ChatCommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Core.Entities;
    using OmniCell.Database.Dao;
    using OmniCell.Enums;
    using OmniCell.ObjectManager;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using ZoneEngine.Core.MessageHandlers;

    /// <summary>
    /// GM-only helpers for creating a small organization test group.
    /// </summary>
    public class OrgTest : AOChatCommand
    {
        public override bool CheckCommandArguments(string[] args)
        {
            if (args.Length < 2)
            {
                return false;
            }

            string action = args[1].ToLowerInvariant();
            return (action == "create" && args.Length >= 3)
                   || (action == "add" && args.Length == 2)
                   || (action == "addname" && args.Length == 3)
                   || (action == "delete" && args.Length == 2);
        }

        public override void CommandHelp(ICharacter character)
        {
            character.Playfield.Publish(
                ChatTextMessageHandler.Default.CreateIM(
                    character,
                    "Usage: .orgtest create <name>, target a character and use .orgtest add, .orgtest addname <character> (online or not, e.g. a chat bot), or .orgtest delete"));
        }

        public override void ExecuteCommand(ICharacter character, Identity target, string[] args)
        {
            string action = args[1].ToLowerInvariant();
            if (action == "create")
            {
                this.Create(character, string.Join(" ", args.Skip(2)));
                return;
            }

            if (action == "add")
            {
                this.AddTarget(character, target);
                return;
            }

            if (action == "addname")
            {
                this.AddByName(character, args[2]);
                return;
            }

            this.Delete(character);
        }

        public override int GMLevelNeeded()
        {
            return 1;
        }

        public override List<string> ListCommands()
        {
            return new List<string> { "orgtest" };
        }

        private void AddTarget(ICharacter leader, Identity target)
        {
            int organizationId = leader.Stats[StatIds.clan].Value;
            if (organizationId == 0 || OrganizationDao.Instance.Get(organizationId) == null)
            {
                this.Send(leader, "Create or join an organization before adding a member.");
                return;
            }

            var member = leader.Playfield.FindByIdentity(target) as Character;
            if (member == null || member.Identity == leader.Identity)
            {
                this.Send(leader, "Target a different logged-in character to add it to your organization.");
                return;
            }

            member.Stats[StatIds.clanlevel].Value = 1;
            member.Stats[StatIds.clan].Value = organizationId;
            member.Controller.SendChangedStats();
            this.Send(leader, member.Name + " was added to organization " + OrganizationDao.Instance.Get(organizationId).Name + ".");
            this.Send(member, "You joined organization " + OrganizationDao.Instance.Get(organizationId).Name + ". Log out and back in to join its chat channel.");
        }

        /// <summary>
        /// Adds a character by name, whether or not it is in the zone. A chat bot
        /// such as Tyrbot only ever logs into chat, so it can never be targeted.
        /// </summary>
        /// <remarks>
        /// Membership is the clan and clanlevel stats in the database, which is
        /// where ChatEngine reads it at chat login. A character that is in the
        /// zone saves its in-memory stats on logout, so those are changed too.
        /// </remarks>
        private void AddByName(ICharacter leader, string name)
        {
            int organizationId = leader.Stats[StatIds.clan].Value;
            var organization = organizationId == 0 ? null : OrganizationDao.Instance.Get(organizationId);
            if (organization == null)
            {
                this.Send(leader, "Create or join an organization before adding a member.");
                return;
            }

            var member = CharacterDao.Instance.GetByCharName(name);
            if (member == null)
            {
                this.Send(leader, "No character named " + name + ".");
                return;
            }

            if (member.Id == leader.Identity.Instance)
            {
                this.Send(leader, "You are already in " + organization.Name + ".");
                return;
            }

            const int CharacterType = (int)IdentityType.CanbeAffected;
            int currentOrganization = StatDao.Instance.GetById(CharacterType, member.Id, (int)StatIds.clan).StatValue;
            var online = Pool.Instance.GetObject<Character>(
                new Identity { Type = IdentityType.CanbeAffected, Instance = member.Id });
            if (online != null)
            {
                currentOrganization = online.Stats[StatIds.clan].Value;
            }

            if (currentOrganization == organizationId)
            {
                this.Send(leader, member.Name + " is already in " + organization.Name + ".");
                return;
            }

            if (currentOrganization != 0)
            {
                this.Send(leader, member.Name + " is in another organization (ID " + currentOrganization + ").");
                return;
            }

            StatDao.SetStat(CharacterType, member.Id, (int)StatIds.clan, organizationId);
            StatDao.SetStat(CharacterType, member.Id, (int)StatIds.clanlevel, 1);

            if (online != null)
            {
                online.Stats[StatIds.clanlevel].Value = 1;
                online.Stats[StatIds.clan].Value = organizationId;
                online.Controller.SendChangedStats();
                this.Send(online, "You joined organization " + organization.Name + ". Log out and back in to join its chat channel.");
            }

            this.Send(
                leader,
                member.Name + " was added to organization " + organization.Name + " (ID " + organizationId
                + "). It joins the org chat channel at its next chat login.");
        }

        private void Create(ICharacter character, string name)
        {
            if (character.Stats[StatIds.clan].Value != 0)
            {
                this.Send(character, "Leave your current organization before creating a test organization.");
                return;
            }

            if (!OrganizationDao.Instance.CreateOrganization(name, DateTime.UtcNow, character.Identity.Instance))
            {
                this.Send(character, "An organization named " + name + " already exists.");
                return;
            }

            int organizationId = OrganizationDao.Instance.GetOrganizationId(name);
            character.Stats[StatIds.clanlevel].Value = 0;
            character.Stats[StatIds.clan].Value = organizationId;
            character.Controller.SendChangedStats();
            this.Send(character, "Created organization " + name + " (ID " + organizationId + ").");
        }

        private void Delete(ICharacter character)
        {
            int organizationId = character.Stats[StatIds.clan].Value;
            var organization = OrganizationDao.Instance.Get(organizationId);
            if (organization == null)
            {
                this.Send(character, "You are not in an organization that can be deleted.");
                return;
            }

            if (organization.LeaderId != character.Identity.Instance)
            {
                this.Send(character, "Only the organization leader can delete this test organization.");
                return;
            }

            StatDao.DisbandOrganization(organizationId);
            OrganizationDao.Instance.Delete(organizationId);
            character.Stats[StatIds.clanlevel].Value = 0;
            character.Stats[StatIds.clan].Value = 0;
            character.Controller.SendChangedStats();
            this.Send(character, "Deleted organization " + organization.Name + ". Members must log out and back in.");
        }

        private void Send(ICharacter character, string text)
        {
            character.Playfield.Publish(ChatTextMessageHandler.Default.CreateIM(character, text));
        }
    }
}
