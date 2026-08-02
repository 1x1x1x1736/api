/*
 * Seralyth Menu  Patches/Menu/ComputerCategoryPatch.cs
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
using GorillaLocomotion;
using GorillaNetworking;
using HarmonyLib;
using Seralyth.Classes.Menu;
using Seralyth.Managers;
using Seralyth.Menu;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Seralyth.Patches.Menu
{
    public static class ComputerCategory
    {
        public static bool InCategory;
        public static int SelectedIndex;
        public static int ScrollOffset;
        public static ButtonInfo[] _currentCategory;
        public static string _currentCategoryName;

        public const int VISIBLE_MODS = 6;

        internal static ButtonInfo[] GetVisibleButtons(ButtonInfo[] all)
        {
#if LEGAL || LEGAL_DEBUG
            return all.Where(b => b.legal || b.label).ToArray();
#else
            return all;
#endif
        }

        public static void DoCategory(GorillaComputer instance)
        {
            if (_currentCategory == null)
                ShowCategoryList(instance);
            else
                ShowModList(instance);
        }

        private static void ShowCategoryList(GorillaComputer instance)
        {
            var main = GetVisibleButtons(Buttons.buttons[0]);
            var sb = new StringBuilder();
            sb.AppendLine($"SERALYTH  ({SelectedIndex + 1}/{main.Length})");
            sb.AppendLine();

            int end = Mathf.Min(ScrollOffset + VISIBLE_MODS, main.Length);
            for (int i = ScrollOffset; i < end; i++)
            {
                var entry = main[i];
                string prefix = (i == SelectedIndex) ? "> " : "  ";
                sb.AppendLine($"{prefix}{Truncate(entry.buttonText, 22)}  [{GetStatus(entry)}]");
            }

            sb.AppendLine();
            sb.Append("OPT2 UP | OPT3 DOWN | ENT Select | DEL Exit");
            instance.screenText.Set(sb.ToString());
        }

        private static void ShowModList(GorillaComputer instance)
        {
            var catName = _currentCategoryName ?? GetCategoryName(_currentCategory);
            if (catName == "Credits") catName = "SERALYTHREMAKE";
            var sb = new StringBuilder();
            sb.AppendLine($"{catName}  ({SelectedIndex + 1}/{_currentCategory.Length})");
            sb.AppendLine();

            int end = Mathf.Min(ScrollOffset + VISIBLE_MODS, _currentCategory.Length);
            for (int i = ScrollOffset; i < end; i++)
            {
                var mod = _currentCategory[i];
                string prefix = (i == SelectedIndex) ? "> " : "  ";
                string status = mod.isTogglable ? (mod.enabled ? "ON" : "OFF") : "";
                sb.AppendLine($"{prefix}{Truncate(mod.buttonText, 22)} {(mod.isTogglable ? "[" + status + "]" : "")}");
            }

            sb.AppendLine();
            sb.Append("OPT2 UP | OPT3 DOWN | ENT Toggle | DEL Back");
            instance.screenText.Set(sb.ToString());
        }

        private static string GetStatus(ButtonInfo entry)
        {
            if (entry.isTogglable)
                return entry.enabled ? "ON" : "OFF";

            if (Buttons.categoryNames.Contains(entry.buttonText))
            {
                int idx = Buttons.GetCategory(entry.buttonText);
                if (idx >= 0 && idx < Buttons.buttons.Length && GetVisibleButtons(Buttons.buttons[idx]).Any(b => b.isTogglable && b.enabled))
                    return "ON";
            }
            return "OFF";
        }

        private static string GetCategoryName(ButtonInfo[] cat)
        {
            for (int i = 0; i < Buttons.buttons.Length; i++)
                if (Buttons.buttons[i] == cat)
                    return Buttons.categoryNames[i];
            return "Unknown";
        }

        static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 2) + ".";
        }

        public static void Reset()
        {
            InCategory = false;
            SelectedIndex = 0;
            ScrollOffset = 0;
            _currentCategory = null;
            _currentCategoryName = null;
        }
    }

    [HarmonyPatch(typeof(GTPlayer), nameof(GTPlayer.LateUpdate))]
    public class ComputerLateUpdatePatch
    {
        public static void Postfix()
        {
            var computer = GorillaComputer.instance;
            if (computer == null) return;

            string text = computer.screenText.currentText;

            if (ComputerCategory.InCategory)
            {
                ComputerCategory.DoCategory(computer);
                return;
            }

            if (text.TrimStart().StartsWith("CREDITS"))
            {
                ComputerCategory.InCategory = true;
                ComputerCategory._currentCategory = null;
                ComputerCategory.SelectedIndex = 0;
                ComputerCategory.ScrollOffset = 0;
                ComputerCategory.DoCategory(computer);
                return;
            }

            if (text.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                computer.screenText.Set(text.Replace("Credits", "SERALYTH").Replace("CREDITS", "SERALYTH"));
            }

            string funcText = computer.functionSelectText.currentText;
            if (funcText.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                computer.functionSelectText.Set(funcText.Replace("Credits", "SERALYTH").Replace("CREDITS", "SERALYTH"));
            }
        }
    }

    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.UpdateScreen))]
    public class ComputerUpdateScreenPatch
    {
        public static void Postfix(GorillaComputer __instance)
        {
            string text = __instance.screenText.currentText;
            if (text.TrimStart().StartsWith("CREDITS"))
            {
                ComputerCategory.InCategory = true;
                ComputerCategory._currentCategory = null;
                ComputerCategory.SelectedIndex = 0;
                ComputerCategory.ScrollOffset = 0;
                ComputerCategory.DoCategory(__instance);
            }
            else if (ComputerCategory.InCategory)
            {
                ComputerCategory.DoCategory(__instance);
            }
            else
            {
                if (text.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __instance.screenText.Set(text.Replace("Credits", "SERALYTHREMAKE").Replace("CREDITS", "SERALYTHREMAKE"));
                }

                string funcText = __instance.functionSelectText.currentText;
                if (funcText.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __instance.functionSelectText.Set(funcText.Replace("Credits", "SERALYTHREMAKE").Replace("CREDITS", "SERALYTHREMAKE"));
                }
            }
        }
    }

    [HarmonyPatch(typeof(GorillaComputer), "SliceUpdate")]
    public class ComputerSliceUpdatePatch
    {
        public static void Postfix(GorillaComputer __instance)
        {
            string text = __instance.screenText.currentText;
            if (text.TrimStart().StartsWith("CREDITS"))
            {
                ComputerCategory.InCategory = true;
                ComputerCategory._currentCategory = null;
                ComputerCategory.SelectedIndex = 0;
                ComputerCategory.ScrollOffset = 0;
                ComputerCategory.DoCategory(__instance);
            }
            else if (ComputerCategory.InCategory)
            {
                ComputerCategory.DoCategory(__instance);
            }
            else
            {
                if (text.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __instance.screenText.Set(text.Replace("Credits", "SERALYTHREMAKE").Replace("CREDITS", "SERALYTHREMAKE"));
                }

                string funcText = __instance.functionSelectText.currentText;
                if (funcText.IndexOf("Credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    __instance.functionSelectText.Set(funcText.Replace("Credits", "SERALYTHREMAKE").Replace("CREDITS", "SERALYTHREMAKE"));
                }
            }
        }
    }

    [HarmonyPatch(typeof(GorillaComputer), nameof(GorillaComputer.PressButton))]
    public class ComputerPressButtonPatch
    {
        public static bool Prefix(GorillaComputer __instance, GorillaKeyboardBindings buttonPressed)
        {
            if (!ComputerCategory.InCategory)
            {
                if (buttonPressed == GorillaKeyboardBindings.option1)
                {
                    ComputerCategory.InCategory = true;
                    ComputerCategory._currentCategory = null;
                    ComputerCategory.SelectedIndex = 0;
                    ComputerCategory.ScrollOffset = 0;
                    ComputerCategory.DoCategory(__instance);
                    return false;
                }
                return true;
            }

            switch (buttonPressed)
            {
                case GorillaKeyboardBindings.up:
                case GorillaKeyboardBindings.option2:
                    var listup = ComputerCategory._currentCategory ?? ComputerCategory.GetVisibleButtons(Buttons.buttons[0]);
                    if (ComputerCategory.SelectedIndex > 0)
                    {
                        ComputerCategory.SelectedIndex--;
                        if (ComputerCategory.SelectedIndex < ComputerCategory.ScrollOffset)
                            ComputerCategory.ScrollOffset = ComputerCategory.SelectedIndex;
                    }
                    break;

                case GorillaKeyboardBindings.down:
                case GorillaKeyboardBindings.option3:
                    var listdn = ComputerCategory._currentCategory ?? ComputerCategory.GetVisibleButtons(Buttons.buttons[0]);
                    if (ComputerCategory.SelectedIndex < listdn.Length - 1)
                    {
                        ComputerCategory.SelectedIndex++;
                        if (ComputerCategory.SelectedIndex >= ComputerCategory.ScrollOffset + ComputerCategory.VISIBLE_MODS)
                            ComputerCategory.ScrollOffset = ComputerCategory.SelectedIndex - ComputerCategory.VISIBLE_MODS + 1;
                    }
                    break;

                case GorillaKeyboardBindings.enter:
                    var currentList = ComputerCategory._currentCategory ?? ComputerCategory.GetVisibleButtons(Buttons.buttons[0]);
                    var sel = currentList[ComputerCategory.SelectedIndex];
                    if (Buttons.categoryNames.Contains(sel.buttonText))
                    {
                        int idx = Buttons.GetCategory(sel.buttonText);
                        if (idx >= 0)
                        {
                            string catName = Buttons.categoryNames[idx] == "Credits" ? "SERALYTHREMAKE" : Buttons.categoryNames[idx];
                            ButtonInfo[] catButtons;
                            if (catName == "Favorite Mods")
                                catButtons = Main.StringsToInfos(Main.favorites.ToArray());
                            else if (catName == "Enabled Mods")
                                catButtons = Buttons.buttons.SelectMany(x => x).Where(b => b.enabled && b.isTogglable).ToArray();
                            else if (catName == "Achievements")
                            {
                                AchievementManager.EnterAchievementTab();
                                catButtons = Buttons.buttons[idx];
                            }
                            else if (catName == "Friends")
                            {
                                FriendManager.FriendsListUpdated();
                                catButtons = Buttons.buttons[idx];
                            }
                            else
                                catButtons = Buttons.buttons[idx];
                            catButtons = ComputerCategory.GetVisibleButtons(catButtons);
                            ComputerCategory._currentCategory = catButtons;
                            ComputerCategory._currentCategoryName = catName;
                            ComputerCategory.SelectedIndex = 0;
                            ComputerCategory.ScrollOffset = 0;
                            ComputerCategory.DoCategory(__instance);
                            break;
                        }
                    }
                    if (sel.isTogglable)
                        Main.Toggle(sel);
                    sel.method?.Invoke();
                    break;

                case GorillaKeyboardBindings.delete:
                    if (ComputerCategory._currentCategory != null)
                    {
                        ComputerCategory._currentCategory = null;
                        ComputerCategory.SelectedIndex = 0;
                        ComputerCategory.ScrollOffset = 0;
                        ComputerCategory.DoCategory(__instance);
                    }
                    else
                    {
                        ComputerCategory.Reset();
                        __instance.UpdateScreen();
                    }
                    break;

                case GorillaKeyboardBindings.zero:
                    ComputerCategory.Reset();
                    var gcType = typeof(GorillaComputer);
                    var curField = gcType.GetField("currentScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var supField = gcType.GetField("supportScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (curField != null && supField != null)
                    {
                        var supportScreen = supField.GetValue(__instance);
                        if (supportScreen != null)
                            curField.SetValue(__instance, supportScreen);
                    }
                    __instance.UpdateScreen();
                    break;
            }
            return false;
        }
    }
}
