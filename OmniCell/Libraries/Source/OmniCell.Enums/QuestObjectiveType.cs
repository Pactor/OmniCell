#region License

// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace OmniCell.Enums
{
    /// <summary>
    /// What a quest objective asks for.
    /// </summary>
    /// <remarks>
    /// These are OmniCell's own numbering, not Anarchy Online's. The live
    /// protocol carries objectives inside QuestFullUpdate's QuestActions, whose
    /// fields are still mostly unread, so there is no wire value to match here
    /// yet. When there is, this maps onto it rather than being replaced by it.
    /// </remarks>
    public enum QuestObjectiveType
    {
        /// <summary>
        /// Kill a number of a named mob.
        /// </summary>
        Kill = 0,

        /// <summary>
        /// Collect a number of a named item.
        /// </summary>
        Collect = 1,

        /// <summary>
        /// Use an item on a named target.
        /// </summary>
        UseItemOn = 2,

        /// <summary>
        /// Speak to a named character.
        /// </summary>
        TalkTo = 3,

        /// <summary>
        /// Use a fixture of the playfield. The target is its instance id,
        /// written as text, because a fixture has no name of its own.
        /// </summary>
        Use = 4,

        /// <summary>
        /// Go to a place. The target is "x,z" in playfield coordinates.
        /// </summary>
        /// <remarks>
        /// For objectives whose target is absent from the captured world
        /// state. This must not be used when a captured target exists. Arete's
        /// four Gas Fires, for example, are captured fixtures and use
        /// <see cref="UseItemOn"/>; they are never reduced to a reach objective.
        /// A reach target is the exact marker coordinate carried by the quest,
        /// not a fabricated world object.
        /// </remarks>
        Reach = 5,

        /// <summary>
        /// Wear a named item.
        /// </summary>
        Equip = 6,

        /// <summary>
        /// Put a number of a named item into the character's hands.
        /// </summary>
        /// <remarks>
        /// Not Collect, which is satisfied by owning the thing. This one is
        /// satisfied by parting with it: the character asks, a trade window
        /// opens, the player drags the item in and clicks OK, and the item is
        /// gone afterwards.
        ///
        /// Nothing captured uses this yet, because nobody handed anything over
        /// in front of a running sniffer. It is here for quests written in the
        /// game with /questedit, and the extract will use it when a capture of
        /// a hand-over turns up.
        /// </remarks>
        HandIn = 7,

        /// <summary>
        /// Buy a named item from a vendor. Merely acquiring the same item by
        /// another route does not satisfy this objective.
        /// </summary>
        Purchase = 8,

        /// <summary>
        /// Create a named item by completing a tradeskill build. Merely
        /// receiving or looting the same item does not satisfy this objective.
        /// </summary>
        TradeSkill = 9,

        /// <summary>
        /// Use an item from the inventory. The target is the item's id or name.
        /// </summary>
        UseItem = 10,

        /// <summary>
        /// Use an item on a character - the stim on a Wounded Dockworker. The target is the
        /// character's name; TargetLowId, when set, is the item that has to be used.
        /// </summary>
        UseItemOnCharacter = 11,

        /// <summary>
        /// Choose an answer in a conversation. The target is the answer's text.
        /// </summary>
        DialogueAnswer = 12
    }

    /// <summary>
    /// What one row of a character's conversation is.
    /// </summary>
    /// <remarks>
    /// The Kind column of knubotscript. Rows are grouped into steps and
    /// ordered within one, so a step is some lines the character says, then
    /// optionally a request for items, then the answers the window offers.
    /// </remarks>
    public enum ScriptLine
    {
        /// <summary>
        /// A line the character says.
        /// </summary>
        Says = 0,

        /// <summary>
        /// A line the player can say back.
        /// </summary>
        Answer = 1,

        /// <summary>
        /// The character asks for something to be handed over, which opens a
        /// trade window. The text is the wording on it.
        /// </summary>
        WantsItems = 2
    }

    /// <summary>
    /// A server action attached to a selectable dialogue answer.
    /// </summary>
    public enum ScriptAction
    {
        None = 0,
        AcceptQuest = 1,
        TurnInQuest = 2,
        OpenQuestTrade = 3,
        CloseDialogue = 4
    }

    /// <summary>
    /// How far a character has got with a quest.
    /// </summary>
    public enum QuestState
    {
        /// <summary>
        /// Never started. Not stored - the absence of a row means this.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Accepted, objective not yet met.
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Objective met, not yet handed in.
        /// </summary>
        Complete = 2,

        /// <summary>
        /// Handed in and paid out.
        /// </summary>
        HandedIn = 3
    }
}
