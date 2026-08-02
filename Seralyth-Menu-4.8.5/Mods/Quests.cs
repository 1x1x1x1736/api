using Seralyth.Classes.Menu;
using Seralyth.Managers;
using Seralyth.Menu;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Seralyth.Mods
{
    public static class Quests
    {
        public static string activeQuestName;
        public static string activeQuestDare;
        public static Func<bool> activeQuestCheck;
        public static string activeHint;
        public static string activeDifficulty;
        public static int completedCount;
        public static float nextQuestTime;
        public static string lastCompletedName;
        public static string lastCompletedDifficulty;
        public static float lastCompletedTime;
        public static int lastCompletedLevel;
        public static bool lastWasLevelUp;
        public static int playerLevel = 1;
        public static int questsUntilNextLevel = 2;
        public static string selectedDifficulty = "Random";
        private static System.Random rng = new System.Random();
        private static string lastQuestName = "";
        private static int hintStep = 0;
        private static string lastQuestType = "";
        private static object[] lastQuestParams = new object[0];
        private static readonly string gorillaTagPhotosPath = @"C:\Users\kalew\OneDrive\Pictures\Gorilla Tag photos";
        private static string lastShownPhoto = "";
        public static string sharedWithPlayerId = null;
        public static string sharedWithPlayerName = null;
        public static bool isSharingQuest = false;

        private static string GetRandomGorillaTagPhoto()
        {
            if (!Directory.Exists(gorillaTagPhotosPath)) return null;
            string[] files = Directory.GetFiles(gorillaTagPhotosPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                             f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                             f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (files.Length == 0) return null;
            if (files.Length == 1) return files[0];
            string picked;
            int attempts = 0;
            do { picked = files[rng.Next(files.Length)]; attempts++; } while (picked == lastShownPhoto && attempts < 10);
            lastShownPhoto = picked;
            return picked;
        }

        private static void TakeScreenshotToFolder()
        {
            string folder = gorillaTagPhotosPath;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName = $"GT_Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(folder, fileName);

            if (Camera.main != null)
                Camera.main.Render();

            ScreenCapture.CaptureScreenshot(fullPath);

            CoroutineManager.instance.StartCoroutine(ScreenshotNotify(fullPath));
        }

        private static IEnumerator ScreenshotNotify(string path)
        {
            yield return new WaitForEndOfFrame();
            NotificationManager.SendNotification($"<color=grey>[</color><color=green>SCREENSHOT</color><color=grey>]</color> Screenshot saved to {Path.GetFileName(path)}");
        }

        private static void GenerateAIPhoto(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Please enter something for the AI to generate.");
                return;
            }

            CoroutineManager.instance.StartCoroutine(GenerateAIPhotoCoroutine(prompt));
        }

        private static IEnumerator GenerateAIPhotoCoroutine(string prompt)
        {
            string folder = gorillaTagPhotosPath;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            NotificationManager.SendNotification($"<color=green>[</color><color=green>AI</color><color=grey>]</color> Generating your photo...");

            string url = "https://image.pollinations.ai/prompt/" + Uri.EscapeDataString(prompt) + "?width=1024&height=1024&nologo=true";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Could not generate photo: {request.error}");
                yield break;
            }

            string fileName = $"GT_AI_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string fullPath = Path.Combine(folder, fileName);
            File.WriteAllBytes(fullPath, request.downloadHandler.data);

            NotificationManager.SendNotification($"<color=green>[</color><color=green>AI</color><color=grey>]</color> Generated photo saved as {fileName}");
        }

        private static string[] maps = { "Forest", "Cave", "Beach", "Canyon", "Mountain", "City", "Clouds", "Basement", "Metropolis", "Bayou" };

        private static string[] adj = { "Epic", "Wild", "Crazy", "Brave", "Sneaky", "Swift", "Bold", "Chill", "Lucky", "Daring", "Fierce", "Quick", "Sly", "Nimble", "Tough", "Frosty", "Savage", "Wicked", "Turbo", "Mega", "Ultra", "Shadow", "Phantom", "Blazing", "Toxic", "Insane" };

        private static string[] title = { "Explorer", "Champion", "Legend", "Ninja", "Pirate", "Wizard", "Warrior", "Hunter", "Prowler", "Scout", "Pathfinder", "Challenger", "Daredevil", "Wanderer", "Stalker", "Lurker", "Survivor", "Nomad", "Rebel", "Ghost", "Dragon", "Titan", "Viking", "Samurai", "Knight" };

        private static ButtonInfo[] GetAllMods()
        {
            return Menu.Buttons.buttons.SelectMany(b => b).Where(b => b.isTogglable && !b.label).ToArray();
        }

        // --- DIFFICULTY ---
        private enum Diff { Easy, Medium, Hard }
        private static Diff GetDiff()
        {
            if (selectedDifficulty == "Easy") return Diff.Easy;
            if (selectedDifficulty == "Medium") return Diff.Medium;
            if (selectedDifficulty == "Hard") return Diff.Hard;
            int r = rng.Next(3);
            return r == 0 ? Diff.Easy : r == 1 ? Diff.Medium : Diff.Hard;
        }

        private static string DiffLabel(Diff d)
        {
            if (d == Diff.Easy) return "<color=green>EASY</color>";
            if (d == Diff.Medium) return "<color=yellow>MEDIUM</color>";
            return "<color=red>HARD</color>";
        }

        private static int DiffHeight(Diff d)
        {
            if (d == Diff.Easy) { int[] h = { 3, 5, 8 }; return h[rng.Next(h.Length)]; }
            if (d == Diff.Medium) { int[] h = { 10, 12, 15, 18 }; return h[rng.Next(h.Length)]; }
            int[] hh = { 20, 25, 30, 35, 40 }; return hh[rng.Next(hh.Length)];
        }

        private static float DiffDepth(Diff d)
        {
            if (d == Diff.Easy) return -2f;
            if (d == Diff.Medium) return -4f;
            return -6f;
        }

        private static float DiffCloseDist(Diff d)
        {
            if (d == Diff.Easy) return Mathf.Round((float)(rng.NextDouble() * 1.5 + 1.5) * 10f) / 10f;
            if (d == Diff.Medium) return Mathf.Round((float)(rng.NextDouble() * 1.5 + 0.8) * 10f) / 10f;
            return Mathf.Round((float)(rng.NextDouble() * 0.5 + 0.3) * 10f) / 10f;
        }

        private static float DiffFarDist(Diff d)
        {
            if (d == Diff.Easy) return 8f;
            if (d == Diff.Medium) return 12f;
            return 18f;
        }

        private static int DiffPlayers(Diff d)
        {
            if (d == Diff.Easy) return rng.Next(2, 5);
            if (d == Diff.Medium) return rng.Next(4, 8);
            return rng.Next(7, 12);
        }

        private static int DiffCrowdPlayers(Diff d)
        {
            if (d == Diff.Easy) return rng.Next(5, 8);
            if (d == Diff.Medium) return rng.Next(8, 11);
            return rng.Next(10, 16);
        }

        private static float DiffHoldTime(Diff d)
        {
            if (d == Diff.Easy) return 3f;
            if (d == Diff.Medium) return 5f;
            return 8f;
        }

        private static int DiffModCount(Diff d, int max)
        {
            if (d == Diff.Easy) return rng.Next(1, Math.Min(4, max + 1));
            if (d == Diff.Medium) return rng.Next(3, Math.Min(7, max + 1));
            return rng.Next(6, Math.Min(max + 1, 15));
        }

        public static void GenerateQuest()
        {
            string chosen = "";
            int attempts = 0;
            while (chosen == "" || chosen == lastQuestName)
            {
                chosen = GenerateRandomQuest();
                attempts++;
                if (attempts > 30) break;
            }
            lastQuestName = chosen;
            nextQuestTime = 0f;
            hintStep = 0;
            lastCompletedName = null;
            UpdateVRButtons();
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=yellow>NEW {activeDifficulty} QUEST</color><color=grey>]</color> <color=green>{activeQuestName}</color> - {activeQuestDare}");
            SendQuestSync();
        }

        private static string GenerateRandomQuest()
        {
            Diff diff = GetDiff();
            string name = rng.Next(3) == 0 ? $"{adj[rng.Next(adj.Length)]} {title[rng.Next(title.Length)]}" : title[rng.Next(title.Length)];
            var mods = GetAllMods();
            bool hasMods = mods.Length > 0;

            bool useMod = hasMods && rng.Next(2) == 0;

            if (useMod)
                return ModPlusGT(name, diff, mods);
            else
                return PureGT(name, diff);
        }

        private static string PureGT(string name, Diff diff)
        {
            int questType = rng.Next(12);
            switch (questType)
            {
                case 0: return Q_GoToMap(name, diff);
                case 1: return Q_Climb(name, diff);
                case 2: return Q_GetLow(name, diff);
                case 3: return Q_NearPlayer(name, diff);
                case 4: return Q_FarPlayer(name, diff);
                case 5: return Q_JoinRoom(name, diff);
                case 6: return Q_TwoMaps(name, diff);
                case 7: return Q_SteadyHeight(name, diff);
                case 8: return Q_VeryClose(name, diff);
                case 9: return Q_Crowded(name, diff);
                case 10: return Q_MapClimb(name, diff);
                case 11: return Q_MapExplore(name, diff);
                default: return Q_GoToMap(name, diff);
            }
        }

        private static string ModPlusGT(string name, Diff diff, ButtonInfo[] mods)
        {
            ButtonInfo mod = mods[rng.Next(mods.Length)];
            int questType = rng.Next(10);
            switch (questType)
            {
                case 0: return Q_ModGoToMap(name, diff, mod);
                case 1: return Q_ModClimb(name, diff, mod);
                case 2: return Q_ModNear(name, diff, mod);
                case 3: return Q_ModMapClimb(name, diff, mod);
                case 4: return Q_TwoMods(name, diff, mod);
                case 5: return Q_ModCount(name, diff, mod);
                case 6: return Q_ModSteady(name, diff, mod);
                case 7: return Q_ModExplore(name, diff, mod);
                case 8: return Q_ModCombo(name, diff, mod);
                case 9: return Q_ModCrowded(name, diff, mod);
                default: return Q_ModGoToMap(name, diff, mod);
            }
        }

        // --- PURE GT ---
        private static string Q_GoToMap(string name, Diff diff)
        {
            string map = PickMap();
            activeQuestName = name;
            activeQuestDare = $"Go to the {map} map.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Look for the portal or tunnel that leads to {map}.";
            activeQuestCheck = () => IsInMap(map);
            lastQuestType = "gt_map";
            lastQuestParams = new object[] { map };
            return "gt_map";
        }

        private static string Q_Climb(string name, Diff diff)
        {
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Climb above {h}m.";
            activeDifficulty = DiffLabel(diff);
            activeHint = h > 20 ? "Hint: Try scaling the tallest structures or trees." : "Hint: Climb any surface to gain height.";
            activeQuestCheck = () => VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "gt_climb";
            lastQuestParams = new object[] { h };
            return "gt_climb";
        }

        private static string Q_GetLow(string name, Diff diff)
        {
            float depth = DiffDepth(diff);
            activeQuestName = name;
            activeQuestDare = $"Get below {depth}m.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Go underground or into caves and get low.";
            activeQuestCheck = () => VRRig.LocalRig.headMesh.transform.position.y < depth;
            lastQuestType = "gt_low";
            lastQuestParams = new object[] { depth };
            return "gt_low";
        }

        private static string Q_NearPlayer(string name, Diff diff)
        {
            float dist = DiffCloseDist(diff);
            activeQuestName = name;
            activeQuestDare = $"Get within {dist}m of another player.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Walk up to someone and get close to them.";
            activeQuestCheck = () => { float d = GetClosestDist(); return d > 0f && d < dist; };
            lastQuestType = "gt_near";
            lastQuestParams = new object[] { dist };
            return "gt_near";
        }

        private static string Q_FarPlayer(string name, Diff diff)
        {
            float dist = DiffFarDist(diff);
            activeQuestName = name;
            activeQuestDare = $"Stay {dist}m+ from all players.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Find a quiet corner away from everyone.";
            activeQuestCheck = () => { float d = GetClosestDist(); return d > dist || d == 0f; };
            lastQuestType = "gt_far";
            lastQuestParams = new object[] { dist };
            return "gt_far";
        }

        private static string Q_JoinRoom(string name, Diff diff)
        {
            int players = DiffPlayers(diff);
            activeQuestName = name;
            activeQuestDare = $"Join a room with {players}+ players.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Join a public lobby or one with friends.";
            activeQuestCheck = () => Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= players;
            lastQuestType = "gt_join";
            lastQuestParams = new object[] { players };
            return "gt_join";
        }

        private static string Q_TwoMaps(string name, Diff diff)
        {
            string m1 = PickMap(), m2 = PickMap();
            while (m2 == m1) m2 = PickMap();
            activeQuestName = name;
            activeQuestDare = $"Visit {m1} then {m2}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Go to {m1} first, then travel to {m2}.";
            bool hit1 = false;
            activeQuestCheck = () =>
            {
                if (!hit1 && IsInMap(m1)) hit1 = true;
                return hit1 && IsInMap(m2);
            };
            lastQuestType = "gt_twomap";
            lastQuestParams = new object[] { m1, m2 };
            return "gt_twomap";
        }

        private static string Q_SteadyHeight(string name, Diff diff)
        {
            int h = DiffHeight(diff);
            float hold = DiffHoldTime(diff);
            activeQuestName = name;
            activeQuestDare = $"Stay above {h}m for {hold}s.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Climb up and hold your position.";
            float timer = 0f;
            activeQuestCheck = () =>
            {
                if (VRRig.LocalRig.headMesh.transform.position.y > h) { timer += Time.deltaTime; return timer >= hold; }
                timer = 0f;
                return false;
            };
            lastQuestType = "gt_steady";
            lastQuestParams = new object[] { h, hold };
            return "gt_steady";
        }

        private static string Q_VeryClose(string name, Diff diff)
        {
            float dist = diff == Diff.Hard ? 0.3f : diff == Diff.Medium ? 0.5f : 0.8f;
            activeQuestName = name;
            activeQuestDare = $"Get within {dist}m of another player.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Walk right up to someone.";
            activeQuestCheck = () => { float d = GetClosestDist(); return d > 0f && d < dist; };
            lastQuestType = "gt_veryclose";
            lastQuestParams = new object[] { dist };
            return "gt_veryclose";
        }

        private static string Q_Crowded(string name, Diff diff)
        {
            int players = DiffCrowdPlayers(diff);
            activeQuestName = name;
            activeQuestDare = $"Find a room with {players}+ players.";
            activeDifficulty = DiffLabel(diff);
            activeHint = "Hint: Try joining a popular public lobby.";
            activeQuestCheck = () => Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= players;
            lastQuestType = "gt_crowded";
            lastQuestParams = new object[] { players };
            return "gt_crowded";
        }

        private static string Q_MapClimb(string name, Diff diff)
        {
            string map = PickMap();
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Go to {map} and climb {h}m+.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Travel to {map} then start climbing.";
            activeQuestCheck = () => IsInMap(map) && VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "gt_mapclimb";
            lastQuestParams = new object[] { map, h };
            return "gt_mapclimb";
        }

        private static string Q_MapExplore(string name, Diff diff)
        {
            string map = PickMap();
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Explore {map} and climb {h}m+.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Head to {map} and explore upward.";
            activeQuestCheck = () => IsInMap(map) && VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "gt_explore";
            lastQuestParams = new object[] { map, h };
            return "gt_explore";
        }

        // --- MOD + GT ---
        private static string Q_ModGoToMap(string name, Diff diff, ButtonInfo mod)
        {
            string map = PickMap();
            activeQuestName = name;
            activeQuestDare = $"Go to {map} with {mod.buttonText} enabled.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable {mod.buttonText} first, then go to {map}.";
            activeQuestCheck = () => IsInMap(map) && mod.enabled;
            lastQuestType = "mgt_map";
            lastQuestParams = new object[] { map, mod.buttonText };
            return "mgt_map";
        }

        private static string Q_ModClimb(string name, Diff diff, ButtonInfo mod)
        {
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Climb above {h}m with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Turn on {mod.buttonText} then climb up.";
            activeQuestCheck = () => mod.enabled && VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "mgt_climb";
            lastQuestParams = new object[] { h, mod.buttonText };
            return "mgt_climb";
        }

        private static string Q_ModNear(string name, Diff diff, ButtonInfo mod)
        {
            float dist = DiffCloseDist(diff);
            activeQuestName = name;
            activeQuestDare = $"Get within {dist}m of someone with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable {mod.buttonText} and approach a player.";
            activeQuestCheck = () => mod.enabled && GetClosestDist() < dist && GetClosestDist() > 0f;
            lastQuestType = "mgt_near";
            lastQuestParams = new object[] { dist, mod.buttonText };
            return "mgt_near";
        }

        private static string Q_ModMapClimb(string name, Diff diff, ButtonInfo mod)
        {
            string map = PickMap();
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Go to {map} and climb {h}m+ with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable {mod.buttonText}, go to {map}, then climb.";
            activeQuestCheck = () => IsInMap(map) && mod.enabled && VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "mgt_mapclimb";
            lastQuestParams = new object[] { map, h, mod.buttonText };
            return "mgt_mapclimb";
        }

        private static string Q_TwoMods(string name, Diff diff, ButtonInfo mod)
        {
            var mods = GetAllMods();
            var m2 = mods[rng.Next(mods.Length)];
            int safety = 0;
            while (m2 == mod && safety < 10) { m2 = mods[rng.Next(mods.Length)]; safety++; }
            if (m2 == mod) { return Q_ModGoToMap(name, diff, mod); }

            int challenge = rng.Next(3);
            switch (challenge)
            {
                case 0:
                    string map = PickMap();
                    activeQuestName = name;
                    activeQuestDare = $"Go to {map} with {mod.buttonText} and {m2.buttonText}.";
                    activeDifficulty = DiffLabel(diff);
                    activeHint = $"Hint: Enable both {mod.buttonText} and {m2.buttonText}, then go to {map}.";
                    activeQuestCheck = () => IsInMap(map) && mod.enabled && m2.enabled;
                    lastQuestType = "mgt_two_map";
                    lastQuestParams = new object[] { map, mod.buttonText, m2.buttonText };
                    break;
                case 1:
                    int h = DiffHeight(diff);
                    activeQuestName = name;
                    activeQuestDare = $"Climb {h}m+ with {mod.buttonText} and {m2.buttonText}.";
                    activeDifficulty = DiffLabel(diff);
                    activeHint = $"Hint: Turn on both mods then climb high.";
                    activeQuestCheck = () => mod.enabled && m2.enabled && VRRig.LocalRig.headMesh.transform.position.y > h;
                    lastQuestType = "mgt_two_climb";
                    lastQuestParams = new object[] { h, mod.buttonText, m2.buttonText };
                    break;
                case 2:
                    float dist = DiffCloseDist(diff);
                    activeQuestName = name;
                    activeQuestDare = $"Get within {dist}m with {mod.buttonText} and {m2.buttonText}.";
                    activeDifficulty = DiffLabel(diff);
                    activeHint = $"Hint: Enable both mods and get close to a player.";
                    activeQuestCheck = () => mod.enabled && m2.enabled && GetClosestDist() < dist && GetClosestDist() > 0f;
                    lastQuestType = "mgt_two_near";
                    lastQuestParams = new object[] { dist, mod.buttonText, m2.buttonText };
                    break;
            }
            return "mgt_two";
        }

        private static string Q_ModCount(string name, Diff diff, ButtonInfo mod)
        {
            var mods = GetAllMods();
            int count = DiffModCount(diff, mods.Length);
            activeQuestName = name;
            activeQuestDare = $"Enable {count} mods at once.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Turn on {count} different mods from any category.";
            activeQuestCheck = () => mods.Count(b => b.enabled) >= count;
            lastQuestType = "mgt_count";
            lastQuestParams = new object[] { count };
            return "mgt_count";
        }

        private static string Q_ModSteady(string name, Diff diff, ButtonInfo mod)
        {
            int h = DiffHeight(diff);
            float hold = DiffHoldTime(diff);
            activeQuestName = name;
            activeQuestDare = $"Stay above {h}m for {hold}s with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable {mod.buttonText}, climb up, and hold position.";
            float timer = 0f;
            activeQuestCheck = () =>
            {
                if (mod.enabled && VRRig.LocalRig.headMesh.transform.position.y > h) { timer += Time.deltaTime; return timer >= hold; }
                timer = 0f;
                return false;
            };
            lastQuestType = "mgt_steady";
            lastQuestParams = new object[] { h, hold, mod.buttonText };
            return "mgt_steady";
        }

        private static string Q_ModExplore(string name, Diff diff, ButtonInfo mod)
        {
            string map = PickMap();
            int h = DiffHeight(diff);
            activeQuestName = name;
            activeQuestDare = $"Explore {map} and climb {h}m+ with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Turn on {mod.buttonText} and explore {map}.";
            activeQuestCheck = () => IsInMap(map) && mod.enabled && VRRig.LocalRig.headMesh.transform.position.y > h;
            lastQuestType = "mgt_explore";
            lastQuestParams = new object[] { map, h, mod.buttonText };
            return "mgt_explore";
        }

        private static string Q_ModCombo(string name, Diff diff, ButtonInfo mod)
        {
            var mods = GetAllMods();
            var m2 = mods[rng.Next(mods.Length)];
            int safety = 0;
            while (m2 == mod && safety < 10) { m2 = mods[rng.Next(mods.Length)]; safety++; }
            if (m2 == mod) { return Q_ModGoToMap(name, diff, mod); }

            string map = PickMap();
            float dist = DiffCloseDist(diff);
            activeQuestName = name;
            activeQuestDare = $"Go to {map}, get within {dist}m with {mod.buttonText} and {m2.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable both mods, go to {map}, approach a player.";
            activeQuestCheck = () => IsInMap(map) && mod.enabled && m2.enabled && GetClosestDist() < dist && GetClosestDist() > 0f;
            lastQuestType = "mgt_combo";
            lastQuestParams = new object[] { map, dist, mod.buttonText, m2.buttonText };
            return "mgt_combo";
        }

        private static string Q_ModCrowded(string name, Diff diff, ButtonInfo mod)
        {
            int players = DiffCrowdPlayers(diff);
            activeQuestName = name;
            activeQuestDare = $"Find {players}+ players with {mod.buttonText}.";
            activeDifficulty = DiffLabel(diff);
            activeHint = $"Hint: Enable {mod.buttonText} and join a busy lobby.";
            activeQuestCheck = () => mod.enabled && Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= players;
            lastQuestType = "mgt_crowded";
            lastQuestParams = new object[] { players, mod.buttonText };
            return "mgt_crowded";
        }

        // --- HELPERS ---
        private static string PickMap() { return maps[rng.Next(maps.Length)]; }

        public static bool IsInMap(string mapName)
        {
            if (!Photon.Pun.PhotonNetwork.InRoom) return false;
            string zone = VRRig.LocalRig.zoneEntity.currentZone.ToString();
            if (zone.IndexOf(mapName, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            string roomName = Photon.Pun.PhotonNetwork.CurrentRoom.Name ?? "";
            return roomName.IndexOf(mapName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float GetClosestDist()
        {
            float closest = float.MaxValue;
            Vector3 myPos = VRRig.LocalRig.headMesh.transform.position;
            foreach (var rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig == VRRig.LocalRig) continue;
                float dist = Vector3.Distance(myPos, rig.headMesh.transform.position);
                if (dist < closest) closest = dist;
            }
            return closest == float.MaxValue ? 0f : closest;
        }

        public static void SendQuestSync()
        {
            if (activeQuestName == null || lastQuestType == "") return;
            FriendManager.ExecuteCommand("quest", new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                activeQuestName, activeQuestDare ?? "", activeDifficulty ?? "",
                activeHint ?? "", lastQuestType, lastQuestParams);
        }

        public static void ReceiveQuestSync(object[] data)
        {
            if (data.Length < 6) return;
            string name = (string)data[0];
            string dare = (string)data[1];
            string difficulty = (string)data[2];
            string hint = (string)data[3];
            string questType = (string)data[4];
            object[] p = (object[])data[5];

            activeQuestName = name;
            activeQuestDare = dare;
            activeDifficulty = difficulty;
            activeHint = hint;
            lastQuestType = questType;
            lastQuestParams = p;
            hintStep = 0;

            Func<bool> check = RebuildCheck(questType, p);
            activeQuestCheck = check;
            nextQuestTime = 0f;
            lastCompletedName = null;
            UpdateVRButtons();
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=yellow>SHARED QUEST</color><color=grey>]</color> <color=green>{name}</color> - {dare}");
        }

        private static Func<bool> RebuildCheck(string type, object[] p)
        {
            switch (type)
            {
                case "gt_map":
                    return () => IsInMap((string)p[0]);
                case "gt_climb":
                    return () => VRRig.LocalRig.headMesh.transform.position.y > Convert.ToSingle(p[0]);
                case "gt_low":
                    return () => VRRig.LocalRig.headMesh.transform.position.y < Convert.ToSingle(p[0]);
                case "gt_near":
                    { float d2 = Convert.ToSingle(p[0]); return () => { float d = GetClosestDist(); return d > 0f && d < d2; }; }
                case "gt_far":
                    { float d3 = Convert.ToSingle(p[0]); return () => { float d = GetClosestDist(); return d > d3 || d == 0f; }; }
                case "gt_join":
                    { int minP = Convert.ToInt32(p[0]); return () => Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= minP; }
                case "gt_twomap":
                    { string m1 = (string)p[0], m2 = (string)p[1]; bool hit = false; return () => { if (!hit && IsInMap(m1)) hit = true; return hit && IsInMap(m2); }; }
                case "gt_steady":
                    { int h = Convert.ToInt32(p[0]); float hold = Convert.ToSingle(p[1]); float t = 0f; return () => { if (VRRig.LocalRig.headMesh.transform.position.y > h) { t += Time.deltaTime; return t >= hold; } t = 0f; return false; }; }
                case "gt_veryclose":
                    { float d4 = Convert.ToSingle(p[0]); return () => { float d = GetClosestDist(); return d > 0f && d < d4; }; }
                case "gt_crowded":
                    { int minP2 = Convert.ToInt32(p[0]); return () => Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= minP2; }
                case "gt_mapclimb":
                    { string mp = (string)p[0]; int h2 = Convert.ToInt32(p[1]); return () => IsInMap(mp) && VRRig.LocalRig.headMesh.transform.position.y > h2; }
                case "gt_explore":
                    { string mp2 = (string)p[0]; int h3 = Convert.ToInt32(p[1]); return () => IsInMap(mp2) && VRRig.LocalRig.headMesh.transform.position.y > h3; }
                case "mgt_map":
                    { string mp3 = (string)p[0]; string mn = (string)p[1]; ButtonInfo md = FindMod(mn); return () => IsInMap(mp3) && (md == null || md.enabled); }
                case "mgt_climb":
                    { int h4 = Convert.ToInt32(p[0]); string mn2 = (string)p[1]; ButtonInfo md2 = FindMod(mn2); return () => (md2 == null || md2.enabled) && VRRig.LocalRig.headMesh.transform.position.y > h4; }
                case "mgt_near":
                    { float d5 = Convert.ToSingle(p[0]); string mn3 = (string)p[1]; ButtonInfo md3 = FindMod(mn3); return () => (md3 == null || md3.enabled) && GetClosestDist() < d5 && GetClosestDist() > 0f; }
                case "mgt_mapclimb":
                    { string mp4 = (string)p[0]; int h5 = Convert.ToInt32(p[1]); string mn4 = (string)p[2]; ButtonInfo md4 = FindMod(mn4); return () => IsInMap(mp4) && (md4 == null || md4.enabled) && VRRig.LocalRig.headMesh.transform.position.y > h5; }
                case "mgt_two_map":
                    { string mp5 = (string)p[0]; string mn5 = (string)p[1]; string mn6 = (string)p[2]; ButtonInfo md5 = FindMod(mn5); ButtonInfo md6 = FindMod(mn6); return () => IsInMap(mp5) && (md5 == null || md5.enabled) && (md6 == null || md6.enabled); }
                case "mgt_two_climb":
                    { int h6 = Convert.ToInt32(p[0]); string mn7 = (string)p[1]; string mn8 = (string)p[2]; ButtonInfo md7 = FindMod(mn7); ButtonInfo md8 = FindMod(mn8); return () => (md7 == null || md7.enabled) && (md8 == null || md8.enabled) && VRRig.LocalRig.headMesh.transform.position.y > h6; }
                case "mgt_two_near":
                    { float d6 = Convert.ToSingle(p[0]); string mn9 = (string)p[1]; string mn10 = (string)p[2]; ButtonInfo md9 = FindMod(mn9); ButtonInfo md10 = FindMod(mn10); return () => (md9 == null || md9.enabled) && (md10 == null || md10.enabled) && GetClosestDist() < d6 && GetClosestDist() > 0f; }
                case "mgt_count":
                    { int cnt = Convert.ToInt32(p[0]); return () => GetAllMods().Count(b => b.enabled) >= cnt; }
                case "mgt_steady":
                    { int h7 = Convert.ToInt32(p[0]); float hold2 = Convert.ToSingle(p[1]); string mn11 = (string)p[2]; ButtonInfo md11 = FindMod(mn11); float t2 = 0f; return () => { if ((md11 == null || md11.enabled) && VRRig.LocalRig.headMesh.transform.position.y > h7) { t2 += Time.deltaTime; return t2 >= hold2; } t2 = 0f; return false; }; }
                case "mgt_explore":
                    { string mp6 = (string)p[0]; int h8 = Convert.ToInt32(p[1]); string mn12 = (string)p[2]; ButtonInfo md12 = FindMod(mn12); return () => IsInMap(mp6) && (md12 == null || md12.enabled) && VRRig.LocalRig.headMesh.transform.position.y > h8; }
                case "mgt_combo":
                    { string mp7 = (string)p[0]; float d7 = Convert.ToSingle(p[1]); string mn13 = (string)p[2]; string mn14 = (string)p[3]; ButtonInfo md13 = FindMod(mn13); ButtonInfo md14 = FindMod(mn14); return () => IsInMap(mp7) && (md13 == null || md13.enabled) && (md14 == null || md14.enabled) && GetClosestDist() < d7 && GetClosestDist() > 0f; }
                case "mgt_crowded":
                    { int minP3 = Convert.ToInt32(p[0]); string mn15 = (string)p[1]; ButtonInfo md15 = FindMod(mn15); return () => (md15 == null || md15.enabled) && Photon.Pun.PhotonNetwork.InRoom && Photon.Pun.PhotonNetwork.PlayerListOthers.Length >= minP3; }
                default:
                    return () => false;
            }
        }

        private static ButtonInfo FindMod(string modName)
        {
            return GetAllMods().FirstOrDefault(b => b.buttonText == modName);
        }

        public static void ShareQuestWith(string targetUserId, string targetName)
        {
            if (activeQuestName == null || lastQuestType == "")
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> You need an active quest to share!");
                return;
            }
            FriendManager.ExecuteCommand("sharequest", new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                targetUserId, Photon.Pun.PhotonNetwork.NickName, activeQuestName, activeQuestDare ?? "",
                activeDifficulty ?? "", activeHint ?? "", lastQuestType, lastQuestParams);
            NotificationManager.SendNotification($"<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> Quest share request sent to <color=green>{targetName}</color>!");
        }

        public static void AcceptShareQuest(string senderName, string senderId, object[] questData)
        {
            string name = (string)questData[0];
            string dare = (string)questData[1];
            string difficulty = (string)questData[2];
            string hint = (string)questData[3];
            string questType = (string)questData[4];
            object[] p = (object[])questData[5];

            activeQuestName = name;
            activeQuestDare = dare;
            activeDifficulty = difficulty;
            activeHint = hint;
            lastQuestType = questType;
            lastQuestParams = p;
            hintStep = 0;
            activeQuestCheck = RebuildCheck(questType, p);
            nextQuestTime = 0f;
            lastCompletedName = null;
            isSharingQuest = true;
            sharedWithPlayerId = senderId;
            sharedWithPlayerName = senderName;
            UpdateVRButtons();
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=yellow>SHARED QUEST</color><color=grey>]</color> You're now doing <color=green>{name}</color> with <color=green>{senderName}</color>!");
        }

        public static void StopSharingQuest()
        {
            if (!isSharingQuest) return;
            FriendManager.ExecuteCommand("stopsharequest", new RaiseEventOptions { Receivers = ReceiverGroup.Others }, sharedWithPlayerId);
            isSharingQuest = false;
            sharedWithPlayerId = null;
            sharedWithPlayerName = null;
            UpdateVRButtons();
            NotificationManager.SendNotification("<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> Stopped sharing quest.");
        }

        public static void HandleQuestCompletedByFriend(string friendName)
        {
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> <color=green>{friendName}</color> completed the shared quest!");
        }

        public static void HandleStopShareQuest()
        {
            isSharingQuest = false;
            sharedWithPlayerId = null;
            sharedWithPlayerName = null;
            UpdateVRButtons();
            NotificationManager.SendNotification("<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> Your friend stopped sharing quests.");
        }

        public static void ShowQuestDetails()
        {
            string questText = $"{activeDifficulty} <color=yellow>{activeQuestName}</color>\n\n{activeQuestDare}\n\n<color=grey>Hint: {activeHint}</color>";
            string photo = GetRandomGorillaTagPhoto();

            if (photo != null)
            {
                string localUrl = "file:///" + photo.Replace("\\", "/");
                Main.PromptSingle($"{questText}\n\n<{localUrl}>", null, "Ok");
            }
            else
            {
                Main.Prompt("Looks like you don't have a Gorilla Tag photo. Do you want to take one?",
                    () => Main.Prompt("How would you like to get your photo?",
                        () => TakeScreenshotToFolder(),
                        () => Main.PromptSingleText("What would you like the AI to create? Type a description and it will generate your photo.", () => GenerateAIPhoto(Main.keyboardInput), "Generate"),
                        "Take a Picture", "AI Create"),
                    null, "Yes", "No");
            }
        }

        public static void CheckQuests()
        {
            float now = Time.time;

            if (activeQuestCheck == null)
            {
                if (now >= nextQuestTime)
                    GenerateQuest();
                return;
            }

            if (activeQuestCheck())
            {
                completedCount++;
                questsUntilNextLevel--;
                lastCompletedName = activeQuestName;
                lastCompletedDifficulty = activeDifficulty;
                lastCompletedTime = now;
                lastCompletedLevel = playerLevel;
                lastWasLevelUp = false;
                if (questsUntilNextLevel <= 0)
                {
                    playerLevel++;
                    questsUntilNextLevel = 2;
                    lastWasLevelUp = true;
                    NotificationManager.SendNotification(
                        $"<color=grey>[</color><color=green>LEVEL UP</color><color=grey>]</color> You are now <color=green>Lvl {playerLevel}</color>!");
                }
                NotificationManager.SendNotification(
                    $"<color=grey>[</color><color=green>QUEST COMPLETE</color><color=grey>]</color> {activeDifficulty} <color=yellow>{activeQuestName}</color>! ({questsUntilNextLevel} more to lvl {playerLevel + 1})");
                VRRig.LocalRig.PlayHandTapLocal(50, true, 0.6f);
                VRRig.LocalRig.PlayHandTapLocal(50, false, 0.6f);
                if (isSharingQuest && sharedWithPlayerId != null)
                {
                    FriendManager.ExecuteCommand("questcompleted", new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                        sharedWithPlayerId, Photon.Pun.PhotonNetwork.NickName);
                }
                activeQuestCheck = null;
                activeQuestName = null;
                activeQuestDare = null;
                activeHint = null;
                activeDifficulty = null;
                hintStep = 0;
                isSharingQuest = false;
                sharedWithPlayerId = null;
                sharedWithPlayerName = null;
                nextQuestTime = now + 60f;
                UpdateVRButtons();
            }
        }

        public static void GiveHint()
        {
            if (activeHint == null) return;
            NotificationManager.SendNotification(
                $"<color=grey>[</color><color=green>HINT</color><color=grey>]</color> <color=white>{activeHint}</color>");
        }

        private static string[] difficultyOrder = { "Random", "Easy", "Medium", "Hard" };
        private static string[] difficultyColors = { "white", "green", "yellow", "red" };

        public static void ChangeDifficulty(bool forward = true)
        {
            int idx = Array.IndexOf(difficultyOrder, selectedDifficulty);
            if (idx < 0) idx = 0;
            idx = forward ? (idx + 1) % difficultyOrder.Length : (idx - 1 + difficultyOrder.Length) % difficultyOrder.Length;
            selectedDifficulty = difficultyOrder[idx];
            Menu.Buttons.GetIndex("Change Quest Difficulty").overlapText = $"Change Quest Difficulty <color=grey>[</color><color={difficultyColors[idx]}>{selectedDifficulty}</color><color=grey>]</color>";
            NotificationManager.SendNotification($"<color=grey>[</color><color=yellow>DIFF</color><color=grey>]</color> Difficulty set to <color={difficultyColors[idx]}>{selectedDifficulty}</color>");
        }

        public static void ResetAllQuests()
        {
            activeQuestCheck = null;
            activeQuestName = null;
            activeQuestDare = null;
            activeHint = null;
            activeDifficulty = null;
            hintStep = 0;
            completedCount = 0;
            playerLevel = 1;
            questsUntilNextLevel = 2;
            nextQuestTime = Time.time + 60f;
            lastCompletedName = null;
            UpdateVRButtons();
            NotificationManager.SendNotification("<color=grey>[</color><color=yellow>QUEST</color><color=grey>]</color> Reset! Level back to 1. New quest in 60 seconds.");
        }

        public static void UpdateVRButtons()
        {
            int idx = Menu.Buttons.GetCategory("Quest Mods");
            if (idx < 0) return;

            var list = new List<ButtonInfo>();
            list.Add(new ButtonInfo { buttonText = "Exit Quests", method = () => Menu.Buttons.CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true });

            list.Add(new ButtonInfo { buttonText = $"Level: {playerLevel}", overlapText = $"Level <color=grey>[</color><color=green>{playerLevel}</color><color=grey>]</color> ({2 - questsUntilNextLevel}/2)", isTogglable = false, toolTip = "Your quest level goes up every 2 completions. Infinite levels!", legal = true });

            // difficulty button
            string diffColor = selectedDifficulty == "Random" ? "white" : selectedDifficulty == "Easy" ? "green" : selectedDifficulty == "Medium" ? "yellow" : "red";
            list.Add(new ButtonInfo { buttonText = "Change Quest Difficulty", overlapText = $"Change Quest Difficulty <color=grey>[</color><color={diffColor}>{selectedDifficulty}</color><color=grey>]</color>", method =() => ChangeDifficulty(), enableMethod =() => ChangeDifficulty(), disableMethod =() => ChangeDifficulty(false), incremental = true, isTogglable = false, toolTip = "Changes the quest difficulty.", legal = true });

            if (activeQuestCheck != null)
            {
                list.Add(new ButtonInfo { buttonText = "ActiveQuest", overlapText = $"{activeDifficulty} {activeQuestName}", isTogglable = false, toolTip = activeQuestDare, legal = true });
                list.Add(new ButtonInfo { buttonText = "Quest Details", method = ShowQuestDetails, isTogglable = false, toolTip = "View quest rules with a Gorilla Tag photo.", legal = true });
                list.Add(new ButtonInfo { buttonText = "Get Hint", method = GiveHint, isTogglable = false, toolTip = "Get a hint about the current quest.", legal = true });
            }
            else
            {
                float remaining = Mathf.Max(0, Mathf.CeilToInt(nextQuestTime - Time.time));
                list.Add(new ButtonInfo { buttonText = "Next Quest...", isTogglable = false, toolTip = $"New quest in {remaining}s", legal = true });
                if (lastCompletedName != null)
                {
                    string lvlMsg = lastWasLevelUp ? $" <color=green>(LEVEL UP to {lastCompletedLevel + 1}!)</color>" : $" ({2 - questsUntilNextLevel}/2 to lvl {playerLevel + 1})";
                    list.Add(new ButtonInfo { buttonText = $"Last: {lastCompletedDifficulty} {lastCompletedName}{lvlMsg}", overlapText = $"<color=green>QUEST COMPLETE</color> {lastCompletedDifficulty} <color=yellow>{lastCompletedName}</color>{lvlMsg}", isTogglable = false, toolTip = "The quest you just completed!", legal = true });
                }
            }
            list.Add(new ButtonInfo { buttonText = $"Completed: {completedCount}", overlapText = $"Completed <color=grey>[</color><color=green>{completedCount}</color><color=grey>]</color>", isTogglable = false, toolTip = "Total quests completed.", legal = true });
            list.Add(new ButtonInfo { buttonText = "Reset Quests", method = ResetAllQuests, isTogglable = false, toolTip = "Resets level, progress, and starts a new quest in 60 seconds.", legal = true });

            Menu.Buttons.buttons[idx] = list.ToArray();
        }
    }
}
