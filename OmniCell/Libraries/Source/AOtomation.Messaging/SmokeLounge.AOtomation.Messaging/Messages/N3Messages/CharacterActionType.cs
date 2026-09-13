// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterActionType.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterActionType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// What a CharacterAction asks the server to do.
    /// </summary>
    /// <remarks>
    /// Most of these names are the client's own. Gamecode.dll exports 379
    /// methods of n3EngineClientAnarchy_t with their names intact - it is the
    /// whole client-to-server surface - and the ones that send a character
    /// action push the number as a literal into CharacterActionIIR_t's
    /// constructor at 0x10072A88. Tools/Capture/ClientActions.py reads them
    /// back out, which is where fifty of these come from.
    ///
    /// That corrected several numbers that had been guessed from captures and
    /// were wrong, so a handler keyed to them could never have fired: leaving a
    /// team is 24 and not 32, replying to an invite is 28 and not 21, splitting
    /// a stack is 52 and joining two is 53 rather than 34 and 35, and cancelling
    /// a camp is 121 rather than 122.
    ///
    /// Where the client has no exported name the older one is kept and says so.
    /// </remarks>
    public enum CharacterActionType
    {
        CastNanoSpell = 0x00000013,

        KickTeamMember = 0x00000016,

        /// <summary>
        /// The client sends 24 here. This was 32 until the client was asked, so
        /// nothing the server did for a leaving member had ever run.
        /// </summary>
        LeaveTeam = 0x00000018,

        TransferTeamLeadership = 0x00000019,

        TeamJoinRequest = 0x0000001A,

        /// <summary>
        /// Answering an invitation, with the yes or no in the first parameter.
        /// Generic rather than team specific, which is what the client calls it.
        /// This was 21 until the client was asked, and a two account capture
        /// shows the invited character sending 28 with a 1 the moment they
        /// accept.
        /// </summary>
        RequestReply = 0x0000001C,

        /// <summary>
        /// Server to client. A team request, with the kind of request in the
        /// second parameter.
        /// </summary>
        /// <remarks>
        /// The client has no exported name for this one; what it does with it
        /// is the evidence. The switch at 0x1005D6C7 sends action 21 to
        /// 0x1005D7CB, which hands the second parameter to the team's request
        /// routine at 0x10065C04 - and that routine acts only on 17 through 20
        /// and 24, and ignores everything else. The one captured copy carries
        /// 17, and arrives at the inviting character just before the
        /// TeamMember messages.
        ///
        /// The older table called this TeamRequestReply. The reply is 28: a two
        /// account capture shows the invited character sending 28, and the
        /// client's own N3Msg_RequestReply pushes 28.
        /// </remarks>
        TeamRequestNotice = 0x00000015,

        /// <summary>
        /// Server to client. The character in this message now belongs to the
        /// team in it.
        /// </summary>
        /// <remarks>
        /// Action 35 goes to 0x1005D8CB, which passes the message's team
        /// identity to 0x10065C8B - the same routine TeamMember's dispatcher
        /// uses to put a team on a member, and the same one that writes a null
        /// identity over three of the member's fields when the instance half is
        /// empty. The captured copy carries the character and the TeamWindow
        /// identity of the team that had just formed.
        ///
        /// The older table called this AcceptTeamRequest and the server code
        /// under that name was merging item stacks, which is 53.
        /// </remarks>
        TeamAssigned = 0x00000023,

        SplitItem = 0x00000034,

        /// <summary>
        /// Merging one stack into another. This was 35 in the old table under the
        /// name AcceptTeamRequest, and the server code under that name has always
        /// been stack merging - the name was wrong, not the code.
        /// </summary>
        JoinItems = 0x00000035,

        HideAgainstOpponent = 0x00000036,

        RemoveBuff = 0x00000041,

        UseSkill = 0x00000046,

        TradeskillCombine = 0x00000051,

        /// <summary>
        /// One action for sitting down and standing up, which is why the client
        /// calls it a toggle.
        /// </summary>
        /// <summary>
        /// Puts a quest in the mission window, or takes it out.
        /// </summary>
        /// <remarks>
        /// The live server sends this immediately before the QuestMessage and
        /// the QuestFullUpdate that describe a quest being handed over -
        /// packets 30512, 30513 and 30516 of one captured session, in that
        /// order. Target is the quest, and its identity is repeated in the two
        /// parameters: Parameter1 is 56003, the identity type of a quest, and
        /// Parameter2 the quest's own id.
        ///
        /// Without it the client is told about a quest it has no window entry
        /// for: it says to check the mission window and there is nothing in it.
        /// </remarks>
        MissionChanged = 0x0000003B,

        SitToggle = 0x00000057,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        Unknown3 = 0x00000061,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        SetNanoDuration = 0x00000062,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        Search = 0x00000066,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        InfoRequest = 0x00000069,

        UseItem = 0x0000006A,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        FinishNanoCasting = 0x0000006B,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        InterruptNanoCasting = 0x0000006C,

        ToggleReclaim = 0x0000006E,

        DeleteItem = 0x00000070,

        /// <summary>
        /// /camp, which is logging out while sitting down. The older table called
        /// this Logout, and the number was right.
        /// </summary>
        StartCamping = 0x00000078,

        /// <summary>
        /// Cancelling it. This was 122 in the older table and the client sends
        /// 121, so the server never saw a cancelled logout.
        /// </summary>
        StopCamping = 0x00000079,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        Equip = 0x00000083,

        ResetSkill = 0x0000009A,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        StartedSneaking = 0x000000A2,

        TryEnterSneakMode = 0x000000A3,

        /// <summary>
        /// The older table called this ChangeVisualFlag. The number is the same
        /// and the client disagrees about what it is for.
        /// </summary>
        EventFeedback = 0x000000A6,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        ChangeAnimationAndStance = 0x000000A7,

        RequestChecklist = 0x000000AF,

        SetPlayerOption = 0x000000B8,

        StartAltState = 0x000000C6,

        StopAltState = 0x000000C7,

        Forage = 0x000000CA,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        UploadNano = 0x000000CC,

        DeleteNano = 0x000000D3,

        InsertSourceAnalyzerItem = 0x000000DC,

        InsertTargetAnalyzerItem = 0x000000DD,

        BuildAnalyzerItem = 0x000000DE,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillSource = 0x000000DF,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillTarget = 0x000000E0,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillNotValid = 0x000000E1,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillOutOfRange = 0x000000E2,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillRequirement = 0x000000E3,

        /// <summary>
        /// No exported name. From the older table.
        /// </summary>
        TradeskillResult = 0x000000E4,

        PetDuelChallenge = 0x000000EF,

        /// <summary>
        /// Accepting and refusing a pet duel are the same action; the client
        /// tells them apart with a value it writes into the message.
        /// </summary>
        PetDuelReply = 0x000000F0,

        PetDuelStop = 0x000000F1,

        AddToQueue = 0x000000FD,

        GetInfo = 0x000000FE,

        LeaveQueue = 0x000000FF,

        RefreshLaserTags = 0x00000100,

        ArtilleryAttack = 0x00000101,

        OrbitalAttack = 0x00000102,

        Airstrike = 0x00000103,

        GetPointLocations = 0x00000104,

        Inspect = 0x00000105,

        /// <summary>
        /// Challenging, accepting, refusing, drawing and stopping a duel are all
        /// this one action - five exported functions push 262 - and the client
        /// tells them apart with a value it writes into the message.
        /// </summary>
        Duel = 0x00000106,

        /// <summary>
        /// The client asking for its claims. The older table called this
        /// FinishedLoading because of when it arrives: once per zone, after
        /// CharInPlay and before the first movement. The timing was right and the
        /// name was not, and the live server answers it with nothing at all -
        /// there is no 263 anywhere in the server half of any capture.
        /// </summary>
        RequestClaims = 0x00000107,

        RequestClaimsDBCheck = 0x00000108,
    }
}
