#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.KnuBot
{
    #region Usings

    using System.Collections.Generic;
    using System.Linq;

    using OmniCell.Database.Entities;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using OmniCell.Core.Entities;
    using OmniCell.Enums;
    using ZoneEngine.Core.Quests;

    #endregion

    /// <summary>
    /// An NPC that hands out quests when you talk to it.
    /// </summary>
    /// <remarks>
    /// The KnuBot framework was already here - dialog trees, options, the
    /// messages, all of it - and nothing used it, so no NPC in the world could
    /// be spoken to and no quest could be started except by typing a command.
    /// This is the first thing to use it.
    ///
    /// It is deliberately not the real conversation. The captures hold 414 lines
    /// of Funcom's dialogue, but only along the branches that were actually
    /// walked: an option not taken leaves no trace, so the tree cannot be
    /// recovered from a playthrough, only the path through it. Writing the real
    /// trees is authoring work, not decoding work.
    ///
    /// What this does instead is the part that can be built honestly - offer
    /// every quest this character gives that you are not already on, take the
    /// answer, and start it. The quest's own name and description are Funcom's
    /// and come from the database; the four lines around them are ours and are
    /// plainly ours.
    ///
    /// One thing it inherits and cannot fix from here: which quests a character
    /// gives comes from DBQuest.GiverId, and that field is not reliably the
    /// giver - see its remarks. Rex Larsson ends up offering thirteen quests
    /// including several that are plainly someone else's. The conversation
    /// works; the list in it is only as good as that column.
    /// </remarks>
    public class QuestGiverKnuBot : BaseKnuBot
    {
        #region Fields

        /// <summary>
        /// What this character can do with you, in the order it is offered.
        /// </summary>
        private readonly List<Offer> offers = new List<Offer>();

        /// <summary>
        /// One line of the answer list: a quest, and whether picking it starts
        /// the quest or finishes it.
        /// </summary>
        private sealed class Offer
        {
            public DBQuest Quest;

            public bool Finishes;
        }

        #endregion

        #region Constructors and Destructors

        public QuestGiverKnuBot(Identity knubotIdentity)
            : base(knubotIdentity)
        {
            var root = new KnuBotDialogTree(
                "root",
                this.Choose,
                new[]
                    {
                        this.CAS(this.Greet, "self"), this.CAS(this.Take, "self"),
                        this.CAS(this.Leave, "self")
                    });

            this.SetRootNode(root);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Works out what an answer means.
        /// </summary>
        /// <remarks>
        /// The last option is always goodbye; everything before it is a quest to
        /// start or one to hand in, in the order they were listed. Anything else is treated as leaving,
        /// because a client that sends an option that was never offered is
        /// either confused or lying and neither deserves a quest.
        /// </remarks>
        private KnuBotAction Choose(KnuBotOptionId optionId)
        {
            if (optionId == KnuBotOptionId.DialogStart)
            {
                return this.Greet;
            }

            var index = (int)optionId;
            return index >= 0 && index < this.offers.Count ? (KnuBotAction)this.Take : this.Leave;
        }

        /// <summary>
        /// Says hello and lists what is on offer.
        /// </summary>
        private void Greet()
        {
            ICharacter talker = this.GetCharacter();
            this.offers.Clear();

            // Finishing comes before starting. A chain is walked by going back
            // to the person who sent you, and if the errand you just did were
            // listed under the next one they had for you it would read as a
            // fresh job rather than the end of the last.
            foreach (DBQuest quest in QuestManager.All()
                .Where(q => q.GiverId == this.KnuBotIdentity.Instance)
                .Where(q => QuestManager.IsComplete(talker, q.Id))
                .Where(q => QuestManager.ProgressOf(talker, q.Id).Any(r => r.State == (int)QuestState.Complete))
                .OrderBy(q => q.Id))
            {
                this.offers.Add(new Offer { Quest = quest, Finishes = true });
            }

            foreach (DBQuest quest in QuestManager.All()
                .Where(q => q.GiverId == this.KnuBotIdentity.Instance)
                .Where(q => !QuestManager.ProgressOf(talker, q.Id).Any())
                .Where(q => QuestManager.ObjectivesOf(q.Id).Count > 0)
                .Where(q => QuestManager.IsUnlocked(talker, q))
                .OrderBy(q => q.Id))
            {
                this.offers.Add(new Offer { Quest = quest, Finishes = false });
            }

            if (this.offers.Count == 0)
            {
                this.WriteLine("I have nothing for you at the moment.");
                this.SendAnswerList("Goodbye");
                return;
            }

            this.WriteLine(
                this.offers[0].Finishes ? "You are back, then." : "There is something you could do for me.");
            this.WriteLine();

            var choices = new List<string>();
            foreach (Offer offer in this.offers)
            {
                string line = offer.Finishes ? "Done: " + offer.Quest.Name : offer.Quest.Name;
                this.WriteLine(line);
                choices.Add(line);
            }

            choices.Add("Goodbye");
            this.SendAnswerList(choices.ToArray());
        }

        /// <summary>
        /// Starts or finishes whichever quest was picked.
        /// </summary>
        /// <remarks>
        /// Which one is picked is not passed in - the framework calls the action
        /// without the option that chose it - so the option is read back from
        /// the last answer the base class handled. That is a limitation of the
        /// framework rather than of this bot, and it is why the offer list is
        /// rebuilt on every greeting: the index has to mean the same thing when
        /// the answer comes back as it did when the list went out.
        /// </remarks>
        private void Take()
        {
            int index = this.LastAnswer;
            if (index < 0 || index >= this.offers.Count)
            {
                this.Leave();
                return;
            }

            Offer offer = this.offers[index];

            if (offer.Finishes)
            {
                this.WriteLine(
                    QuestManager.HandIn(this.GetCharacter(), offer.Quest.Id)
                        ? "That is done. " + offer.Quest.Name + "."
                        : "That one is not finished yet.");
            }
            else
            {
                this.WriteLine(
                    QuestManager.Accept(this.GetCharacter(), offer.Quest.Id)
                        ? "Good. " + offer.Quest.Name + "."
                        : "You cannot take that one.");
            }

            this.CloseChatWindow();
        }

        private void Leave()
        {
            this.WriteLine("Another time, then.");
            this.CloseChatWindow();
        }

        #endregion
    }
}
