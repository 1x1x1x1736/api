/*
 * Seralyth Menu  Classes/Mods/ChangelogEntry.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Seralyth Software
 * https://github.com/Seralyth/Seralyth-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using Seralyth.Classes.Menu;
using Seralyth.Menu;
using System.Collections.Generic;

namespace Seralyth.Classes.Mods
{
    public static class Changelog
    {
        private static List<ChangelogEntry> entries = new List<ChangelogEntry>();
        private static bool populated;

        private static readonly Dictionary<string, string> typeDisplayNames = new Dictionary<string, string>
        {
            { "ADDED", "ADDED" },
            { "REMOVED", "REMOVED" },
            { "UPDATED", "UPDATED" },
            { "FIXED", "FIXED" },
        };

        public static string GetTypeDisplayName(string type)
        {
            if (typeDisplayNames.TryGetValue(type, out string displayName))
                return displayName;
            return type;
        }

        public static List<ChangelogEntry> Entries
        {
            get
            {
                if (!populated)
                {
                    AutoChangelogEntries.Populate();
                    populated = true;
                }
                return entries;
            }
        }

        public static void Add(string type, string description)
        {
            entries.Add(new ChangelogEntry { type = type, description = description });
        }

        public static void Clear()
        {
            entries.Clear();
            populated = false;
        }

        public static void RefreshCategory()
        {
            if (!populated)
            {
                AutoChangelogEntries.Populate();
                populated = true;
            }

            int catIndex = Buttons.GetCategory("Update Category");
            if (catIndex < 0) return;

            Buttons.buttons[catIndex] = new ButtonInfo[]
            {
                new ButtonInfo { buttonText = "Exit Update Category", method = () => Buttons.CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page." },
            };

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                string color = entry.type == "ADDED" ? "green" : entry.type == "REMOVED" ? "red" : entry.type == "UPDATED" ? "yellow" : "purple";
                string display = $"<color={color}>[{GetTypeDisplayName(entry.type)}]</color> {entry.description}";
                Buttons.AddButton(catIndex, new ButtonInfo { buttonText = display, label = true }, 1);
            }
        }
    }

    public class ChangelogEntry
    {
        public string type;
        public string description;
    }
}
