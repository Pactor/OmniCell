// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoCriterionOperator.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoCriterionOperator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// What a criterion tests.
    /// </summary>
    /// <remarks>
    /// The third int32 of a criterion, and the client names all of them:
    /// category 2008 of its text database is this list, spelled the way the
    /// client spells it - Operator_Equal, Operator_Less and so on, with the
    /// prefix dropped here.
    ///
    /// They are not all comparisons. Most of the list is questions about the
    /// world rather than about the stat beside them - whether the target is
    /// alive, whether it is a pet, whether a nano is already running - and for
    /// those the stat and value halves of the criterion are the question's
    /// arguments rather than a left and right side.
    ///
    /// Six of the hundred turn up in the captures. Larger leads on 368
    /// criteria, then Less on 265, Equal on 132 and Unequal on 9.
    /// </remarks>
    public enum NanoCriterionOperator
    {
        /// <summary>
        /// "Operator_Equal"
        /// </summary>
        Equal = 0,

        /// <summary>
        /// "Operator_Less"
        /// </summary>
        Less = 1,

        /// <summary>
        /// "Operator_Larger"
        /// </summary>
        Larger = 2,

        /// <summary>
        /// "Operator_Or"
        /// </summary>
        Or = 3,

        /// <summary>
        /// "Operator_And"
        /// </summary>
        And = 4,

        /// <summary>
        /// "Operator_Time_Less"
        /// </summary>
        TimeLess = 5,

        /// <summary>
        /// "Operator_Time_Larger"
        /// </summary>
        TimeLarger = 6,

        /// <summary>
        /// "Operator_Item_Has"
        /// </summary>
        ItemHas = 7,

        /// <summary>
        /// "Operator_Item_HasNot"
        /// </summary>
        ItemHasNot = 8,

        /// <summary>
        /// "Operator_ID"
        /// </summary>
        ID = 9,

        /// <summary>
        /// "Operator_TargetID"
        /// </summary>
        TargetID = 10,

        /// <summary>
        /// "Operator_TargetSignal"
        /// </summary>
        TargetSignal = 11,

        /// <summary>
        /// "Operator_TargetStat"
        /// </summary>
        TargetStat = 12,

        /// <summary>
        /// "Operator_Primary_Item"
        /// </summary>
        PrimaryItem = 13,

        /// <summary>
        /// "Operator_Secondary_Item"
        /// </summary>
        SecondaryItem = 14,

        /// <summary>
        /// "Operator_Area"
        /// </summary>
        Area = 15,

        /// <summary>
        /// "Operator_User"
        /// </summary>
        User = 16,

        /// <summary>
        /// "Operator_ItemAnim"
        /// </summary>
        ItemAnim = 17,

        /// <summary>
        /// "Operator_OnTarget"
        /// </summary>
        OnTarget = 18,

        /// <summary>
        /// "Operator_OnSelf"
        /// </summary>
        OnSelf = 19,

        /// <summary>
        /// "Operator_Signal"
        /// </summary>
        Signal = 20,

        /// <summary>
        /// "Operator_OnSecondaryItem"
        /// </summary>
        OnSecondaryItem = 21,

        /// <summary>
        /// "Operator_BitAnd"
        /// </summary>
        BitAnd = 22,

        /// <summary>
        /// "Operator_BitOr"
        /// </summary>
        BitOr = 23,

        /// <summary>
        /// "Operator_Unequal"
        /// </summary>
        Unequal = 24,

        /// <summary>
        /// "Operator_Illegal"
        /// </summary>
        Illegal = 25,

        /// <summary>
        /// "Operator_OnUser"
        /// </summary>
        OnUser = 26,

        /// <summary>
        /// "Operator_OnValidTarget"
        /// </summary>
        OnValidTarget = 27,

        /// <summary>
        /// "Operator_OnInvalidTarget"
        /// </summary>
        OnInvalidTarget = 28,

        /// <summary>
        /// "Operator_OnValidUser"
        /// </summary>
        OnValidUser = 29,

        /// <summary>
        /// "Operator_OnInvalidUser"
        /// </summary>
        OnInvalidUser = 30,

        /// <summary>
        /// "Operator_HasWornItem"
        /// </summary>
        HasWornItem = 31,

        /// <summary>
        /// "Operator_HasNotWornItem"
        /// </summary>
        HasNotWornItem = 32,

        /// <summary>
        /// "Operator_HasWieldedItem"
        /// </summary>
        HasWieldedItem = 33,

        /// <summary>
        /// "Operator_HasNotWieldedItem"
        /// </summary>
        HasNotWieldedItem = 34,

        /// <summary>
        /// "Operator_HasFormula"
        /// </summary>
        HasFormula = 35,

        /// <summary>
        /// "Operator_HasNotFormula"
        /// </summary>
        HasNotFormula = 36,

        /// <summary>
        /// "Operator_OnGeneralBeholder"
        /// </summary>
        OnGeneralBeholder = 37,

        /// <summary>
        /// "Operator_IsValid"
        /// </summary>
        IsValid = 38,

        /// <summary>
        /// "Operator_IsInvalid"
        /// </summary>
        IsInvalid = 39,

        /// <summary>
        /// "Operator_IsAlive"
        /// </summary>
        IsAlive = 40,

        /// <summary>
        /// "Operator_IsWithinVicinity"
        /// </summary>
        IsWithinVicinity = 41,

        /// <summary>
        /// "Operator_Not"
        /// </summary>
        Not = 42,

        /// <summary>
        /// "Operator_IsWithinWeaponRange"
        /// </summary>
        IsWithinWeaponRange = 43,

        /// <summary>
        /// "Operator_IsNPC"
        /// </summary>
        IsNPC = 44,

        /// <summary>
        /// "Operator_IsFighting"
        /// </summary>
        IsFighting = 45,

        /// <summary>
        /// "Operator_IsAttacked"
        /// </summary>
        IsAttacked = 46,

        /// <summary>
        /// "Operator_IsAnyoneLooking"
        /// </summary>
        IsAnyoneLooking = 47,

        /// <summary>
        /// "Operator_IsFoe"
        /// </summary>
        IsFoe = 48,

        /// <summary>
        /// "Operator_IsInDungeon"
        /// </summary>
        IsInDungeon = 49,

        /// <summary>
        /// "Operator_IsSameAs"
        /// </summary>
        IsSameAs = 50,

        /// <summary>
        /// "Operator_DistanceTo"
        /// </summary>
        DistanceTo = 51,

        /// <summary>
        /// "Operator_IsInNoFightingArea"
        /// </summary>
        IsInNoFightingArea = 52,

        /// <summary>
        /// "Operator_Template_Compare"
        /// </summary>
        TemplateCompare = 53,

        /// <summary>
        /// "Operator_Min_Max_Level_Compare"
        /// </summary>
        MinMaxLevelCompare = 54,

        /// <summary>
        /// "Operator_MonsterTemplate"
        /// </summary>
        MonsterTemplate = 57,

        /// <summary>
        /// "Operator_HasMaster"
        /// </summary>
        HasMaster = 58,

        /// <summary>
        /// "Operator_CanExecuteFormulaOnTarget"
        /// </summary>
        CanExecuteFormulaOnTarget = 59,

        /// <summary>
        /// "Operator_Area_TargetInVicinity"
        /// </summary>
        AreaTargetInVicinity = 60,

        /// <summary>
        /// "Operator_IsUnderHeavyAttack"
        /// </summary>
        IsUnderHeavyAttack = 61,

        /// <summary>
        /// "Operator_IsLocationOk"
        /// </summary>
        IsLocationOk = 62,

        /// <summary>
        /// "Operator_IsNotTooHighLevel"
        /// </summary>
        IsNotTooHighLevel = 63,

        /// <summary>
        /// "Operator_HasChangedRoomWhileFighting"
        /// </summary>
        HasChangedRoomWhileFighting = 64,

        /// <summary>
        /// "Operator_KullNumberOf"
        /// </summary>
        KullNumberOf = 65,

        /// <summary>
        /// "Operator_TestNumPets"
        /// </summary>
        TestNumPets = 66,

        /// <summary>
        /// "Operator_NumberOfItems"
        /// </summary>
        NumberOfItems = 67,

        /// <summary>
        /// "Operator_PrimaryTemplate"
        /// </summary>
        PrimaryTemplate = 68,

        /// <summary>
        /// "Operator_IsTeleporting"
        /// </summary>
        IsTeleporting = 69,

        /// <summary>
        /// "Operator_IsFlying"
        /// </summary>
        IsFlying = 70,

        /// <summary>
        /// "Operator_ScanForStat"
        /// </summary>
        ScanForStat = 71,

        /// <summary>
        /// "Operator_HasMeOnPetlist"
        /// </summary>
        HasMeOnPetlist = 72,

        /// <summary>
        /// "Operator_TrickleDownLarger"
        /// </summary>
        TrickleDownLarger = 73,

        /// <summary>
        /// "Operator_TrickleDownLess"
        /// </summary>
        TrickleDownLess = 74,

        /// <summary>
        /// "Operator_IsPetOverequipped"
        /// </summary>
        IsPetOverequipped = 75,

        /// <summary>
        /// "Operator_HasPetPendingNanoFormula"
        /// </summary>
        HasPetPendingNanoFormula = 76,

        /// <summary>
        /// "Operator_IsPet"
        /// </summary>
        IsPet = 77,

        /// <summary>
        /// "Operator_CanAttackChar"
        /// </summary>
        CanAttackChar = 79,

        /// <summary>
        /// "Operator_IsTowerCreateAllowed"
        /// </summary>
        IsTowerCreateAllowed = 80,

        /// <summary>
        /// "Operator_InventorySlotIsFull"
        /// </summary>
        InventorySlotIsFull = 81,

        /// <summary>
        /// "Operator_InventorySlotIsEmpty"
        /// </summary>
        InventorySlotIsEmpty = 82,

        /// <summary>
        /// "Operator_CanDisableDefenseShield"
        /// </summary>
        CanDisableDefenseShield = 83,

        /// <summary>
        /// "Operator_IsNpcOrNpcControlledPet"
        /// </summary>
        IsNpcOrNpcControlledPet = 84,

        /// <summary>
        /// "Operator_SameAsSelectedTarget"
        /// </summary>
        SameAsSelectedTarget = 85,

        /// <summary>
        /// "Operator_IsPlayerOrPlayerControlledPet"
        /// </summary>
        IsPlayerOrPlayerControlledPet = 86,

        /// <summary>
        /// "Operator_HasEnteredNonPvpZone"
        /// </summary>
        HasEnteredNonPvpZone = 87,

        /// <summary>
        /// "Operator_UseLocation"
        /// </summary>
        UseLocation = 88,

        /// <summary>
        /// "Operator_IsFalling"
        /// </summary>
        IsFalling = 89,

        /// <summary>
        /// "Operator_IsOnDifferentPlayfield"
        /// </summary>
        IsOnDifferentPlayfield = 90,

        /// <summary>
        /// "Operator_HasRunningNano"
        /// </summary>
        HasRunningNano = 91,

        /// <summary>
        /// "Operator_HasRunningNanoLine"
        /// </summary>
        HasRunningNanoLine = 92,

        /// <summary>
        /// "Operator_HasPerk"
        /// </summary>
        HasPerk = 93,

        /// <summary>
        /// "Operator_IsPerkLocked"
        /// </summary>
        IsPerkLocked = 94,

        /// <summary>
        /// "Operator_IsFactionReactionSet"
        /// </summary>
        IsFactionReactionSet = 95,

        /// <summary>
        /// "Operator_HasMoveToTarget"
        /// </summary>
        HasMoveToTarget = 96,

        /// <summary>
        /// "Operator_IsPerkUnlocked"
        /// </summary>
        IsPerkUnlocked = 97,

        /// <summary>
        /// "Operator_True"
        /// </summary>
        True = 98,

        /// <summary>
        /// "Operator_False"
        /// </summary>
        False = 99,

        /// <summary>
        /// "Operator_OnCaster"
        /// </summary>
        OnCaster = 100,

        /// <summary>
        /// "Operator_HasNotRunningNano"
        /// </summary>
        HasNotRunningNano = 101,

        /// <summary>
        /// "Operator_HasNotRunningNanoLine"
        /// </summary>
        HasNotRunningNanoLine = 102
    }
}
