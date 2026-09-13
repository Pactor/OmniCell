// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SocialAction.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SocialAction type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// The emote a <see cref="SocialActionCmdMessage"/> carries.
    /// </summary>
    /// <remarks>
    /// The client holds the whole list as a table of {command, id, alternate}
    /// records - Gamecode.dll 0x1015FBD8 and the same table again in GUI.dll at
    /// 0x101BAE30 - and it runs from 1 to 70 with no gaps, followed by a
    /// terminator whose command is null, whose id is 71 and whose animation is
    /// noanim_01_01. That is where every name and number below comes from.
    ///
    /// Three of the commands have a second spelling that means the same emote:
    /// allah is prostrate, ass is itch, and crotch is adjust. Four more carry an
    /// animation name in that third slot rather than an alias - kiss_01_01,
    /// kissdown_01_01, kissup_01_01 and hug_01_01 - which is why those four sit
    /// at the end of the table: they were added after the rest.
    ///
    /// The category prefixes on these names are not the client's. They came from
    /// CellAO and server code refers to them, so they stay; the client's own
    /// command word is on each one.
    /// </remarks>
    public enum SocialAction
    {
        /// <summary>crossarm.</summary>
        GesturesCrossarm = 13,

        /// <summary>kiss.</summary>
        ApprovalKiss = 64,

        /// <summary>hug.</summary>
        ApprovalHug = 67,

        /// <summary>sleep.</summary>
        RelaxingSleep = 68,

        /// <summary>facepalm.</summary>
        GesturesFacepalm = 70,

        GreetingBow = 9, 

        GreetingCurt = 15, 

        GreetingGreet = 25, 

        GreetingKneel = 27, 

        GreetingSalute = 45, 

        GreetingScared = 46, 

        GreetingSurprised = 57, 

        GreetingSurrender = 58, 

        GreetingWave = 62, 

        GesturesCross = 12, 

        GesturesFishsize = 20, 

        GesturesGloat = 24, 

        GesturesItalian = 26, 

        GesturesNod = 33, 

        GesturesNono = 34, 

        GesturesPray = 40, 

        GesturesShrug = 49, 

        GesturesSpeech = 51, 

        GesturesThinker = 60, 

        ApprovalApplause = 4, 

        ApprovalBlowkiss = 8, 

        ApprovalGiggle = 23, 

        ApprovalKisshigh = 66, 

        ApprovalKisslow = 65, 

        ApprovalLaughB = 28, 

        ApprovalLaughS = 29, 

        ApprovalProstrate = 1, 

        ApprovalSwroyal = 59, 

        ApprovalThumbs = 61, 

        DislikeAngry = 2, 

        DislikeBulge = 10, 

        DislikeFblock = 19, 

        DislikeFlip = 22, 

        DislikeMoon = 32, 

        DislikePuke = 41, 

        DislikeShake = 48, 

        DislikeSlap = 50, 

        DislikeSpit = 52, 

        DanceBallet = 7, 

        DanceChicken = 11, 

        DanceDisco = 16, 

        DanceFlamenco = 21, 

        DancePulp = 42, 

        DanceRocky = 44, 

        DanceYmca = 63, 

        AthleteBackflip = 6, 

        AthleteStrong1 = 53, 

        AthleteStrong2 = 54, 

        AthleteStrong3 = 55, 

        AthleteStrong4 = 56, 

        RelaxingAdjust = 14, 

        RelaxingDrink = 17, 

        RelaxingEat = 18, 

        RelaxingItch = 5, 

        RelaxingLegshake = 30, 

        RelaxingLounge = 69, 

        RelaxingRead = 43, 

        RelaxingScratch = 47, 

        DirectionsApachi = 3, 

        DirectionsLookout = 31, 

        DirectionsPointba = 35, 

        DirectionsPointfor = 36, 

        DirectionsPointlef = 37, 

        DirectionsPointrig = 38, 

        DirectionsPointup = 39
    }
}