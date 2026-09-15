#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace ZoneEngine.Core.Quests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using OmniCell.Database.Entities;
    using OmniCell.Enums;

    /// <summary>
    /// Deterministic quest-state rules shared by the runtime and source-built
    /// tests. Persistence, inventory delivery and client messages remain in
    /// <c>QuestManager</c>.
    /// </summary>
    public static class QuestStateRules
    {
        public static string ObjectiveValidationError(DBQuestObjective objective)
        {
            if (objective == null)
            {
                return "objective row is missing";
            }

            if (!Enum.IsDefined(typeof(QuestObjectiveType), objective.ObjectiveType))
            {
                return "objective type " + objective.ObjectiveType + " is unsupported";
            }

            if (objective.Required < 1)
            {
                return "required count must be at least one";
            }

            if (string.IsNullOrWhiteSpace(objective.Target))
            {
                return "target is empty";
            }

            QuestObjectiveType kind = (QuestObjectiveType)objective.ObjectiveType;
            if (kind == QuestObjectiveType.Use)
            {
                int instance;
                if (!int.TryParse(objective.Target, NumberStyles.Integer, CultureInfo.InvariantCulture, out instance)
                    || instance < 1)
                {
                    return "fixture target is not a positive instance id";
                }
            }

            if (kind == QuestObjectiveType.Reach)
            {
                string[] parts = objective.Target.Split(',');
                float x;
                float z;
                if (parts.Length != 2
                    || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out z)
                    || float.IsNaN(x)
                    || float.IsInfinity(x)
                    || float.IsNaN(z)
                    || float.IsInfinity(z))
                {
                    return "reach target is not a finite x,z coordinate";
                }
            }

            if (kind == QuestObjectiveType.HandIn
                && (objective.TargetLowId < 1 || objective.TargetHighId < 1 || objective.TargetQuality < 1))
            {
                return "hand-in target has no exact low/high/QL identity";
            }

            return null;
        }

        public static bool IsSupportedObjective(DBQuestObjective objective)
        {
            return ObjectiveValidationError(objective) == null;
        }

        public static bool IsUnlocked(DBQuest quest, IEnumerable<DBCharacterQuest> progress)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.Requires == 0)
            {
                return true;
            }

            return progress.Any(
                row => row.QuestId == quest.Requires && row.State == (int)QuestState.HandedIn);
        }

        public static bool CanAccept(
            DBQuest quest,
            IEnumerable<DBCharacterQuest> progress,
            int objectiveCount)
        {
            if (quest == null || objectiveCount < 1)
            {
                return false;
            }

            IList<DBCharacterQuest> rows = progress as IList<DBCharacterQuest> ?? progress.ToList();
            return IsUnlocked(quest, rows) && rows.All(row => row.QuestId != quest.Id);
        }

        /// <summary>
        /// The names an objective's target stands for. A kill counter can name a group of creatures -
        /// "Junkyard Robots" for Cleaning Robots and Malfunctioning Cleaning Robots (follow_new #8628) -
        /// written "label|creature|creature"; any other target is one name.
        /// </summary>
        public static string[] TargetNames(string target)
        {
            return (target ?? string.Empty).Split('|');
        }

        /// <summary>
        /// What the kill counter calls an objective's creatures: the label, or the one name.
        /// </summary>
        public static string CounterName(string target)
        {
            return TargetNames(target)[0];
        }

        public static bool TryAdvance(
            DBCharacterQuest row,
            DBQuestObjective objective,
            QuestObjectiveType kind,
            string target,
            int amount)
        {
            if (row == null
                || objective == null
                || row.QuestId != objective.QuestId
                || row.Ordinal != objective.Ordinal
                || row.State != (int)QuestState.InProgress
                || objective.Required < 1
                || objective.ObjectiveType != (int)kind
                || string.IsNullOrEmpty(target)
                || !TargetNames(objective.Target).Any(name => string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
                || amount < 1)
            {
                return false;
            }

            long advanced = (long)row.Progress + amount;
            row.Progress = (int)Math.Min(advanced, objective.Required);
            if (row.Progress >= objective.Required)
            {
                row.State = (int)QuestState.Complete;
            }

            return true;
        }

        public static bool IsComplete(IEnumerable<DBCharacterQuest> progress, int questId)
        {
            List<DBCharacterQuest> mine = progress.Where(row => row.QuestId == questId).ToList();
            return mine.Count > 0 && mine.All(row => row.State >= (int)QuestState.Complete);
        }

        public static bool CanHandIn(IEnumerable<DBCharacterQuest> progress, int questId)
        {
            List<DBCharacterQuest> mine = progress.Where(row => row.QuestId == questId).ToList();
            return mine.Count > 0
                   && mine.All(row => row.State == (int)QuestState.Complete);
        }

        public static bool CanAbandon(IEnumerable<DBCharacterQuest> progress, int questId)
        {
            List<DBCharacterQuest> mine = progress.Where(row => row.QuestId == questId).ToList();
            return mine.Count > 0
                   && mine.All(row => row.State != (int)QuestState.HandedIn);
        }
    }
}
