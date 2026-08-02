/*
 * Seralyth Menu  Mods/Safety.cs
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

using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTagScripts;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Patches.Menu;
using Seralyth.Patches.Safety;
using Seralyth.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.RigUtilities;
using Random = UnityEngine.Random;

namespace Seralyth.Mods
{
    public static class Safety
    {
        private static bool antiOculusReportHooked;

        public static void GeneralSafety()
        {
            if (!Buttons.GetIndex(
                        "Anti Report <color=grey>[</color><color=green>Disconnect</color><color=grey>]</color>")
                    .enabled) AntiReportDisconnect();
            if (!Buttons.GetIndex(
                        "Anti Report <color=grey>[</color><color=green>Anti Cheat</color><color=grey>]</color>")
                    .enabled) AntiCheatPatches.SendReportPatch.AntiACReport = true;
            if (!Buttons.GetIndex("Anti Moderator").enabled) AntiModerator();
            if (!Buttons.GetIndex("Anti Report <color=grey>[</color><color=green>Oculus</color><color=grey>]</color>")
                    .enabled && !antiOculusReportHooked)
            {
                antiOculusReportHooked = true;
                EnableAntiOculusReport();
            }
        }

        public static void DisableGeneral()
        {
            if (!Buttons.GetIndex(
                        "Anti Report <color=grey>[</color><color=green>Anti Cheat</color><color=grey>]</color>")
                    .enabled) AntiCheatPatches.SendReportPatch.AntiACReport = false;
            if (!Buttons.GetIndex("Anti Report <color=grey>[</color><color=green>Oculus</color><color=grey>]</color>")
                    .enabled) DisableAntiOculusReport();
        }

        public static void NoFinger()
        {
            ControllerInputPoller.instance.leftControllerGripFloat = 0f;
            ControllerInputPoller.instance.rightControllerGripFloat = 0f;
            ControllerInputPoller.instance.leftControllerIndexFloat = 0f;
            ControllerInputPoller.instance.rightControllerIndexFloat = 0f;
            ControllerInputPoller.instance.leftControllerPrimaryButton = false;
            ControllerInputPoller.instance.leftControllerSecondaryButton = false;
            ControllerInputPoller.instance.rightControllerPrimaryButton = false;
            ControllerInputPoller.instance.rightControllerSecondaryButton = false;
            ControllerInputPoller.instance.leftControllerPrimaryButtonTouch = false;
            ControllerInputPoller.instance.leftControllerSecondaryButtonTouch = false;
            ControllerInputPoller.instance.rightControllerPrimaryButtonTouch = false;
            ControllerInputPoller.instance.rightControllerSecondaryButtonTouch = false;
        }

        public static void SetGamemodeButtonActive(bool active = true) =>
            GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/TreeRoomInteractables/UI/ModeSelector_Group")
                .SetActive(active);

        public static void FakeOculusMenu()
        {
            if (leftPrimary)
            {
                NoFinger();
                ConnectedControllerHandler.Instance.leftHandFollower.UpdatePositionRotation();
                ConnectedControllerHandler.Instance.rightHandFollower.UpdatePositionRotation();
            }

            Movement.SetHandEnabled(!leftPrimary);
        }

        public static void FakeReportMenu()
        {
            if (leftSecondary)
                NoFinger();

            GTPlayer.Instance.inOverlay = leftPrimary;
        }

        public static void FakeBrokenController()
        {
            Vector3 Position = leftPrimary
                ? GorillaTagger.Instance.leftHandTransform.position
                : GorillaTagger.Instance.rightHandTransform.position;
            Quaternion Rotation = leftPrimary
                ? GorillaTagger.Instance.leftHandTransform.rotation
                : GorillaTagger.Instance.rightHandTransform.rotation;

            GTPlayer.Instance.GetControllerTransform(true).position =
                GTPlayer.Instance.headCollider.transform.position + GTPlayer.Instance.headCollider.transform.up *
                (-0.5f * GTPlayer.Instance.scale);
            GTPlayer.Instance.GetControllerTransform(true).rotation =
                Camera.main.transform.rotation * Quaternion.Euler(-55f, 90f, 0f);

            GTPlayer.Instance.GetControllerTransform(false).position = Position;
            GTPlayer.Instance.GetControllerTransform(false).rotation = Rotation;

            ControllerInputPoller.instance.leftControllerGripFloat = 0f;
            ControllerInputPoller.instance.leftControllerIndexFloat = 0f;
            ControllerInputPoller.instance.leftControllerPrimaryButton = false;
            ControllerInputPoller.instance.leftControllerSecondaryButton = false;
            ControllerInputPoller.instance.leftControllerPrimaryButtonTouch = false;
            ControllerInputPoller.instance.leftControllerSecondaryButtonTouch = false;
        }

        public static Vector3 deadPosition = Vector3.zero;
        public static Vector3 lvel = Vector3.zero;

        public static void FakePowerOff()
        {
            if (leftJoystickClick)
            {
                if (deadPosition == Vector3.zero)
                {
                    deadPosition = GorillaTagger.Instance.rigidbody.transform.position;
                    lvel = GorillaTagger.Instance.rigidbody.linearVelocity;
                }

                VRRig.LocalRig.enabled = false;
                GorillaTagger.Instance.rigidbody.transform.position = deadPosition;
                GorillaTagger.Instance.rigidbody.linearVelocity = lvel;
            }
            else
            {
                deadPosition = Vector3.zero;
                VRRig.LocalRig.enabled = true;
            }
        }

        public static void FakeValveTracking()
        {
            if (rightJoystickClick)
                VRRig.LocalRig.head.rigTarget.transform.rotation = Quaternion.identity;
        }

        public static void SpoofSupportPage() =>
            GorillaComputer.instance.screenText.Set(GorillaComputer.instance.screenText.currentText
                .Replace("STEAM", "QUEST").Replace(GorillaComputer.instance.buildDate,
                    "05/30/2024 16:50:12\nBUILD CODE 4893\nMANAGED ACCOUNT: NO"));

        private static string previousNickName;

        public static void AntiNameBan()
        {
            if (previousNickName != PhotonNetwork.LocalPlayer.NickName)
            {
                if (!BanPatches.CheckAutoBanListForName.CheckBanList(PhotonNetwork.LocalPlayer.NickName))
                {
                    NotificationManager.SendNotification(
                        $"<color=grey>[</color><color=red>WARNING</color><color=grey>]</color> Your name, {PhotonNetwork.LocalPlayer.NickName}, is not allowed. It has been reset for your safety.");
                    ChangeName(RandomUtilities.RandomString(8));
                }
            }

            previousNickName = PhotonNetwork.LocalPlayer.NickName;
        }

        public static float flushCooldown;

        public static void FlushRPCs()
        {
            if (Time.time > flushCooldown)
            {
                RPCProtection();
                flushCooldown = Time.time + 5f;
                return;
            }

            NotificationManager.SendNotification(
                "<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not meant to spam Flush RPCs. Only call it once after you are done spamming RPCs.");
        }

        public static void AntiLurker()
        {
            LurkerGhost lurker = Overpowered.Lurker;
            if (lurker.currentState == LurkerGhost.ghostState.possess &&
                lurker.targetPlayer == NetworkSystem.Instance.LocalPlayer)
                lurker.ChangeState(LurkerGhost.ghostState.patrol);
        }

        private static float lastCacheClearedTime;

        public static void AutoClearCache()
        {
            if (Time.time > lastCacheClearedTime)
            {
                lastCacheClearedTime = Time.time + 60f;
                GC.Collect();
            }
        }

        public static int antiReportRangeIndex;
        public static float threshold = 0.35f;

        public static void ChangeAntiReportRange(bool positive = true)
        {
            string[] rangeNames =
            {
                "Default", // The report button
                "Large", // The report button within the range of 3 people
                "Massive" // The entire fucking board
            };
            float[] distances =
            {
                0.35f,
                0.7f,
                1.5f
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    antiReportRangeIndex++;
                else
                    antiReportRangeIndex--;
            }

            antiReportRangeIndex %= rangeNames.Length;
            if (antiReportRangeIndex < 0)
                antiReportRangeIndex = rangeNames.Length - 1;

            threshold = distances[antiReportRangeIndex];
            Buttons.GetIndex("Change Anti Report Distance").overlapText =
                "Change Anti Report Distance <color=grey>[</color><color=green>" + rangeNames[antiReportRangeIndex] +
                "</color><color=grey>]</color>";
        }

        public static bool smartAntiReport;
        public static int buttonClickTime;
        public static string buttonClickPlayer;

        public static bool SmartAntiReport(NetPlayer linePlayer) =>
            smartAntiReport && linePlayer.UserId == buttonClickPlayer && Time.frameCount == buttonClickTime &&
            PhotonNetwork.CurrentRoom.IsVisible &&
            !PhotonNetwork.CurrentRoom.CustomProperties.ToString().Contains("MODDED");

        public static void EventReceived_SmartAntiReport(EventData data)
        {
            try
            {
                if (data.Code == 200)
                {
                    string rpcName =
                        PhotonNetwork.PhotonServerSettings.RpcList[
                            int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                    object[] args = (object[])((Hashtable)data.CustomData)[4];
                    if (rpcName == "RPC_PlayHandTap" && (int)args[0] == 67)
                    {
                        buttonClickTime = Time.frameCount;
                        buttonClickPlayer = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender).UserId;
                    }
                }
            }
            catch
            {
            }
        }

        public static void EnableSmartAntiReport()
        {
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_SmartAntiReport;
            smartAntiReport = true;
        }

        public static void DisableSmartAntiReport()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_SmartAntiReport;
            smartAntiReport = false;
        }

        public static void VisualizeAntiReport()
        {
            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != NetworkSystem.Instance.LocalPlayer) continue;
                Transform report = line.reportButton.gameObject.transform;

                Visuals.VisualizeAura(report.position, threshold, Color.red);

                if (antiMute)
                    Visuals.VisualizeAura(line.muteButton.gameObject.transform.position, threshold, Color.red);
            }
        }

        private static bool OverlappingButton(VRRig vrrig, Vector3 position) =>
            new[]
            {
                vrrig.rightHandTransform.position,
                vrrig.leftHandTransform.position,
                vrrig.rightHand.syncPos,
                vrrig.leftHand.syncPos
            }.Any(handPos => Vector3.Distance(handPos, position) < threshold);

        public static bool antiMute;

        public static VRRig reportRig;

        public static void AntiReport(Action<VRRig, Vector3> onReport)
        {
            if (!NetworkSystem.Instance.InRoom) return;

            if (reportRig != null)
            {
                onReport?.Invoke(reportRig, reportRig.transform.position);
                reportRig = null;
                AchievementManager.UnlockAchievement(new AchievementManager.Achievement
                {
                    name = "Troublemaker",
                    description = "Evade a player report.",
                    icon = "Images/Achievements/troublemaker.png"
                });
                return;
            }

            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != NetworkSystem.Instance.LocalPlayer) continue;
                Transform report = line.reportButton.gameObject.transform;

                foreach (var vrrig in from vrrig in VRRigCache.ActiveRigs
                         where !vrrig.isLocal
                         where OverlappingButton(vrrig, report.position) || (antiMute &&
                                                                             OverlappingButton(vrrig,
                                                                                 line.muteButton.gameObject.transform
                                                                                     .position))
                         where !smartAntiReport || SmartAntiReport(line.linePlayer)
                         select vrrig)
                    onReport?.Invoke(vrrig, report.transform.position);
            }
        }

        public static float antiReportDelay;
        private static bool afrEnabled;
        private static string afrPendingReport;
        private static readonly HashSet<string> afrReportedPlayers = new HashSet<string>();

        public static void AntiReportDisconnect()
        {
            AntiReport((vrrig, position) =>
            {
                NetworkSystem.Instance.ReturnToSinglePlayer();
                RPCProtection();

                if (!(Time.time > antiReportDelay)) return;
                antiReportDelay = Time.time + 1f;
                NotificationManager.SendNotification(
                    "<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " +
                    GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, you have been disconnected.");
            });
        }

        public static void AntiReportReconnect()
        {
            AntiReport((vrrig, position) =>
            {
                if (!(Time.time > antiReportDelay)) return;
                Important.Reconnect();
                RPCProtection();

                antiReportDelay = Time.time + 1f;
                NotificationManager.SendNotification(
                    "<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " +
                    GetPlayerFromVRRig(vrrig).NickName +
                    " attempted to report you, you have been disconnected and will be reconnected shortly.");
            });
        }

        public static void AntiReportJoinRandom()
        {
            AntiReport((vrrig, position) =>
            {
                if (!(Time.time > antiReportDelay)) return;

                Important.JoinRandom();
                RPCProtection();

                antiReportDelay = Time.time + 1f;
                NotificationManager.SendNotification(
                    "<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " +
                    GetPlayerFromVRRig(vrrig).NickName +
                    " attempted to report you, you have been disconnected and will be reconnected shortly.");
            });
        }

        public static void EventReceived_AntiOculusReport(EventData data)
        {
            try
            {
                if (data.Code == 200)
                {
                    string rpcName =
                        PhotonNetwork.PhotonServerSettings.RpcList[
                            int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                    object[] args = (object[])((Hashtable)data.CustomData)[4];
                    if (rpcName == "RPC_PlayHandTap" && (int)args[0] == 67)
                    {
                        VRRig target =
                            GetVRRigFromPlayer(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender));
                        if (Vector3.Distance(target.leftHandTransform.position, target.rightHandTransform.position) <
                            0.1f)
                            AntiReportFRT(PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender));
                    }
                }
            }
            catch
            {
            }
        }

        public static void EnableAntiOculusReport() =>
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_AntiOculusReport;

        public static void DisableAntiOculusReport() =>
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_AntiOculusReport;

        public static float antiReportNotifyDelay;

        public static void AntiReportNotify()
        {
            if (!(Time.time > antiReportNotifyDelay)) return;

            string notifyText = "";
            AntiReport((vrrig, position) =>
            {
                antiReportNotifyDelay = Time.time + 0.1f;

                if (notifyText == "")
                    notifyText = GetPlayerFromVRRig(vrrig).NickName;
                else
                {
                    if (notifyText.Contains("&"))
                        notifyText = GetPlayerFromVRRig(vrrig).NickName + ", " + notifyText;
                    else
                        notifyText += " & " + GetPlayerFromVRRig(vrrig).NickName;
                }
            });

            if (notifyText != "")
                NotificationManager.SendNotification(
                    $"<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> {notifyText} {(notifyText.Contains("&") || notifyText.Contains(",") ? "are" : "is")} reporting you.");
        }

        public static void AntiReportOverlay()
        {
            if (!(Time.time > antiReportNotifyDelay)) return;
            string notifyText = null;
            AntiReport((vrrig, position) =>
            {
                if (notifyText == null)
                    notifyText = GetPlayerFromVRRig(vrrig).NickName;
                else
                {
                    if (notifyText.Contains("&"))
                        notifyText = GetPlayerFromVRRig(vrrig).NickName + ", " + notifyText;
                    else
                        notifyText += " & " + GetPlayerFromVRRig(vrrig).NickName;
                }
            });

            if (notifyText.IsNullOrEmpty())
                NotificationManager.information.Remove("Anti-Report");
            else
                NotificationManager.information["Anti-Report"] = notifyText;
        }

        private static void EventReceived_AFR(EventData data)
        {
            if (!afrEnabled) return;
            if (data.Code != 200) return;

            try
            {
                string rpcName = PhotonNetwork.PhotonServerSettings.RpcList[int.Parse(((Hashtable)data.CustomData)[5].ToString())];
                if (rpcName != "RPC_PlayHandTap") return;

                object[] args = (object[])((Hashtable)data.CustomData)[4];
                if (args.Length == 0 || (int)args[0] != 67) return;

                NetPlayer sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (sender == null || sender == NetworkSystem.Instance.LocalPlayer) return;

                VRRig senderRig = GetVRRigFromPlayer(sender);
                if (senderRig == null) return;

                foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                {
                    if (line.linePlayer != NetworkSystem.Instance.LocalPlayer) continue;

                    if (OverlappingButton(senderRig, line.reportButton.gameObject.transform.position))
                    {
                        afrPendingReport = sender.UserId;
                        break;
                    }
                }
            }
            catch { }
        }

        public static void EnableAFR()
        {
            afrEnabled = true;
            PhotonNetwork.NetworkingClient.EventReceived += EventReceived_AFR;
        }

        public static void DisableAFR()
        {
            afrEnabled = false;
            PhotonNetwork.NetworkingClient.EventReceived -= EventReceived_AFR;
            afrReportedPlayers.Clear();
        }

        public static void AFR()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer == null || !afrReportedPlayers.Contains(line.linePlayer.UserId)) continue;
                line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cheating);
                line.reportButton.isOn = true;
                line.reportButton.UpdateColor();
            }

            if (!string.IsNullOrEmpty(afrPendingReport))
            {
                string userId = afrPendingReport;
                afrPendingReport = null;

                NetPlayer reporter = GetPlayerFromID(userId);
                if (reporter != null && reporter != NetworkSystem.Instance.LocalPlayer)
                {
                    HandleAFRReport(reporter);
                }
            }

            if (reportRig != null && reportRig != VRRig.LocalRig)
            {
                VRRig rig = reportRig;
                reportRig = null;

                NetPlayer reporter = GetPlayerFromVRRig(rig);
                if (reporter != null && reporter != NetworkSystem.Instance.LocalPlayer)
                {
                    HandleAFRReport(reporter);
                }
            }
        }

        private static void HandleAFRReport(NetPlayer reporter)
        {
            RPCProtection();

            afrReportedPlayers.Add(reporter.UserId);

            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != reporter) continue;
                line.SetReportState(false, GorillaPlayerLineButton.ButtonType.Cheating);
                line.reportButton.isOn = true;
                line.reportButton.UpdateColor();
                break;
            }

            GorillaPlayerScoreboardLine.ReportPlayer(reporter.UserId, GorillaPlayerLineButton.ButtonType.Cheating, reporter.NickName);
            NotificationManager.SendNotification($"<color=grey>[</color><color=red>AFR</color><color=grey>]</color> Auto Flush Report: reported {reporter.NickName}");
        }

        private static readonly HashSet<string> afMuted = new HashSet<string>();

        public static void EnableAF()
        {
            afMuted.Clear();
        }

        public static void DisableAF()
        {
            afMuted.Clear();
        }

        public static void AF()
        {
            if (!NetworkSystem.Instance.InRoom) return;

            foreach (var line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != NetworkSystem.Instance.LocalPlayer) continue;

                foreach (var vrrig in VRRigCache.ActiveRigs)
                {
                    if (vrrig.isLocal) continue;
                    if (!OverlappingButton(vrrig, line.muteButton.gameObject.transform.position)) continue;

                    NetPlayer muter = GetPlayerFromVRRig(vrrig);
                    if (muter == null || muter == NetworkSystem.Instance.LocalPlayer) continue;
                    if (!afMuted.Add(muter.UserId)) continue;

                    RPCProtection();

                    foreach (var muteLine in GorillaScoreboardTotalUpdater.allScoreboardLines)
                    {
                        if (muteLine.linePlayer != muter) continue;
                        muteLine.muteButton.isOn = true;
                        muteLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                        break;
                    }

                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>AF</color><color=grey>]</color> Auto Mute: muted {muter.NickName}");
                    break;
                }
            }
        }

        private static string ptTargetId;
        private static float ptTimeout;
        private static bool ptActive;

        public static void PTEnable()
        {
            PromptText("Enter Player ID to track:", () =>
            {
                ptTargetId = keyboardInput?.Trim();
                if (string.IsNullOrEmpty(ptTargetId))
                {
                    ptActive = false;
                    return;
                }

                ptTimeout = Time.time + 5f;
                ptActive = true;

                PhotonNetwork.NetworkingClient.OpFindFriends(new string[] { ptTargetId });
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Searching for player...");
            });
        }

        public static void PTDisable()
        {
            ptActive = false;
            ptTargetId = null;
        }

        public static void PT()
        {
            if (!ptActive || string.IsNullOrEmpty(ptTargetId)) return;
            if (Time.time > ptTimeout)
            {
                ptActive = false;
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Player search timed out.");
            }
        }

        public static void HandleFindFriendsResponse(OperationResponse response)
        {
            if (!ptActive) return;
            if (response.OperationCode != 222) return;

            ptActive = false;

            try
            {
                string[] userIds = response[2] as string[];
                bool[] onlineStatus = response[3] as bool[];
                string[] roomNames = response[4] as string[];

                if (userIds == null || onlineStatus == null)
                {
                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Could not parse server response.");
                    return;
                }

                for (int i = 0; i < userIds.Length; i++)
                {
                    if (userIds[i] != ptTargetId) continue;

                    if (onlineStatus[i] && roomNames != null && !string.IsNullOrEmpty(roomNames[i]))
                        NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Player found in room: {roomNames[i]}");
                    else if (onlineStatus[i])
                        NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Player is online but not in a room.");
                    else
                        NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Player is offline or not found.");
                    return;
                }

                NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Player ID not found in server response.");
            }
            catch
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>PT</color><color=grey>]</color> Error parsing server response.");
            }
        }

        public static void AntiReportFRT(Player subject) =>
            reportRig = subject.VRRig();

        public static void AntiModerator()
        {
            foreach (var vrrig in VRRigCache.ActiveRigs.Where(vrrig =>
                         !vrrig.isOfflineVRRig && vrrig.Cosmetics().Contains("LBAAK") ||
                         vrrig.Cosmetics().Contains("LBAAD") || vrrig.Cosmetics().Contains("LMAPY")))
            {
                try
                {

                    VRRig plr = vrrig;
                    NetPlayer player = GetPlayerFromVRRig(plr);
                    if (player != null)
                    {
                        string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
                        float r = 0f;
                        float g = 0f;
                        float b = 0f;
                        try
                        {

                            r = plr.playerColor.r * 255;
                            g = plr.playerColor.r * 255;
                            b = plr.playerColor.r * 255;
                        }
                        catch
                        {
                            LogManager.Log("Failed to log colors, rig most likely nonexistent");
                        }

                        try
                        {
                            text += "\n====================================\n";
                            text += string.Concat("Player Name: \"", player.NickName, "\", Player ID: \"",
                                player.UserId, "\", Player Color: (R: ", r.ToString(), ", G: ", g.ToString(), ", B: ",
                                b.ToString(), ")");
                        }
                        catch
                        {
                            LogManager.Log("Failed to log player");
                        }

                        text += "\n====================================\n";
                        text += "Text file generated with Seralyth Menu";
                        string fileName = $"{PluginInfo.BaseDirectory}/" + player.NickName + " - Anti Moderator.txt";

                        File.WriteAllText(fileName, text);
                    }
                }
                catch
                {
                }

                NetworkSystem.Instance.ReturnToSinglePlayer();
                NotificationManager.SendNotification(
                    $"<color=grey>[</color><color=purple>ANTI-MODERATOR</color><color=grey>]</color> {vrrig.GetName()} is a moderator, you have been disconnected. Their player ID and room code have been saved to a file.");
            }
        }

        public static void AntiContentCreator()
        {
            foreach (var vrrig in VRRigCache.ActiveRigs.Where(vrrig =>
                         !vrrig.isOfflineVRRig &&
                         Visuals.specialCosmetics.Keys.Any(x => vrrig.Cosmetics().Contains(x))))
            {
                try
                {

                    VRRig plr = vrrig;
                    NetPlayer player = GetPlayerFromVRRig(plr);
                    if (player != null)
                    {
                        string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
                        float r = 0f;
                        float g = 0f;
                        float b = 0f;
                        try
                        {

                            r = plr.playerColor.r * 255;
                            g = plr.playerColor.r * 255;
                            b = plr.playerColor.r * 255;
                        }
                        catch
                        {
                            LogManager.Log("Failed to log colors, rig most likely nonexistent");
                        }

                        try
                        {
                            text += "\n====================================\n";
                            text += string.Concat("Player Name: \"", player.NickName, "\", Player ID: \"",
                                player.UserId, "\", Player Color: (R: ", r.ToString(), ", G: ", g.ToString(), ", B: ",
                                b.ToString(), ")");
                        }
                        catch
                        {
                            LogManager.Log("Failed to log player");
                        }

                        text += "\n====================================\n";
                        text += "Text file generated with Seralyth Menu";
                        string fileName = $"{PluginInfo.BaseDirectory}/" + player.NickName +
                                          " - Anti Content Creator.txt";

                        File.WriteAllText(fileName, text);
                    }
                }
                catch
                {
                }

                NetworkSystem.Instance.ReturnToSinglePlayer();
                NotificationManager.SendNotification(
                    $"<color=grey>[</color><color=purple>ANTI-CONTENT CREATOR</color><color=grey>]</color> {vrrig.GetName()} is a content creator, you have been disconnected. Their player ID and room code have been saved to a file.");
            }
        }

        private static bool previousSpecial;

        public static void CosmeticNotifications()
        {
            VRRig specialRig = null;
            string specialCosmetic = null;

            foreach (VRRig rig in VRRigCache.ActiveRigs.Where(rig => !rig.IsLocal()))
            {
                foreach (var cosmetic in Visuals.specialCosmetics.Where(cosmetic =>
                             rig.Cosmetics().Contains(cosmetic.Key)))
                {
                    specialRig = rig;
                    specialCosmetic = cosmetic.Value;
                    break;
                }

                if (specialRig != null)
                    break;
            }

            if (specialRig != null && !previousSpecial)
                NotificationManager.SendNotification(
                    $"<color=grey>[</color><color=#{specialRig.GetColor().ToHex()}>COSMETIC</color><color=grey>]</color> {specialRig.GetName()} has {specialCosmetic}.");

            previousSpecial = specialRig != null;
        }

        private static float lastVol;
        private static float startSilenceTime = -1f;
        private static bool reloaded;

        public static void BypassAutomod()
        {
            GorillaTagger.moderationMutedTime = -1f;

            if (GorillaComputer.instance.autoMuteType != "OFF")
            {
                GorillaComputer.instance.autoMuteType = "OFF";
                PlayerPrefs.SetInt("autoMute", 0);
                PlayerPrefs.Save();
            }

            Recorder mic = NetworkSystem.Instance.VoiceConnection.PrimaryRecorder;
            if (mic == null)
                return;

            if (mic.SourceType == Recorder.InputSourceType.AudioClip)
                return;

            float volume = 0f;
            GorillaSpeakerLoudness recorder = VRRig.LocalRig.GetComponent<GorillaSpeakerLoudness>();
            if (recorder != null)
                volume = recorder.Loudness;

            if (volume == 0f)
            {
                if (lastVol != 0f)
                {
                    startSilenceTime = Time.time;
                    reloaded = false;
                }

                if (startSilenceTime > 0f && !reloaded && Time.time - startSilenceTime >= 0.25f)
                {
                    mic.RestartRecording(true);
                    reloaded = true;
                }
            }
            else
            {
                startSilenceTime = -1f;
                reloaded = false;
            }

            lastVol = volume;
        }

        public static void BypassModCheckers()
        {
            var player = PhotonNetwork.LocalPlayer;
            if (player == null) return;

            if (player.CustomProperties == null || player.CustomProperties.Count == 0) return;

            Hashtable toRemove = new Hashtable();

            foreach (var key in from keyObj in player.CustomProperties.Keys.ToList()
                     select keyObj?.ToString()
                     into key
                     where key != null
                     where !key.Equals("didTutorial")
                     select key)
                toRemove[key] = null;

            if (toRemove.Count > 0)
                player.SetCustomProperties(toRemove);
        }

        private static Vector3 smoothedLeftHandPosition;
        private static Vector3 smoothedRightHandPosition;

        public static void AntiPredictions()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                MassSerialize(true, new[] { VRRig.LocalRig.GetPhotonView() });

                Vector3 leftHandPosition = VRRig.LocalRig.leftHand.rigTarget.localPosition;
                Vector3 rightHandPosition = VRRig.LocalRig.rightHand.rigTarget.localPosition;

                smoothedLeftHandPosition = Vector3.Lerp(smoothedLeftHandPosition, leftHandPosition, 0.75f);
                smoothedRightHandPosition = Vector3.Lerp(smoothedRightHandPosition, rightHandPosition, 0.75f);

                VRRig.LocalRig.leftHand.rigTarget.localPosition = smoothedLeftHandPosition;
                VRRig.LocalRig.rightHand.rigTarget.localPosition = smoothedRightHandPosition;

                SendSerialize(VRRig.LocalRig.GetPhotonView());

                VRRig.LocalRig.leftHand.rigTarget.localPosition = leftHandPosition;
                VRRig.LocalRig.rightHand.rigTarget.localPosition = rightHandPosition;

                return false;
            };
        }

        public static void ChangeIdentity()
        {
            string randomName = "gorilla";
            for (var i = 0; i < 4; i++)
                randomName += Random.Range(0, 9).ToString();

            ChangeName(randomName);

            byte randA = (byte)Random.Range(0, 255);
            byte randB = (byte)Random.Range(0, 255);
            byte randC = (byte)Random.Range(0, 255);
            ChangeColor(new Color32(randA, randB, randC, 255));
        }

        public static void ChangeIdentityRegular()
        {
            string prefix = Random.Range(0, 3) == 0 ? namePrefix[Random.Range(0, namePrefix.Length)] : "";
            string suffix = Random.Range(0, 3) == 0 ? nameSuffix[Random.Range(0, nameSuffix.Length)] : "";
            string fName = prefix + names[Random.Range(0, names.Length)] + suffix;
            ChangeName(fName.Length > 12 ? fName[..12] : fName);

            Color[] colors =
            {
                Color.cyan,
                Color.yellow,
                Color.blue,
                Color.gray,
                Color.black,
                Color.white,
                Color.magenta,
                Color.yellow,
                Color.green,
                new Color(1f, 0.5f, 1f, 255f),
                new Color(0f, 0.5f, 0f, 255f),
                new Color32(113, 0, 198, 255),
                new Color32(170, 198, 170, 255),
                new Color32(170, 170, 170, 255),
                new Color32(227, 170, 85, 255),
                new Color32(0, 226, 255, 255)
            };
            ChangeColor(colors[Random.Range(0, colors.Length)]);
        }

        public static void ChangeIdentityCustom()
        {
            string[] names = { "seralyth", "me" };
            Color[] colors = { new Color32(255, 128, 0, 255), Color.white };

            string fileName = $"{PluginInfo.BaseDirectory}/CustomIdentities.txt";
            if (File.Exists(fileName))
            {
                string[] data = File.ReadAllText(fileName).Split("\n");
                names = data[0].Split(";");
                colors = data[1].Split(";").Select(HexToColor).ToArray();
            }
            else
                File.WriteAllText(fileName, "seralyth;me\n9b59b6;ffffff");

            string name = names[Random.Range(0, names.Length)];
            Color color = colors[Random.Range(0, colors.Length)];

            ChangeName(name.Length > 12 ? name[..12] : name);
            ChangeColor(color);
        }

        private static bool previouslyInLobby;

        public static void ChangeIdentityOnDisconnect(Action identityType)
        {
            if (!PhotonNetwork.InRoom && previouslyInLobby)
                identityType?.Invoke();

            previouslyInLobby = PhotonNetwork.InRoom;
        }

        private static readonly List<VRRig> nameSpoofRigs = new List<VRRig>();

        public static void NameSpoof()
        {
            List<VRRig> toRemove = new List<VRRig>();
            foreach (VRRig rig in nameSpoofRigs)
            {
                if (!VRRigCache.ActiveRigs.Contains(rig))
                    toRemove.Add(rig);
            }

            foreach (VRRig rig in toRemove)
                nameSpoofRigs.Remove(rig);

            toRemove.Clear();

            string archiveNickname = PhotonNetwork.NickName;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig.isLocal) continue;
                if (!nameSpoofRigs.Contains(rig))
                {
                    string prefix = Random.Range(0, 3) == 0 ? namePrefix[Random.Range(0, namePrefix.Length)] : "";
                    string suffix = Random.Range(0, 3) == 0 ? nameSuffix[Random.Range(0, nameSuffix.Length)] : "";
                    string fName = prefix + names[Random.Range(0, names.Length)] + suffix;
                    ChangeName(fName.EnforceLength(12), true);

                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", GetPlayerFromVRRig(rig),
                        Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                    nameSpoofRigs.Add(rig);
                }
            }

            if (PhotonNetwork.NickName != archiveNickname)
                PhotonNetwork.NickName = archiveNickname;
        }

        private static readonly List<VRRig> colorSpoofRigs = new List<VRRig>();

        public static void ColorSpoof()
        {
            List<VRRig> toRemove = new List<VRRig>();
            foreach (VRRig rig in colorSpoofRigs)
            {
                if (!VRRigCache.ActiveRigs.Contains(rig))
                    toRemove.Add(rig);
            }

            foreach (VRRig rig in toRemove)
                colorSpoofRigs.Remove(rig);

            toRemove.Clear();

            foreach (var rig in VRRigCache.ActiveRigs.Where(rig => !rig.isLocal)
                         .Where(rig => !colorSpoofRigs.Contains(rig)))
            {
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_InitializeNoobMaterial", GetPlayerFromVRRig(rig),
                    Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                colorSpoofRigs.Add(rig);
            }
        }

        public static int fpsSpoofValue = 90;

        public static void FPSSpoof()
        {
            FPSPatch.enabled = true;
            FPSPatch.spoofFPSValue = Random.Range(fpsSpoofValue - 10, fpsSpoofValue + 10);
        }

        public static int pingSpoofValue = 200;

        public static void PingSpoof()
        {
            SerializePatch.OverrideSerialization ??= () =>
            {
                MassSerialize(timeOffset: pingSpoofValue);
                return false;
            };
        }

        public static void ChangeFPSSpoofValue(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    fpsSpoofValue += 5;
                else
                    fpsSpoofValue -= 5;
            }

            if (fpsSpoofValue > 140)
                fpsSpoofValue = 5;
            if (fpsSpoofValue < 5)
                fpsSpoofValue = 140;

            Buttons.GetIndex("Change FPS Spoof Value").overlapText =
                "Change FPS Spoof Value <color=grey>[</color><color=green>" + fpsSpoofValue +
                "</color><color=grey>]</color>";
        }

        public static void ChangePingSpoofValue(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    pingSpoofValue += 100;
                else
                    pingSpoofValue -= 100;
            }

            if (pingSpoofValue > 10000)
                pingSpoofValue = 100;
            if (pingSpoofValue < 100)
                pingSpoofValue = 10000;

            Buttons.GetIndex("Change Ping Spoof Value").overlapText =
                "Change Ping Spoof Value <color=grey>[</color><color=green>" + pingSpoofValue +
                "</color><color=grey>]</color>";
        }

        public static readonly string[] namePrefix =
        {
            "EPIC", "EPIK", "REAL", "NOT", "SILLY", "LITTLE", "BIG", "MAYBE", "MONKE", "SUB2", "OG", "FUN", "FR", "NOT",
            "NOTA"
        };

        public static readonly string[] nameSuffix =
        {
            "GT", "VR", "LOL", "GTVR", "FAN", "XD", "LOL", "MONK", "YT", "NOT", "FR"
        };

        public static readonly string[] names =
        {
            "0", "SHIBA", "PBBV", "J3VU", "BEES", "NAMO", "MANGO", "FROSTY", "FRISH", "LEMMING",
            "BILLY", "TIMMY", "MINIGAMES", "JMANCURLY", "VMT", "ELLIOT", "POLAR", "3CLIPCE", "DAISY09",
            "SHARKPUPPET", "DUCKY", "EDDIE", "EDDY", "RAKZZ", "CASEOH", "SKETCH", "SKY", "RETURN",
            "WATERMELON", "CRAZY", "MONK", "MONKE", "MONKI", "MONKEY", "MONKIY", "GORILL", "GOORILA", "GORILLA",
            "REDBERRY", "FOX", "RUFUS", "TTT", "TTTPIG", "PPPTIG", "K9", "BTC", "TICKLETIPJR", "BANANA",
            "PEANUTBUTTER", "GHOSTMONKE", "STATUE", "TURBOALLEN", "NOVA", "LUNAR", "MOON", "SUN", "RANDOM", "UNKNOWN",
            "GLITCH", "BUG", "ERROR", "CODE", "HACKER", "MODDER", "INVIS", "INVISIBLE", "TAGGER", "UNTAGGED",
            "BLUE", "RED", "GREEN", "PURPLE", "YELLOW", "BLACK", "WHITE", "BROWN", "CYAN", "GRAY",
            "GREY", "BANNED", "LEMON", "PLUSHIE", "CHEETO", "TIKTOK", "YOUTUBE", "TWITCH", "DISCORD", "MODDER", "HACKER"
        };

        public static string targetRank = "High";
        public static int rankIndex = 2;

        public static void ChangeRankedTier(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    rankIndex++;
                else
                    rankIndex--;
            }

            rankIndex %= 3;
            if (rankIndex < 0)
                rankIndex = 2;

            targetRank = ((RankedProgressionManager.ERankedMatchmakingTier)rankIndex).ToString();
            Buttons.GetIndex("Change Ranked Tier").overlapText =
                "Change Matchmaking Tier <color=grey>[</color><color=green>" + targetRank +
                "</color><color=grey>]</color>";
        }

        public static void ChangeELOValue(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    targetElo += 100;
                else
                    targetElo -= 100;
            }

            if (targetElo > 4000)
                targetElo = 0;
            if (targetElo < 0)
                targetElo = 4000;

            Buttons.GetIndex("Change ELO Value").overlapText = "Change ELO Value <color=grey>[</color><color=green>" +
                                                               targetElo + "</color><color=grey>]</color>";
        }

        public static void ChangeBadgeTier(bool positive = true)
        {
            string[] badgeNames =
            {
                "Wood",
                "Rock",
                "Bronze",
                "Silver",
                "Gold",
                "Platinum",
                "Crystal",
                "Banana"
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    targetBadge++;
                else
                    targetBadge--;
            }

            targetBadge %= 8;
            if (targetBadge < 0)
                targetBadge = 7;

            Buttons.GetIndex("Change Badge Tier").overlapText = "Change Badge Tier <color=grey>[</color><color=green>" +
                                                                badgeNames[targetBadge] +
                                                                "</color><color=grey>]</color>";
        }

        public static void SpoofRank(bool enabled, string tier = null)
        {
            RankedPatch.enabled = enabled;
            RankedPatch.targetTier = tier;
        }

        /// <summary>
        /// Spoofs your platform for the Ranked gamemode.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="target"></param>
        public static void SpoofPlatform(bool enabled, string target = null)
        {
            RankedPatch.enabled = enabled;
            RankedPatch.targetPlatform = target;
        }


        public static bool spoofingPlatform;

        public static void SpoofPlatform(bool enabled)
        {
            spoofingPlatform = enabled;
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateRankedInfo", RpcTarget.Others, 0, enabled ? 1 : 0,
                enabled ? 0 : 1);
        }

        public static int targetElo = 4000;
        public static int targetBadge = 7;

        public static void SpoofBadge()
        {
            SetRankedPatch.enabled = true;
            if (!Mathf.Approximately(VRRig.LocalRig.currentRankedELO, targetElo) ||
                VRRig.LocalRig.currentRankedSubTierQuest != targetBadge ||
                VRRig.LocalRig.currentRankedSubTierPC != targetBadge)
                VRRig.LocalRig.SetRankedInfo(targetElo, targetBadge, targetBadge);
        }

        private const string ArsPlayersUrl = "https://raw.githubusercontent.com/AutoReportSystem/ARSPlayerIDs/refs/heads/main/Player%20Ids.txt";
        private static string[] arsPlayersToReport;
        private static Coroutine arsCoroutine;

        public static void EnableAutoReportSystem()
        {
            if (arsCoroutine != null)
                CoroutineManager.instance.StopCoroutine(arsCoroutine);
            arsCoroutine = CoroutineManager.instance.StartCoroutine(AutoReportSystemCoroutine());
        }

        public static void DisableAutoReportSystem()
        {
            if (arsCoroutine != null)
                CoroutineManager.instance.StopCoroutine(arsCoroutine);
            arsCoroutine = null;
            arsPlayersToReport = null;
        }

        private static System.Collections.IEnumerator AutoReportSystemCoroutine()
        {
            using UnityWebRequest www = UnityWebRequest.Get(ArsPlayersUrl);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                arsPlayersToReport = www.downloadHandler.text.Split(",")
                    .Select(id => id.Trim())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToArray();
            }
            else
            {
                NotificationManager.SendNotification("<color=green>ARS</color> Failed to fetch player list.");
                yield break;
            }

            while (true)
            {
                if (arsPlayersToReport != null)
                {
                    foreach (VRRig rig in VRRigCache.ActiveRigs)
                    {
                        if (rig.isLocal || rig.creator == null) continue;
                        if (arsPlayersToReport.Contains(rig.creator.UserId))
                        {
                            NotificationManager.SendNotification($"<color=green>ARS</color> Player {rig.creator.SanitizedNickName} is on the ARS list, reporting...");

                            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
                            {
                                if (line.playerVRRig == rig)
                                {
                                    line.reportedToxicity = true;
                                    line.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);
                                }
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(1f);
            }
        }

        public static void CopyRoomCode()
        {
            if (!PhotonNetwork.InRoom)
            {
                NotificationManager.SendNotification("You are not currently in a room!");
                return;
            }

            string roomCode = PhotonNetwork.CurrentRoom.Name;
            GUIUtility.systemCopyBuffer = roomCode;
            NotificationManager.SendNotification($"<color=grey>[</color><color=green>COPY ROOM CODE</color><color=grey>]</color> <color=white>Room Code: {roomCode} copied to clipboard!</color>");
        }
    }
}
    



