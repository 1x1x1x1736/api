/*
 * Seralyth Menu  Mods/Settings.cs
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

using GorillaExtensions;
using GorillaLocomotion;
using Photon.Pun;
using Photon.Realtime;
using Seralyth.Classes.Menu;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Patches.Menu;
using Seralyth.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Windows.Speech;
using UnityEngine.XR;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.AssetUtilities;
using static Seralyth.Utilities.RigUtilities;
using Console = Seralyth.Classes.Menu.Console;
using Object = UnityEngine.Object;

namespace Seralyth.Mods
{
    public static class Settings
    {
        public static HashSet<VRRig> Blocked = new HashSet<VRRig>();
        public static void BlockPlayer(VRRig rig)
        {
            Blocked.Add(rig);
            rig.DeactivateAllRenderers();
            rig.voiceAudio.volume = 0f;
        }
        public static void UnblockPlayer(VRRig rig)
        {
            Blocked.Remove(rig);
            rig.ReactivateAllRenderers();
            rig.voiceAudio.volume = 1f;
        }

        public static void HandleBlockedPlayers()
        {
            foreach (VRRig rig in Blocked)
                rig.BreakHandLinks();
            SerializePatch.OverrideSerialization = () =>
            {
                if (Blocked.Count == 0)
                    return true;

                int[] blockedArs = Blocked.Select(rig => rig.Creator.ActorNumber).ToArray();
                int[] normalArs = VRRigExtensions.ActiveRigs
                    .Where(rig => !Blocked.Contains(rig))
                    .Select(rig => rig.Creator.ActorNumber)
                    .ToArray();

                MassSerialize(true, new[] { VRRig.LocalRig.GetPhotonView() });

                Vector3 positionArchive = VRRig.LocalRig.transform.position;
                SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = normalArs });

                VRRig.LocalRig.transform.position = new Vector3(UnityEngine.Random.Range(-99999f, 99999f), 99999f, UnityEngine.Random.Range(-99999f, 99999f));
                SendSerialize(VRRig.LocalRig.GetPhotonView(), new RaiseEventOptions { TargetActors = blockedArs });

                RPCProtection();
                VRRig.LocalRig.transform.position = positionArchive;

                return false;
            };
        }

        public static void Search() // This took me like 4 hours
        {
            isSearching = !isSearching;

            pageNumber = 0;
            keyboardInput = "";
            lastSearchText = "";

            if (isSearching)
            {
                if (clickGUI)
                {
                    searchBuiltAll = true;
                    InitializeClickGUI();
                }
                SpawnKeyboard();
            }
            else
            {
                DestroyKeyboard();
                if (clickGUI)
                {
                    searchBuiltAll = false;
                    InitializeClickGUI();
                }
            }
        }

        public static void SpawnKeyboard()
        {
            isKeyboardPc = isOnPC || toggleButtonActive && keyboardWithToggleButton;
            inTextInput = true;
            keyboardInput = "";

            shift = false;
            lockShift = false;

            if (isKeyboardPc)
                lastPressedKeys.Add(Key.Q);

            if (!isKeyboardPc)
            {
                if (VRKeyboard == null)
                {
                    VRKeyboard = LoadObject<GameObject>("VRKeyboard");
                    VRKeyboard.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
                    VRKeyboard.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation;

                    menuSpawnPosition = VRKeyboard.transform.Find("MenuSpawnPosition").gameObject;
                    VRKeyboard.transform.Find("Canvas").AddComponent<ColorChanger>().colors = textColors[1];

                    VRKeyboard.transform.localScale *= scaleWithPlayer ? GTPlayer.Instance.scale * menuScale : menuScale;
                    menuSpawnPosition.transform.localScale *= scaleWithPlayer ? GTPlayer.Instance.scale * menuScale : menuScale;

                    ColorChanger backgroundColorChanger = VRKeyboard.transform.Find("Background").gameObject.AddComponent<ColorChanger>();
                    backgroundColorChanger.colors = menuBackgroundColor;

                    foreach (GameObject key in VRKeyboard.transform.Find("Seperate").Children()
                        .Select(t => t.gameObject)
                        .Concat(new[] { VRKeyboard.transform.Find("Keys/default").gameObject }))
                    {
                        ColorChanger keyColorChanger = key.AddComponent<ColorChanger>();
                        keyColorChanger.colors = buttonColors[0];
                    }

                    if (shouldOutline)
                        OutlineObject(VRKeyboard.transform.Find("Background").gameObject, true);

                    var keys = new[] { "Numbers", "Letters", "Special", "Seperate" }
                        .Select(name => VRKeyboard.transform.Find(name))
                        .Where(t => t != null)
                        .SelectMany(t => t.Children())
                        .Select(t => t.gameObject);

                    foreach (GameObject v in keys)
                    {
                        v.AddComponent<KeyboardKey>().key = v.name;
                        v.layer = 2;

                        if (shouldOutline)
                            OutlineObject(v, true);
                    }
                }
            }

            if (lKeyReference == null)
            {
                lKeyReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lKeyReference.transform.parent = GorillaTagger.Instance.leftHandTransform;
                lKeyReference.GetComponent<Renderer>().material.color = backgroundColor.GetColor(0);
                lKeyReference.transform.localPosition = pointerOffset;
                lKeyReference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                lKeyCollider = lKeyReference.GetComponent<SphereCollider>();

                ColorChanger colorChanger = lKeyReference.AddComponent<ColorChanger>();
                colorChanger.colors = backgroundColor;
            }

            if (rKeyReference == null)
            {
                rKeyReference = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rKeyReference.transform.parent = GorillaTagger.Instance.rightHandTransform;
                rKeyReference.GetComponent<Renderer>().material.color = backgroundColor.GetColor(0);
                rKeyReference.transform.localPosition = pointerOffset;
                rKeyReference.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                rKeyCollider = rKeyReference.GetComponent<SphereCollider>();

                ColorChanger colorChanger = rKeyReference.AddComponent<ColorChanger>();
                colorChanger.colors = backgroundColor;
            }
        }

        public static void DestroyKeyboard()
        {
            inTextInput = false;
            isKeyboardPc = false;

            if (lKeyReference != null)
            {
                Object.Destroy(lKeyReference);
                lKeyReference = null;
            }

            if (rKeyReference != null)
            {
                Object.Destroy(rKeyReference);
                rKeyReference = null;
            }

            if (VRKeyboard != null)
            {
                Object.Destroy(VRKeyboard);
                VRKeyboard = null;
            }

            if (TPC != null && TPC.transform.parent.gameObject.name.Contains("CameraTablet") && isOnPC)
            {
                isOnPC = false;
                TPC.transform.position = TPC.transform.parent.position;
                TPC.transform.rotation = TPC.transform.parent.rotation;
            }
        }

        public static void GlobalReturn()
        {
            NotificationManager.ClearAllNotifications();
            Toggle(Buttons.buttons[Buttons.CurrentCategoryIndex][Buttons.GetCategory("Main")].buttonText, true);
            SoundManager.Play("Return");

            if (prompts.Count > 0)
                StopCurrentPrompt();
        }

        public static void StopCurrentPrompt() =>
            prompts.RemoveAt(0);

        public static void MergePreferences_iisStupidMenu()
        {
            string directoryToUse = "iisStupidMenu";
            string preferences = "iiMenu_Preferences.txt";

            if (!Directory.Exists(directoryToUse))
                return;

            string source = Path.Combine(directoryToUse, "Sounds");
            string destination = Path.Combine(PluginInfo.BaseDirectory, "Sounds");

            if (Directory.Exists(source))
            {
                foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                {
                    string newDir = dir.Replace(source, destination);
                    Directory.CreateDirectory(newDir);
                }

                foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                {
                    string newFile = file.Replace(source, destination);

                    Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
                    File.Copy(file, newFile);
                }
            }

            source = Path.Combine(directoryToUse, preferences);
            destination = Path.Combine(PluginInfo.BaseDirectory, "Seralyth_Preferences.txt");

            if (File.Exists(source))
            {
                string[] lines = File.ReadAllLines(source);

                if (lines.Length >= 5)
                {
                    string[] settings = lines[2].Split(new[] { ";;" }, StringSplitOptions.None);

                    int pcbgIndex = 13;
                    const int maxPcbg = 6;
                    const int maxPageType = 6;
                    const int maxThemeType = 65;

                    if (pcbgIndex < settings.Length && int.TryParse(settings[pcbgIndex], out int pcbgVal))
                        settings[pcbgIndex] = Math.Clamp(pcbgVal + 1, 0, maxPcbg).ToString();

                    lines[2] = string.Join(";;", settings);

                    if (int.TryParse(lines[3], out int pageType))
                        lines[3] = Math.Clamp(pageType - 1, 0, maxPageType).ToString();

                    if (int.TryParse(lines[4], out int theme))
                        lines[4] = Math.Clamp(theme - 1, 0, maxThemeType).ToString();
                }

                File.WriteAllLines(destination, lines);
            }

            LoadPreferences();
            Sound.LoadSoundboard(false);
            NotificationManager.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Successfully completed merge. Have fun using Seralyth Menu!");
        }

        public static void UpdateSoundPreferences()
        {
            string fileText = File.ReadAllText($"{PluginInfo.BaseDirectory}/Seralyth_Preferences.txt").Replace("\r", "");
            string[] textData = fileText.Split('\n');
            string[] data = textData[2].Split(";;");

            if (!int.TryParse(data[16], out _) || !int.TryParse(data[25], out _))
                return;

            static string helper(string value, string[] keys, string defaultKey)
            {
                if (keys.Contains(value))
                    return value;

                int index = int.Parse(value);
                index = Mathf.Clamp(index - 1, 0, keys.Length - 1);
                return keys[index];
            }

            SoundManager.DefaultSounds["Button"] = helper(data[16], SoundManager.Sounds["Buttons"].Keys.ToArray(), "Default");
            SoundManager.DefaultSounds["Notification"] = helper(data[25], SoundManager.Sounds["Notifications"].Keys.ToArray(), "None");

            data[16] = SoundManager.DefaultSounds["Button"];
            data[25] = SoundManager.DefaultSounds["Notification"];
            textData[2] = string.Join(";;", data);

            File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_Preferences.txt", string.Join("\n", textData));
        }

        public static GameObject TutorialObject;
        public static LineRenderer TutorialSelector;
        public static void ShowTutorial()
        {
            if (TutorialObject != null)
                Object.Destroy(TutorialObject);

            TutorialObject = LoadObject<GameObject>("Tutorial");

            TutorialObject.transform.position = GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.bodyCollider.transform.forward * 1f + Vector3.up * 0.25f;
            TutorialObject.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 0f);

            string videoName = "q2";
            switch (ControllerUtilities.GetLeftControllerType())
            {
                case ControllerUtilities.ControllerType.Unknown:
                case ControllerUtilities.ControllerType.Quest2:
                    videoName = "q2";
                    break;
                case ControllerUtilities.ControllerType.Quest3:
                    videoName = "q3";
                    break;
                case ControllerUtilities.ControllerType.ValveIndex:
                    videoName = "index";
                    break;
                case ControllerUtilities.ControllerType.VIVE:
                    videoName = "vive";
                    break;
            }

            VideoPlayer videoPlayer = TutorialObject.transform.Find("Video").GetComponent<VideoPlayer>();
            videoPlayer.url = $"{PluginInfo.ServerResourcePath}/Videos/Tutorial/tutorial-{videoName}.mp4";
            videoPlayer.isLooping = true;

            videoPlayer.AddComponent<TutorialButton>().buttonType = TutorialButton.ButtonType.Pause;

            TutorialObject.transform.Find("Close").AddComponent<TutorialButton>().buttonType = TutorialButton.ButtonType.Close;
        }

        private static bool lastTrigger;
        public static void UpdateTutorial()
        {
            if (Vector3.Distance(TutorialObject.transform.position, GorillaTagger.Instance.bodyCollider.transform.position) > 2f)
            {
                TutorialObject.transform.position = GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.bodyCollider.transform.forward * 1f + Vector3.up * 0.25f;
                TutorialObject.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            }

            if (TutorialSelector == null)
            {
                TutorialSelector = new GameObject("Seralyth_TutorialSelector").AddComponent<LineRenderer>();
                TutorialSelector.material.shader = Shader.Find("Sprites/Default");

                TutorialSelector.startWidth = 0.01f;
                TutorialSelector.endWidth = 0.01f;

                TutorialSelector.positionCount = 2;

                TutorialSelector.useWorldSpace = true;
            }

            TutorialSelector.startColor = BrightenColor(new Color32(255, 128, 0, 128));
            TutorialSelector.endColor = BrightenColor(new Color32(255, 102, 0, 128));

            Vector3 Direction = ControllerUtilities.GetTrueRightHand().forward;
            Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position + Direction / 4f, Direction, out var Ray, 512f, NoInvisLayerMask());
            if (!XRSettings.isDeviceActive)
            {
                Ray ray = TPC.ScreenPointToRay(Mouse.current.position.ReadValue());
                Physics.Raycast(ray, out Ray, 512f, NoInvisLayerMask());
            }

            TutorialSelector.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
            TutorialSelector.SetPosition(1, Ray.point == Vector3.zero ? GorillaTagger.Instance.rightHandTransform.position : Ray.point);

            if ((rightTrigger > 0.5f || Mouse.current.leftButton.isPressed) && !lastTrigger)
            {
                TutorialButton gunTarget = Ray.collider.GetComponentInParent<TutorialButton>();
                if (gunTarget)
                    gunTarget.ClickButton();
            }

            lastTrigger = rightTrigger > 0.5f || Mouse.current.leftButton.isPressed;
        }

        public class TutorialButton : MonoBehaviour
        {
            public enum ButtonType
            {
                Pause,
                Close
            }

            public ButtonType buttonType;
            public void ClickButton()
            {
                switch (buttonType)
                {
                    case ButtonType.Pause:
                        VideoPlayer videoPlayer = TutorialObject.transform.Find("Video").GetComponent<VideoPlayer>();
                        if (videoPlayer.isPlaying)
                            videoPlayer.Pause();
                        else
                            videoPlayer.Play();

                        break;
                    case ButtonType.Close:
                        Destroy(TutorialObject);
                        Destroy(TutorialSelector.gameObject);
                        break;
                }
            }
        }

        public static void ShowDebug()
        {
            int category = Buttons.GetCategory("Temporary Category");

            string version = PluginInfo.Version;
            if (PluginInfo.BetaBuild) version = "<color=blue>Beta</color> " + version;
            Buttons.AddButton(category, new ButtonInfo { buttonText = "Exit Info Screen", method = () => Toggle("Info Screen"), isTogglable = false, toolTip = "Returns you back to the main page." });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugMenuName", overlapText = "<color=grey><b>Seralyth Menu </b></color>" + version, label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugColor", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugName", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugId", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugClip", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugFps", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugRoomA", overlapText = "Loading...", label = true });
            Buttons.AddButton(category, new ButtonInfo { buttonText = "DebugRoomB", overlapText = "Loading...", label = true });

            Debug();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static bool hideId;
        public static void Debug()
        {
            string red = "<color=red>" + MathF.Floor(PlayerPrefs.GetFloat("redValue") * 255f) + "</color>";
            string green = ", <color=green>" + MathF.Floor(PlayerPrefs.GetFloat("greenValue") * 255f) + "</color>";
            string blue = ", <color=blue>" + MathF.Floor(PlayerPrefs.GetFloat("blueValue") * 255f) + "</color>";
            Buttons.GetIndex("DebugColor").overlapText = "Color: " + red + green + blue;

            string master = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient ? "<color=red> [Master]</color>" : "";
            Buttons.GetIndex("DebugName").overlapText = PhotonNetwork.LocalPlayer.NickName + master;

            Buttons.GetIndex("DebugId").overlapText = "<color=green>ID: </color>" + (hideId ? "Hidden" : PhotonNetwork.LocalPlayer.UserId);
            Buttons.GetIndex("DebugClip").overlapText = "<color=green>Clip: </color>" + (GUIUtility.systemCopyBuffer.Length > 25 ? GUIUtility.systemCopyBuffer[..25] : GUIUtility.systemCopyBuffer);
            Buttons.GetIndex("DebugFps").overlapText = "<b>" + lastDeltaTime + "</b> FPS <b>" + PhotonNetwork.GetPing() + "</b> Ping";
            Buttons.GetIndex("DebugRoomA").overlapText = "<color=blue>" + NetworkSystem.Instance.regionNames[NetworkSystem.Instance.currentRegionIndex].ToUpper() + "</color> " + PhotonNetwork.PlayerList.Length + " Players";

            string priv = PhotonNetwork.InRoom ? NetworkSystem.Instance.SessionIsPrivate ? "Private" : "Public" : "";
            Buttons.GetIndex("DebugRoomB").overlapText = "<color=blue>" + priv + "</color> " + (PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "Not in room");
        }
        public static void HideDebug()
        {
            int category = Buttons.GetCategory("Temporary Category");

            Buttons.RemoveButton(category, "DebugMenuName");
            Buttons.RemoveButton(category, "DebugColor");
            Buttons.RemoveButton(category, "DebugName");
            Buttons.RemoveButton(category, "DebugId");
            Buttons.RemoveButton(category, "DebugClip");
            Buttons.RemoveButton(category, "DebugFps");
            Buttons.RemoveButton(category, "DebugRoomA");
            Buttons.RemoveButton(category, "DebugRoomB");
            Buttons.CurrentCategoryName = "Main";
        }

        public static void PlayersTab()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo {
                    buttonText = "Exit Players",
                    method =() => Buttons.CurrentCategoryName = "Main",
                    isTogglable = false,
                    toolTip = "Returns you back to the main page.",
                    legal = true,
                }
            };

            if (!PhotonNetwork.InRoom)
                buttons.Add(new ButtonInfo { buttonText = "Not in a Room", label = true, legal = true });
            else
            {
                for (int i = 0; i < NetworkSystem.Instance.PlayerListOthers.Length; i++)
                {
                    NetPlayer player = NetworkSystem.Instance.PlayerListOthers[i];
                    string playerColor = "#ffffff";
                    try
                    {
                        playerColor = $"#{ColorToHex(GetVRRigFromPlayer(player).playerColor)}";
                    }
                    catch { }

                    buttons.Add(new ButtonInfo
                    {
                        buttonText = $"PlayerButton{i}",
                        overlapText = $"<color={playerColor}>" + player.NickName + "</color>",
                        method = () => NavigatePlayer(player),
                        isTogglable = false,
                        toolTip = $"See information on the player {player.NickName}.",
                        legal = true,
                    });
                }
            }

            Buttons.buttons[Buttons.GetCategory("Players")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Players";
        }

        public static void NavigatePlayer(NetPlayer player)
        {
            string targetName = player.NickName;

            VRRig playerRig = GetVRRigFromPlayer(player) ?? null;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo {
                    buttonText = "Exit PlayerInspect",
                    overlapText = $"Exit {targetName}",
                    method =() => PlayersTab(),
                    isTogglable = false,
                    toolTip = "Returns you back to the players tab.",
                    legal = true
                },

                new ButtonInfo {
                    buttonText = "Spectate Player",
                    overlapText = $"Spectate {targetName}",
                    method =() => SpectatePlayer(playerRig),
                    isTogglable = false,
                    toolTip = $"Shows you what {targetName} sees.",
                    legal = true
                },
              
                new ButtonInfo {
                    buttonText = "Teleport to Player",
                    overlapText = $"Teleport to {targetName}",
                    method =() => Movement.TeleportToPlayer(player),
                    isTogglable = false,
                    toolTip = $"Teleports you to {targetName}."
                    
                },
                new ButtonInfo {
                    buttonText = "Player Tracers",
                    overlapText = $"Tracers: {targetName}",
                    enableMethod =() => tracerTarget = playerRig,
                    disableMethod =() => tracerTarget = null,
                    method = Settings.PlayerTracers,
                    toolTip = $"Draws a tracer line to {targetName}.",
                    legal = true
                },
                new ButtonInfo {
                    buttonText = "Give Player Guns",
                    overlapText = $"Give {targetName} Guns",
                    method =() => GiveGunTarget = playerRig,
                    disableMethod =() => GiveGunTarget = null,
                    toolTip = $"Gives {targetName} every gun on the menu."
                },
                new ButtonInfo {
                    buttonText = "Copy Movement",
                    overlapText = $"Copy Movement {targetName}",
                    method =() => Movement.CopyMovementPlayer(player),
                    disableMethod = Movement.EnableRig,
                    toolTip = $"Copies the movement of {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Follow Player",
                    overlapText = $"Follow {targetName}",
                    method =() => Movement.FollowPlayer(player),
                    disableMethod = Movement.EnableRig,
                    toolTip = $"Follows {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Tag Player",
                    overlapText = $"Tag {targetName}",
                    method =() => Advantages.TagPlayer(player),
                    disableMethod = Movement.EnableRig,
                    toolTip = $"Tags {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Snowball Fling Player",
                    overlapText = $"Snowball Fling {targetName}",
                    method =() => Overpowered.FlingPlayer(player),
                    toolTip = $"Flings {targetName} with snowballs."
                },
                new ButtonInfo {
                    buttonText = "Projectile Blind Player",
                    overlapText = $"Projectile Blind {targetName}",
                    method =() => Projectiles.ProjectileBlindPlayer(player),
                    toolTip = $"Blinds {targetName} using the egg projectiles."
                },
                new ButtonInfo {
                    buttonText = "Projectile Lag Player",
                    overlapText = $"Projectile Lag {targetName}",
                    method =() => Projectiles.ProjectileLagPlayer(player),
                    toolTip = $"Lags {targetName} using the firework projectiles."
                },
                new ButtonInfo {
                    buttonText = "Lag Player",
                    overlapText = $"Lag {targetName}",
                    method =() => Overpowered.LagTarget(player),
                    toolTip = $"Lags {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Destroy Player",
                    overlapText = $"Destroy {targetName}",
                    method =() => Overpowered.DestroyPlayer(player),
                    toolTip = $"Stops all new players from seeing {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Guardian Bring Player",
                    overlapText = $"Guardian Bring {targetName}",
                    method =() => Overpowered.GuardianBringPlayer(player),
                    toolTip = $"Brings {targetName} to you."
                },
                new ButtonInfo {
                    buttonText = "Guardian Bring Player Gun",
                    overlapText = $"Guardian Bring {targetName} Gun",
                    method =() => Overpowered.GuardianBringPlayerGun(player),
                    toolTip = $"Brings {targetName} to wherever your hand desires."
                },
                new ButtonInfo {
                    buttonText = "Guardian Kick Player",
                    overlapText = $"Guardian Kick {targetName}",
                    method =() => Overpowered.GuardianKickTarget(player),
                    toolTip = $"Kicks {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Guardian Obliterate Player",
                    overlapText = $"Guardian Obliterate {targetName}",
                    method =() => Overpowered.GuardianObliteratePlayer(player),
                    toolTip = $"Obliterates {targetName}."
                },
                new ButtonInfo {
                    buttonText = "Guardian Crash Player",
                    overlapText = $"Guardian Crash {targetName}",
                    method =() => Overpowered.GuardianCrashPlayer(player),
                    toolTip = $"Crashes {targetName}."
                }
            };

            if (PhotonNetwork.IsMasterClient)
            {
                buttons.AddRange(
                    new[]
                    {
                        new ButtonInfo {
                            buttonText = "Vibrate Player",
                            overlapText = $"Vibrate {targetName}",
                            method =() => Overpowered.BetaSetStatus(RoomSystem.StatusEffects.JoinedTaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } }),
                            toolTip = $"Vibrates {targetName}'s controllers."
                        },
                        new ButtonInfo {
                            buttonText = "Slow Player",
                            overlapText = $"Slow {targetName}",
                            method =() => Overpowered.BetaSetStatus(RoomSystem.StatusEffects.TaggedTime, new RaiseEventOptions { TargetActors = new[] { player.ActorNumber } } ),
                            toolTip = $"Gives {targetName} tag freeze."
                        }
                    }
                );
            }

            if (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
            {
                buttons.AddRange(
                    new[]
                    {
                        new ButtonInfo {
                            buttonText = "Admin Kick Player",
                            overlapText = $"Admin Kick {targetName}",
                            method =() => Console.ExecuteCommand("kick", ReceiverGroup.All, player.UserId),
                            isTogglable = false,
                            toolTip = $"Kicks {targetName} if they're using the menu.",
                            legal = true
                        },
                        new ButtonInfo {
                            buttonText = "Admin Bring Player",
                            overlapText = $"Admin Bring {targetName}",
                            method =() => Console.ExecuteCommand("tp", player.ActorNumber, GorillaTagger.Instance.headCollider.transform.position),
                            isTogglable = false,
                            toolTip = $"Brings {targetName} to you if they're using the menu.",
                            legal = true
                        },
                        new ButtonInfo {
                            buttonText = "Admin Crash Player",
                            overlapText = $"Admin Crash {targetName}",
                            method =() => Console.ExecuteCommand("crash", player.ActorNumber),
                            isTogglable = false,
                            toolTip = $"Crashes {targetName} if they're using the menu.",
                            legal = true
                        },
                    }
                );
            }

            Color playerColor = playerRig?.playerColor ?? Color.black;
            if (playerRig)
                buttons.AddRange(
                    new[]
                    {
                        new ButtonInfo
                        {
                            buttonText = $"Check {player.NickName}'s Mods",
                            method = () => ModChecker(player),
                            isTogglable = false,
                            toolTip = $"View all of \"{player.NickName}\"'s mods."
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player Name",
                            overlapText = $"Name: {player.NickName}",
                            method = () => ChangeName(player.NickName),
                            isTogglable = false,
                            toolTip = $"Sets your name to \"{player.NickName}\".",
                            legal = true
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player Color",
                            overlapText =
                                $"Color: {playerColor.ToRichRGBString()}",
                            method = () => ChangeColor(playerColor),
                            isTogglable = false,
                            toolTip = $"Sets your color to the same as {targetName}.",
                            legal = true
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player User ID",
                            overlapText = $"User ID: {player.UserId}",
                            method = () =>
                            {
                                NotificationManager.SendNotification(
                                    $"<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Successfully copied {player.UserId} to the clipboard!",
                                    5000);
                                GUIUtility.systemCopyBuffer = player.UserId;
                            },
                            isTogglable = false,
                            toolTip = $"Copies {player.UserId} to your clipboard."
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player Creation Date",
                            overlapText =
                                $"Creation Date: {GetCreationDate(player.UserId, creationDate => { Buttons.GetIndex("Player Creation Date").overlapText = $"Creation Date: {creationDate}"; ReloadMenu(); })}",
                            label = true
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player Platform",
                            overlapText =
                                $"Platform: {((playerRig?.IsSteam() ?? false) ? "Steam" : "Quest")}",
                            label = true
                        },
                        new ButtonInfo
                        {
                            buttonText = "Player FPS",
                            overlapText = $"FPS: {playerRig.fps}",
                            label = true,
                            legal = true
                        }
                    }
                );

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        private static VRRig tracerTarget;

        public static void PlayerTracers()
        {
            if (tracerTarget == null) return;

            LineRenderer line = Visuals.GetLineRender();
            line.startColor = tracerTarget.playerColor;
            line.endColor = tracerTarget.playerColor;
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
            line.SetPosition(1, tracerTarget.transform.position);
        }

        public static void SpectatePlayer(VRRig rig)
        {
            GameObject cameraObject = new GameObject("Seralyth_SpectateCamera");
            RenderTexture renderTexture = new RenderTexture(512, 512, 16);
            cameraObject.AddComponent<Camera>().targetTexture = renderTexture;
            cameraObject.transform.SetParent(rig.headMesh.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.25f, 0.25f);
            promptMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                mainTexture = renderTexture
            };
            PromptSingle("<https://.mat>", () => Object.Destroy(cameraObject), "Done");
        }

        public static void CategorySettings()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> { new ButtonInfo { buttonText = "Exit Menu Settings", method = () => { Buttons.CurrentCategoryName = "Settings"; Buttons.buttons[Buttons.GetCategory("Temporary Category")] = Array.Empty<ButtonInfo>(); }, isTogglable = false, toolTip = "Returns you back to the settings menu.", legal = true } };

            foreach (var button in Buttons.buttons[Buttons.GetCategory("Main")])
            {
#if LEGAL || LEGAL_DEBUG
                if (!button.legal)
                    continue;
#endif
                buttons.Add(new ButtonInfo
                {
                    buttonText = $"Category{button.buttonText.Hash()}",
                    overlapText = button.buttonText,
                    enabled = !skipButtons.Contains(button.buttonText),
                    enableMethod = () => skipButtons.Remove(button.buttonText),
                    disableMethod = () => skipButtons.Add(button.buttonText),
                    toolTip = "Toggles the visibility of the category " + button.buttonText + ".",
                    hideFromArraylist = true,
                    legal = true
                });
            }

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void RightHand()
        {
            rightHand = true;
            if (watchMenu)
            {
                Toggle("Watch Menu");
                Toggle("Watch Menu");
                NotificationManager.ClearAllNotifications();
            }

            if (!Buttons.GetIndex("Info Watch").enabled) return;
            Toggle("Info Watch");
            Toggle("Info Watch");
            NotificationManager.ClearAllNotifications();
        }

        public static void LeftHand()
        {
            rightHand = false;
            if (watchMenu)
            {
                Toggle("Watch Menu");
                Toggle("Watch Menu");
                NotificationManager.ClearAllNotifications();
            }

            if (!Buttons.GetIndex("Info Watch").enabled) return;
            Toggle("Info Watch");
            Toggle("Info Watch");
            NotificationManager.ClearAllNotifications();
        }

        public static void ClearAllKeybinds()
        {
            foreach (KeyValuePair<string, List<string>> bind in ModBindings)
            {
                foreach (string modName in bind.Value)
                {
                    ButtonInfo btn = Buttons.GetIndex(modName);
                    if (btn != null)
                    {
                        btn.customBind = null;
                        btn.pcBindKey = null;
                    }
                }

                bind.Value.Clear();
            }

            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    v.rebindKey = null;
                    v.pcBindKey = null;
                }
            }
        }

        public static void StartBind(string bind)
        {
            if (IsRebinding)
                return;
            IsBinding = true;
            BindInput = bind;
        }
        public static void StartRebind(string bind)
        {
            if (IsBinding)
                return;
            IsRebinding = true;
            BindInput = bind;
        }

        public static void RemoveRebinds()
        {
            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    v.rebindKey = null;
                    v.pcBindKey = null;
                }
            }
            NotificationManager.SendNotification("<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Removed all rebinds.");
        }

        // The code below is fully safe. I know, it seems suspicious.
        public static void UpdateMenu()
        {
            if (File.Exists($"{PluginInfo.BaseDirectory}/.custom-build"))
            {
                Console.SendNotification("<color=yellow>Custom build detected.</color> Update skipped to preserve local changes.", 5000);
                return;
            }
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    {
                        string logoLines = "";
                        foreach (string line in PluginInfo.Logo.Split(@"
"))
                            logoLines += Environment.NewLine + @" ""    " + line + @" """;

                        string updateScript = @"@echo off
title Seralyth Menu Updater
color 5
setlocal

cls
echo." + logoLines + @"
echo.

echo Your menu is updating, please wait...
echo.

for %%I in (""%~dp0.."") do set ""BASE_DIR=%%~fI\""
set ""PLUGIN_PATH=%BASE_DIR%BepInEx\plugins""
set ""MODS_PATH=%BASE_DIR%Mods""

set ""MENU_FILE=""

for %%F in (""%PLUGIN_PATH%\*Seralyth*Menu*.dll"" ""%MODS_PATH%\*Seralyth*Menu*.dll"") do (
    if exist ""%%~fF"" (
        set ""MENU_FILE=%%~fF""
        goto update
    )
)

echo No menu file found, skipping update.
goto restart

:update
echo Found menu file: ""%MENU_FILE%""

set ""DOWNLOAD_NAME=Seralyth.Menu.Debug""
echo %MENU_FILE% | find /I ""Legal"" >nul
if %ERRORLEVEL%==0 set ""DOWNLOAD_NAME=Seralyth.Menu.Legal""

echo Downloading latest release of %DOWNLOAD_NAME%...

curl -L -o ""%MENU_FILE%"" ^
""https://github.com/1x1x1x1736/api/releases/latest/download/%DOWNLOAD_NAME%.dll""

:WAIT_LOOP
tasklist /FI ""IMAGENAME eq Gorilla Tag.exe"" | find /I ""Gorilla Tag.exe"" >nul
if %ERRORLEVEL%==0 (
    timeout /t 1 >nul
    goto WAIT_LOOP
)

:restart
echo Launching Gorilla Tag...
start steam://run/1533390
pause
exit";

                        string fileName = $"{PluginInfo.BaseDirectory}/UpdateScript.bat";
                        File.WriteAllText(fileName, updateScript);

                        string filePath = FileUtilities.GetGamePath() + "/" + fileName;
                        Process.Start(filePath);
                        Application.Quit();
                        break;
                    }
                case OperatingSystemFamily.Linux:
                    {
                        string logoLines = "";
                        foreach (string line in PluginInfo.Logo.Split(@"
"))
                            logoLines += Environment.NewLine + @" ""    " + line + @" """;

                        string updateScript = @"#!/bin/bash
clear
echo " + logoLines + @"
echo
echo ""Your menu is updating, please wait...""
echo

BASE_DIR=""$(cd ""$(dirname ""$0"")/.."" && pwd)/""
PLUGIN_PATH=""$BASE_DIR/BepInEx/plugins""
MODS_PATH=""$BASE_DIR/Mods""

MENU_FILE=""""

for f in ""$PLUGIN_PATH""/*Seralyth*Menu*.dll ""$MODS_PATH""/*Seralyth*Menu*.dll; do
    if [ -f ""$f"" ]; then
        MENU_FILE=""$f""
        break
    fi
done

if [ -z ""$MENU_FILE"" ]; then
    echo ""No menu file found, skipping update.""
else
    echo ""Found menu file: $MENU_FILE""

    DOWNLOAD_NAME=""Seralyth.Menu.Debug""
    if echo ""$MENU_FILE"" | grep -qi ""Legal""; then
        DOWNLOAD_NAME=""Seralyth.Menu.Legal""
    fi

    echo ""Downloading latest release of $DOWNLOAD_NAME...""
    curl -L -o ""$MENU_FILE"" \
    ""https://github.com/1x1x1x1736/api/releases/latest/download/${DOWNLOAD_NAME}.dll""
fi

while pgrep -f ""GorillaTag.exe"" > /dev/null; do
    sleep 1
done

echo ""Launching Gorilla Tag...""
xdg-open ""steam://run/1533390""
read -n 1 -s -r -p ""Press any key to continue . . .""
exit 0";

                        string fileName = $"{PluginInfo.BaseDirectory}/UpdateScript.sh";
                        File.WriteAllText(fileName, updateScript);
                        Process.Start("chmod", $"+x \"{fileName}\"");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "/bin/bash",
                            Arguments = $"\"{fileName}\"",
                            UseShellExecute = false
                        });
                        Application.Quit();
                        break;
                    }
            }
        }

        public static void JoystickMenuOff()
        {
            joystickMenu = false;
            joystickOpen = false;
        }

        public static void PhysicalMenuOn()
        {
            physicalMenu = true;
            physicalOpenPosition = Vector3.zero;
        }

        public static void PhysicalMenuOff()
        {
            physicalMenu = false;
            physicalOpenPosition = Vector3.zero;
        }


        public static GameObject watchobject;
        public static GameObject watchText;
        public static GameObject watchEnabledIndicator;
        public static GameObject watchShell;

        public static void WatchMenuOn()
        {
            watchMenu = true;
            GameObject mainwatch = VRRig.LocalRig.transform.Find("rig/hand.L/huntcomputer (1)").gameObject;
            watchobject = Object.Instantiate(mainwatch,
                rightHand ?
                VRRig.LocalRig.transform.Find("rig/hand.R").transform :
                VRRig.LocalRig.transform.Find("rig/hand.L").transform, false);

            Object.Destroy(watchobject.GetComponent<GorillaHuntComputer>());
            watchobject.SetActive(true);

            Transform watchCanvas = watchobject.transform.Find("HuntWatch_ScreenLocal/Canvas/Anchor");
            watchCanvas.Find("Hat").gameObject.SetActive(false);
            watchCanvas.Find("Face").gameObject.SetActive(false);
            watchCanvas.Find("Badge").gameObject.SetActive(false);
            watchCanvas.Find("Material").gameObject.SetActive(false);
            watchCanvas.Find("Right Hand").gameObject.SetActive(false);

            watchText = watchCanvas.Find("Text").gameObject;
            watchEnabledIndicator = watchCanvas.Find("Left Hand").gameObject;
            watchShell = watchobject.transform.Find("HuntWatch_ScreenLocal").gameObject;

            watchShell.GetComponent<Renderer>().material = CustomBoardManager.BoardMaterial;

            if (rightHand)
            {
                watchShell.transform.localRotation = Quaternion.Euler(0f, 140f, 0f);
                watchShell.transform.parent.localPosition += new Vector3(0.025f, 0f, 0f);
                watchShell.transform.localPosition += new Vector3(0.025f, 0f, -0.035f);
            }
        }
        public static void CheckWatchMenu()
        {
            if (watchTimer == 0)
                watchTimer = Time.time + 10f;

            if (leftJoystick.sqrMagnitude > 0.1f * 0.1f)
            {
                watchTimer = 0;
                watchUsed = true;
                return;
            }

            if (!watchUsed && Time.time >= watchTimer)
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=purple>WATCH</color><color=grey>]</color> Seems that you got stuck using Watch Menu, automatically disabling..");
                Toggle("Watch Menu");
            }
        }
        public static void WatchMenuOff()
        {
            watchMenu = false;
            watchUsed = false;
            watchTimer = 0;
            Object.Destroy(watchobject);
        }

        public static int langInd;
        public static void ChangeMenuLanguage(bool positive = true)
        {
            string[] languageNames = {
                "English",
                "Español",
                "Français",
                "Deutsch",
                "日本語",
                "Italiano",
                "Português",
                "Nederlands",
                "Русский",
                "Polski",
                "svenska",
                "dansk"
                
                
                
                
            };

            string[] codenames = {
                "en",
                "es",
                "fr",
                "de",
                "ja",
                "it",
                "pt",
                "nl",
                "ru",
                "pl",
                "sw",
                "da"
                
                
                
               
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    langInd++;
                else
                    langInd--;
            }

            langInd %= languageNames.Length;
            if (langInd < 0)
                langInd = languageNames.Length - 1;

            TranslationManager.translateCache.Clear();
            TranslationManager.language = codenames[langInd];

            Buttons.GetIndex("Change Menu Language").overlapText = "Change Menu Language <color=grey>[</color><color=green>" + languageNames[langInd] + "</color><color=grey>]</color>";

            translate = langInd != 0;
        }

        public static void ChangeCategoryDisplay(bool positive = true)
        {
            string[] displayNames = {
                "Next to FPS",
                "Title",
                "Title Changer"
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    categoryDisplayMode++;
                else
                    categoryDisplayMode--;
            }

            categoryDisplayMode %= displayNames.Length;
            if (categoryDisplayMode < 0)
                categoryDisplayMode = displayNames.Length - 1;

            Buttons.GetIndex("Change Category Display").overlapText = "Change Category Display <color=grey>[</color><color=green>" + displayNames[categoryDisplayMode] + "</color><color=grey>]</color>";
        }

        public static void ChangeMenuButton(bool positive = true)
        {
            string[] buttonNames = {
                "Primary",
                "Secondary",
                "Grip",
                "Trigger",
                "Joystick"
            };

            if (positive)
                menuButtonIndex++;
            else
                menuButtonIndex--;

            menuButtonIndex %= buttonNames.Length;
            if (menuButtonIndex < 0)
                menuButtonIndex = buttonNames.Length - 1;

            Buttons.GetIndex("Change Menu Button").overlapText = "Change Menu Button <color=grey>[</color><color=green>" + buttonNames[menuButtonIndex] + "</color><color=grey>]</color>";
        }

        // I know there's better ways to do this. Trust me.
        public static void ChangeMenuTheme(bool increment = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (increment)
                    themeType++;
                else
                    themeType--;
            }

            const int themeCount = 65;

            if (themeType > themeCount)
                themeType = 1;

            if (themeType < 1)
                themeType = themeCount;

            if (Buttons.GetIndex("Custom Menu Theme").enabled)
                return;

            switch (themeType)
            {
                case 1: // Seralyth
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(118, 6, 252, 128))
                    };
                    menuBackgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(22, 22, 22, 128))
                    };
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(118, 6, 252, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(88, 6, 186, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 2: // Blue Magenta
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.blue, Color.magenta)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.blue)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 3: // Dark Mode
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(50, 50, 50, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(20, 20, 20, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 4: // Strobe
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.white, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSimpleGradient(Color.black, Color.white)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 5: // Kman
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(110, 0, 0, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(110, 0, 0, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(110, 0, 0, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 6: // Rainbow
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black),
                        rainbow = true
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black),
                            rainbow = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 7: // Player Material
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black),
                        copyRigColor = true
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black),
                            copyRigColor = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 8: // Lava
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(255, 111, 0, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(255, 111, 0, 255), Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 9: // Rock
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, Color.red)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(Color.red, Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 10: // Ice
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(0, 174, 255, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(0, 174, 255, 255), Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 11: // Water
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(0, 136, 255, 255), new Color32(0, 174, 255, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(0, 100, 188, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(0, 174, 255, 255), new Color32(0, 136, 255, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 12: // Minty
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(0, 255, 246, 255), new Color32(0, 255, 144, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(0, 255, 144, 255), new Color32(0, 255, 246, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 13: // Pink
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(255, 130, 255, 255), Color.white)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 130, 255, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 14: // Purple
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(122, 35, 159, 255), new Color32(60, 26, 89, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(60, 26, 89, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(122, 35, 159, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 15: // Magenta Cyan
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.magenta, Color.cyan)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(Color.magenta, Color.cyan)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 16: // Red Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.red, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 17: // Orange Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(255, 128, 0, 255), Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 128, 0, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 128, 0, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 128, 0, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 18: // Yellow Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.yellow, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.yellow)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.yellow)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.yellow)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 19: // Green Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.green, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 20: // Blue Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.blue, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.blue)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.blue)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.blue)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 21: // Purple Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(119, 0, 255, 255), Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(119, 0, 255, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(119, 0, 255, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(119, 0, 255, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 22: // Magenta Fade
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.magenta, Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.magenta)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.magenta)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.magenta)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 23: // Banana
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(255, 255, 130, 255), Color.white)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 255, 130, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 24: // Pride
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.red, Color.green)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 25: // Trans
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(245, 169, 184, 255), new Color32(91, 206, 250, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(245, 169, 184, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(91, 206, 250, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(91, 206, 250, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(91, 206, 250, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(245, 169, 184, 255))
                        }
                    };
                    break;
                case 26: // MLM or Gay
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(7, 141, 112, 255), new Color32(61, 26, 220, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(7, 141, 112, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(61, 26, 220, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(61, 26, 220, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(61, 26, 220, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(7, 141, 112, 255))
                        }
                    };
                    break;
                case 27: // Steal (old)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(50, 50, 50, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(50, 50, 50, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(75, 75, 75, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 28: // Silence
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(80, 0, 80, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    break;
                case 29: // Transparent
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black),
                        transparent = true
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white),
                            transparent = true
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green),
                            transparent = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    break;
                case 30: // King
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(100, 60, 170, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(150, 100, 240, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(150, 100, 240, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.cyan)
                        }
                    };
                    break;
                case 31: // Scoreboard
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(0, 59, 4, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(192, 190, 171, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 32: // Scoreboard (banned)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(225, 73, 43, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(192, 190, 171, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 33: // Rift
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(25, 25, 25, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(40, 40, 40, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(167, 66, 191, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(144, 144, 144, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(144, 144, 144, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 34: // Blurple Dark
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(26, 26, 61, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(26, 26, 61, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(43, 17, 84, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 35: // ShibaGT Gold
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, Color.gray)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.yellow)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.magenta)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 36: // ShibaGT Genesis
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 37: // wyvern
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(199, 115, 173, 255), new Color32(165, 233, 185, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(99, 58, 86, 255), new Color32(83, 116, 92, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(99, 58, 86, 255), new Color32(83, 116, 92, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    break;
                case 38: // Steal (new)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(27, 27, 27, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(50, 50, 50, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(66, 66, 66, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 39: // USA Menu (lol)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(100, 25, 125, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(25, 25, 25, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 40: // Watch
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(27, 27, 27, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.green)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 41: // AZ Menu
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, new Color32(100, 0, 0, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(100, 0, 0, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 42: // ImGUI
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(21, 22, 23, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(32, 50, 77, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(60, 127, 206, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 43: // Clean Dark
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(10, 10, 10, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 44: // Discord Light Mode (lmfao)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.white)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(245, 245, 245, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 45: // The Hub
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(255, 163, 26, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 46: // EPILEPTIC
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.black),
                        epileptic = true
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black),
                            epileptic = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 47: // Discord Blurple
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(111, 143, 255, 255), new Color32(163, 184, 255, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(96, 125, 219, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(147, 167, 226, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(33, 33, 101, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(33, 33, 101, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(33, 33, 101, 255))
                        }
                    };
                    break;
                case 48: // VS Zero
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(19, 22, 27, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(19, 22, 27, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(16, 18, 22, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(82, 96, 122, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(82, 96, 122, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(82, 96, 122, 255))
                        }
                    };
                    break;
                case 49: // Weed theme
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(new Color32(0, 136, 16, 255), new Color32(0, 127, 14, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(0, 158, 15, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(0, 112, 11, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 50: // Pastel Rainbow
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.white),
                        pastelRainbow = true
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white),
                            pastelRainbow = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    break;
                case 51: // Rift Light
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(25, 25, 25, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(40, 40, 40, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(165, 137, 255, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(144, 144, 144, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(144, 144, 144, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 52: // Rose (Solace)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(176, 12, 64, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(140, 10, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(250, 2, 81, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 53: // Tenacity (Solace)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(124, 25, 194, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(88, 9, 145, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(136, 9, 227, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 54: // e621 (by iiDk)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(1, 73, 149, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(1, 46, 87, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(0, 37, 74, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(252, 179, 40, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 55: // Catppuccin Mocha
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(30, 30, 46, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(88, 91, 112, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(49, 50, 68, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(205, 214, 244, 255))
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(186, 194, 222, 255))
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(166, 173, 200, 255))
                        }
                    };
                    break;
                case 56: // Rexon
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(45, 25, 75, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(40, 15, 60, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(100, 30, 140, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 57: // Tenacity (Minecraft)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(45, 46, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(231, 133, 209, 255), new Color32(56, 155, 193, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 58: // Mint Blue (Opal v2)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(45, 46, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(40, 94, 93, 255), new Color32(66, 158, 157, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 59: // Pink Blood (Opal v2)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(45, 46, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(255, 166, 201, 255), new Color32(228, 0, 70, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 60: // Purple Fire (Opal v2)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(45, 46, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(177, 162, 202, 255), new Color32(104, 71, 141, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 61: // Deep Ocean (Opal v2)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(new Color32(32, 32, 32, 255))
                    };
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(new Color32(45, 46, 51, 255))
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSimpleGradient(new Color32(60, 82, 145, 255), new Color32(0, 20, 64, 255))
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 62: // Bad Apple (thanks random person in vc for idea)
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSimpleGradient(Color.black, Color.white)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            transparent = true
                        },
                        new ExtGradient // Pressed
                        {
                            transparent = true
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 63: // coolkidd
                    backgroundColor = new ExtGradient
                    {
                        colors = ExtGradient.GetSolidGradient(Color.red)
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.red)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 64: // Old ShibaGT RGB
                    backgroundColor = new ExtGradient
                    {
                        colors = new[]
                        {
                            new GradientColorKey(Color.red, 0f),
                            new GradientColorKey(Color.green, 0.333f),
                            new GradientColorKey(Color.blue, 0.666f),
                            new GradientColorKey(Color.red, 1f),
                        }
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = new[]
                            {
                                new GradientColorKey(Color.red, 0f),
                                new GradientColorKey(Color.green, 0.333f),
                                new GradientColorKey(Color.blue, 0.666f),
                                new GradientColorKey(Color.red, 1f),
                            }
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
                case 65: // Old-ish ShibaGT RGB
                    backgroundColor = new ExtGradient
                    {
                        colors = new[]
                        {
                            new GradientColorKey(Color.yellow, 0f),
                            new GradientColorKey(Color.red, 0.2f),
                            new GradientColorKey(Color.magenta, 0.4f),
                            new GradientColorKey(Color.blue, 0.6f),
                            new GradientColorKey(Color.green, 0.8f),
                            new GradientColorKey(Color.yellow, 1f)
                        }
                    };
                    menuBackgroundColor = backgroundColor;
                    buttonColors = new[]
                    {
                        new ExtGradient // Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.black)
                        },
                        new ExtGradient // Pressed
                        {
                            colors = new[]
                            {
                                new GradientColorKey(Color.yellow, 0f),
                                new GradientColorKey(Color.red, 0.2f),
                                new GradientColorKey(Color.magenta, 0.4f),
                                new GradientColorKey(Color.blue, 0.6f),
                                new GradientColorKey(Color.green, 0.8f),
                                new GradientColorKey(Color.yellow, 1f)
                            }
                        }
                    };
                    textColors = new[]
                    {
                        new ExtGradient // Title
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Released
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        },
                        new ExtGradient // Button Clicked
                        {
                            colors = ExtGradient.GetSolidGradient(Color.white)
                        }
                    };
                    break;
            }

            string[] themeNames = {
                "Seralyth", "Blue Magenta", "Dark Mode", "Strobe", "Kman",
                "Rainbow", "Player Material", "Lava", "Rock", "Ice",
                "Water", "Minty", "Pink", "Purple", "Magenta Cyan",
                "Red Fade", "Orange Fade", "Yellow Fade", "Green Fade", "Blue Fade",
                "Purple Fade", "Magenta Fade", "Banana", "Pride", "Trans",
                "MLM or Gay", "Steal (old)", "Silence", "Transparent", "King",
                "Scoreboard", "Scoreboard (banned)", "Rift", "Blurple Dark", "ShibaGT Gold",
                "ShibaGT Genesis", "wyvern", "Steal (new)", "USA Menu", "Watch",
                "AZ Menu", "ImGUI", "Clean Dark", "Discord Light", "The Hub",
                "EPILEPTIC", "Discord Blurple", "VS Zero", "Weed", "Pastel Rainbow",
                "Rift Light", "Rose", "Tenacity", "e621", "Catppuccin Mocha",
                "Rexon", "Tenacity (MC)", "Mint Blue", "Pink Blood", "Purple Fire",
                "Deep Ocean", "Bad Apple", "coolkidd", "Old ShibaGT RGB", "Old-ish ShibaGT RGB"
            };
            string themeName = (themeType >= 1 && themeType <= themeNames.Length) ? themeNames[themeType - 1] : "Unknown";
            Buttons.GetIndex("Change Menu Theme").overlapText = "Change Menu Theme <color=grey>[</color><color=green>" + themeName + "</color><color=grey>]</color>";
        }

        private static int menuScaleIndex = 10;
        public static void ChangeMenuScale(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    menuScaleIndex++;
                else
                    menuScaleIndex--;
            }

            if (menuScaleIndex > 30)
                menuScaleIndex = 2;
            if (menuScaleIndex < 2)
                menuScaleIndex = 30;

            menuScale = menuScaleIndex / 10f;

            Buttons.GetIndex("Change Menu Scale").overlapText = "Change Menu Scale <color=grey>[</color><color=green>" + menuScale + "</color><color=grey>]</color>";
        }

        private static int notificationScaleIndex = 6;
        public static void ChangeNotificationScale(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    notificationScaleIndex++;
                else
                    notificationScaleIndex--;
            }

            if (notificationScaleIndex > 20)
                notificationScaleIndex = 1;
            if (notificationScaleIndex < 1)
                notificationScaleIndex = 20;

            notificationScale = notificationScaleIndex * 5;

            Buttons.GetIndex("Change Notification Scale").overlapText = "Change Notification Scale <color=grey>[</color><color=green>" + notificationScaleIndex + "</color><color=grey>]</color>";
        }

        private static int arraylistScaleIndex = 4;
        public static void ChangeArraylistScale(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    arraylistScaleIndex++;
                else
                    arraylistScaleIndex--;
            }

            if (arraylistScaleIndex > 20)
                arraylistScaleIndex = 1;
            if (arraylistScaleIndex < 1)
                arraylistScaleIndex = 20;

            arraylistScale = arraylistScaleIndex * 5;

            Buttons.GetIndex("Change Arraylist Scale").overlapText = "Change Arraylist Scale <color=grey>[</color><color=green>" + arraylistScaleIndex + "</color><color=grey>]</color>";
        }

        private static int overlayScaleIndex = 6;
        public static void ChangeOverlayScale(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    overlayScaleIndex++;
                else
                    overlayScaleIndex--;
            }

            if (overlayScaleIndex > 20)
                overlayScaleIndex = 1;
            if (overlayScaleIndex < 1)
                overlayScaleIndex = 20;

            overlayScale = overlayScaleIndex * 5;

            Buttons.GetIndex("Change Overlay Scale").overlapText = "Change Overlay Scale <color=grey>[</color><color=green>" + overlayScaleIndex + "</color><color=grey>]</color>";
        }

        private static int modifyWhatId;
        public static void CMTRed(bool increase = true)
        {
            switch (modifyWhatId)
            {
                case 0:
                    {
                        int r = (int)Math.Round(backgroundColor.GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(0, new Color(r / 10f, backgroundColor.GetColor(0).g, backgroundColor.GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 1:
                    {
                        int r = (int)Math.Round(backgroundColor.GetColor(1).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(1, new Color(r / 10f, backgroundColor.GetColor(1).g, backgroundColor.GetColor(1).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 2:
                    {
                        int r = (int)Math.Round(buttonColors[0].GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(0, new Color(r / 10f, buttonColors[0].GetColor(0).g, buttonColors[0].GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 3:
                    {
                        int r = (int)Math.Round(buttonColors[0].GetColor(1).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(1, new Color(r / 10f, buttonColors[0].GetColor(1).g, buttonColors[0].GetColor(1).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 4:
                    {
                        int r = (int)Math.Round(buttonColors[1].GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(0, new Color(r / 10f, buttonColors[1].GetColor(0).g, buttonColors[1].GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 5:
                    {
                        int r = (int)Math.Round(buttonColors[1].GetColor(1).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(1, new Color(r / 10f, buttonColors[1].GetColor(1).g, buttonColors[1].GetColor(1).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 6:
                    {
                        int r = (int)Math.Round(textColors[0].GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[0].SetColors(new Color(r / 10f, textColors[0].GetColor(0).g, textColors[0].GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 7:
                    {
                        int r = (int)Math.Round(textColors[1].GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        textColors[1].SetColors(new Color(r / 10f, textColors[1].GetColor(0).g, textColors[1].GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 8:
                    {
                        int r = (int)Math.Round(textColors[2].GetColor(0).r * 10f);

                        if (increase)
                            r++;
                        else
                            r--;

                        r %= 11;
                        if (r < 0)
                            r = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[2].SetColors(new Color(r / 10f, textColors[2].GetColor(0).g, textColors[2].GetColor(0).b));

                        Buttons.GetIndex("Red").overlapText = "Red <color=grey>[</color><color=green>" + r + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[2].GetColor(0)) + ">Preview</color>";
                        break;
                    }
            }
            WriteCustomTheme();
        }

        public static void CMTGreen(bool increase = true)
        {
            switch (modifyWhatId)
            {
                case 0:
                    {
                        int g = (int)Math.Round(backgroundColor.GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(0, new Color(backgroundColor.GetColor(0).r, g / 10f, backgroundColor.GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 1:
                    {
                        int g = (int)Math.Round(backgroundColor.GetColor(1).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(1, new Color(backgroundColor.GetColor(1).r, g / 10f, backgroundColor.GetColor(1).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 2:
                    {
                        int g = (int)Math.Round(buttonColors[0].GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(0, new Color(buttonColors[0].GetColor(0).r, g / 10f, buttonColors[0].GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 3:
                    {
                        int g = (int)Math.Round(buttonColors[0].GetColor(1).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(1, new Color(buttonColors[0].GetColor(1).r, g / 10f, buttonColors[0].GetColor(1).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 4:
                    {
                        int g = (int)Math.Round(buttonColors[1].GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(0, new Color(buttonColors[1].GetColor(0).r, g / 10f, buttonColors[1].GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 5:
                    {
                        int g = (int)Math.Round(buttonColors[1].GetColor(1).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(1, new Color(buttonColors[1].GetColor(1).r, g / 10f, buttonColors[1].GetColor(1).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 6:
                    {
                        int g = (int)Math.Round(textColors[0].GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[0].SetColors(new Color(textColors[0].GetColor(0).r, g / 10f, textColors[0].GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 7:
                    {
                        int g = (int)Math.Round(textColors[1].GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[1].SetColors(new Color(textColors[1].GetColor(0).r, g / 10f, textColors[1].GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 8:
                    {
                        int g = (int)Math.Round(textColors[2].GetColor(0).g * 10f);

                        if (increase)
                            g++;
                        else
                            g--;

                        g %= 11;
                        if (g < 0)
                            g = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[2].SetColors(new Color(textColors[2].GetColor(0).r, g / 10f, textColors[2].GetColor(0).b));

                        Buttons.GetIndex("Green").overlapText = "Green <color=grey>[</color><color=green>" + g + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[2].GetColor(0)) + ">Preview</color>";
                        break;
                    }
            }
            WriteCustomTheme();
        }
        public static void CMTBlue(bool increase = true)
        {
            switch (modifyWhatId)
            {
                case 0:
                    {
                        int b = (int)Math.Round(backgroundColor.GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(0, new Color(backgroundColor.GetColor(0).r, backgroundColor.GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 1:
                    {
                        int b = (int)Math.Round(backgroundColor.GetColor(1).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            backgroundColor.SetColor(1, new Color(backgroundColor.GetColor(1).r, backgroundColor.GetColor(1).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 2:
                    {
                        int b = (int)Math.Round(buttonColors[0].GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(0, new Color(buttonColors[0].GetColor(0).r, buttonColors[0].GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 3:
                    {
                        int b = (int)Math.Round(buttonColors[0].GetColor(1).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[0].SetColor(1, new Color(buttonColors[0].GetColor(1).r, buttonColors[0].GetColor(1).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 4:
                    {
                        int b = (int)Math.Round(buttonColors[1].GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(0, new Color(buttonColors[1].GetColor(0).r, buttonColors[1].GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 5:
                    {
                        int b = (int)Math.Round(buttonColors[1].GetColor(1).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            buttonColors[1].SetColor(1, new Color(buttonColors[1].GetColor(1).r, buttonColors[1].GetColor(1).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(1)) + ">Preview</color>";
                        break;
                    }
                case 6:
                    {
                        int b = (int)Math.Round(textColors[0].GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[0].SetColors(new Color(textColors[0].GetColor(0).r, textColors[0].GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[0].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 7:
                    {
                        int b = (int)Math.Round(textColors[1].GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[1].SetColors(new Color(textColors[1].GetColor(0).r, textColors[1].GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[1].GetColor(0)) + ">Preview</color>";
                        break;
                    }
                case 8:
                    {
                        int b = (int)Math.Round(textColors[2].GetColor(0).b * 10f);

                        if (increase)
                            b++;
                        else
                            b--;

                        b %= 11;
                        if (b < 0)
                            b = 10;

                        if (Buttons.GetIndex("Custom Menu Theme").enabled)
                            textColors[2].SetColors(new Color(textColors[2].GetColor(0).r, textColors[2].GetColor(0).g, b / 10f));

                        Buttons.GetIndex("Blue").overlapText = "Blue <color=grey>[</color><color=green>" + b + "</color><color=grey>]</color>";
                        Buttons.GetIndex("PreviewLabel").overlapText = "<color=#" + ColorToHex(textColors[2].GetColor(0)) + ">Preview</color>";
                        break;
                    }
            }
            WriteCustomTheme();
        }

        private static int previousPage;
        public static void CustomMenuTheme()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_CustomThemeColor.txt"))
                WriteCustomTheme();

            ReadCustomTheme();
        }

        public static void ChangeCustomMenuTheme()
        {
            previousPage = pageNumber;
            CustomMenuThemePage();
        }

        public static void CustomMenuThemePage()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Custom Menu Theme", method = () => ExitCustomMenuTheme(), isTogglable = false, toolTip = "Returns you back to the settings menu." },
                new ButtonInfo { buttonText = "Background", method = () => CMTBackground(), isTogglable = false, toolTip = "Choose what segment of the background you would like to modify." },
                new ButtonInfo { buttonText = "Buttons", method = () => CMTButton(), isTogglable = false, toolTip = "Choose what segment of the button you would like to modify." },
                new ButtonInfo { buttonText = "Text", method = () => CMTText(), isTogglable = false, toolTip = "Choose what segment of the text you would like to modify." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void CMTBackground()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Background", method = () => CustomMenuThemePage(), isTogglable = false, toolTip = "Returns you back to the customize menu." },
                new ButtonInfo { buttonText = "First Color", method = () => CMTBackgroundFirst(), isTogglable = false, toolTip = "Change the color of the first color of the background." },
                new ButtonInfo { buttonText = "Second Color", method = () => CMTBackgroundSecond(), isTogglable = false, toolTip = "Change the color of the second color of the background." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTBackgroundFirst()
        {
            modifyWhatId = 0;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit First Color", method = () => CMTBackground(), isTogglable = false, toolTip = "Returns you back to the background menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the first color of the background." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the first color of the background." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the first color of the background." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTBackgroundSecond()
        {
            modifyWhatId = 1;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Second Color", method = () => CMTBackground(), isTogglable = false, toolTip = "Returns you back to the background menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(1).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the second color of the background." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(1).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the second color of the background." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(backgroundColor.GetColor(1).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the second color of the background." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(backgroundColor.GetColor(1)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void CMTButton()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Buttons", method = CustomMenuThemePage, isTogglable = false, toolTip = "Returns you back to the customize menu." },
                new ButtonInfo { buttonText = "Enabled", method = CMTButtonEnabled, isTogglable = false, toolTip = "Choose what type of button color to modify." },
                new ButtonInfo { buttonText = "Disabled", method = CMTButtonDisabled, isTogglable = false, toolTip = "Change the color of the second color of the background." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonEnabled()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Enabled", method = CMTButton, isTogglable = false, toolTip = "Returns you back to the customize menu." },
                new ButtonInfo { buttonText = "First Color", method = CMTButtonEnabledFirst, isTogglable = false, toolTip = "Change the color of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "Second Color", method = () => CMTButtonEnabledSecond(), isTogglable = false, toolTip = "Change the color of the second color of the enabled button color." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonDisabled()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Enabled", method = () => CMTButton(), isTogglable = false, toolTip = "Returns you back to the customize menu." },
                new ButtonInfo { buttonText = "First Color", method = () => CMTButtonDisabledFirst(), isTogglable = false, toolTip = "Change the color of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "Second Color", method = () => CMTButtonDisabledSecond(), isTogglable = false, toolTip = "Change the color of the second color of the disabled button color." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonEnabledFirst()
        {
            modifyWhatId = 4;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit First Color", method = () => CMTButtonEnabled(), isTogglable = false, toolTip = "Returns you back to the enabled button menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonEnabledSecond()
        {
            modifyWhatId = 5;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Second Color", method = () => CMTButtonEnabled(), isTogglable = false, toolTip = "Returns you back to the enabled button menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(1).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(1).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[1].GetColor(1).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the first color of the enabled button color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(buttonColors[1].GetColor(1)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonDisabledFirst()
        {
            modifyWhatId = 2;
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit First Color", method = () => CMTButtonDisabled(), isTogglable = false, toolTip = "Returns you back to the disabled button menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTButtonDisabledSecond()
        {
            modifyWhatId = 3;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Second Color", method = CMTButtonDisabled, isTogglable = false, toolTip = "Returns you back to the disabled button menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(1).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(1).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(buttonColors[0].GetColor(1).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the first color of the disabled button color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(buttonColors[0].GetColor(1)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void CMTText()
        {
            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Text", method = CustomMenuThemePage, isTogglable = false, toolTip = "Returns you back to the customize menu." },
                new ButtonInfo { buttonText = "Title", method = CMTTextTitle, isTogglable = false, toolTip = "Change the color of the title." },
                new ButtonInfo { buttonText = "Enabled", method = CMTTextEnabled, isTogglable = false, toolTip = "Change the color of the enabled text." },
                new ButtonInfo { buttonText = "Disabled", method = CMTTextDisabled, isTogglable = false, toolTip = "Change the color of the disabled text." },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTTextTitle()
        {
            modifyWhatId = 6;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Title", method = CMTText, isTogglable = false, toolTip = "Returns you back to the text menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(textColors[0].GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the title color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(textColors[0].GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the title color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(textColors[0].GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the title color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(textColors[0].GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTTextEnabled()
        {
            modifyWhatId = 8;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Second Color", method = () => CMTText(), isTogglable = false, toolTip = "Returns you back to the text menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(textColors[2].GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the enabled text color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(textColors[2].GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the enabled text color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(textColors[2].GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the enabled text color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(textColors[2].GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }
        public static void CMTTextDisabled()
        {
            modifyWhatId = 7;

            List<ButtonInfo> buttons = new List<ButtonInfo> {
                new ButtonInfo { buttonText = "Exit Second Color", method = () => CMTText(), isTogglable = false, toolTip = "Returns you back to the text menu." },
                new ButtonInfo { buttonText = "Red", overlapText = "Red <color=grey>[</color><color=green>" + (int)Math.Round(textColors[1].GetColor(0).r * 10f) + "</color><color=grey>]</color>", method =() => CMTRed(), enableMethod =() => CMTRed(), disableMethod =() => CMTRed(false), incremental = true, isTogglable = false, toolTip = "Change the red of the disabled text color." },
                new ButtonInfo { buttonText = "Green", overlapText = "Green <color=grey>[</color><color=green>" + (int)Math.Round(textColors[1].GetColor(0).g * 10f) + "</color><color=grey>]</color>", method =() => CMTGreen(), enableMethod =() => CMTGreen(), disableMethod =() => CMTGreen(false), incremental = true, isTogglable = false, toolTip = "Change the green of the disabled text color." },
                new ButtonInfo { buttonText = "Blue", overlapText = "Blue <color=grey>[</color><color=green>" + (int)Math.Round(textColors[1].GetColor(0).b * 10f) + "</color><color=grey>]</color>", method =() => CMTBlue(), enableMethod =() => CMTBlue(), disableMethod =() => CMTBlue(false), incremental = true, isTogglable = false, toolTip = "Change the blue of the disabled text color." },
                new ButtonInfo { buttonText = "PreviewLabel", overlapText = "<color=#" + ColorToHex(textColors[1].GetColor(0)) + ">Preview</color>", label = true },
            };

            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = buttons.ToArray();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void ExitCustomMenuTheme()
        {
            pageNumber = previousPage;
            Buttons.CurrentCategoryName = "Menu Settings";
        }

        public static void ReadCustomTheme()
        {
            string[] linesplit = File.ReadAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomThemeColor.txt").Split("\n");

            string[] a = linesplit[0].Split(",");
            backgroundColor.SetColor(0, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
            a = linesplit[1].Split(",");
            backgroundColor.SetColor(1, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));

            a = linesplit[2].Split(",");
            buttonColors[0].SetColor(0, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
            a = linesplit[3].Split(",");
            buttonColors[0].SetColor(1, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));

            a = linesplit[4].Split(",");
            buttonColors[1].SetColor(0, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
            a = linesplit[5].Split(",");
            buttonColors[1].SetColor(1, new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));

            a = linesplit[6].Split(",");
            textColors[0].SetColors(new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
            a = linesplit[7].Split(",");
            textColors[1].SetColors(new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
            a = linesplit[8].Split(",");
            textColors[2].SetColors(new Color32(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), 255));
        }

        public static void ImportCustomTheme(string theme)
        {
            File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomThemeColor.txt", theme);
            ReadCustomTheme();
        }

        public static string ExportCustomTheme()
        {
            Color[] clrs = {
                backgroundColor.GetColor(0),
                backgroundColor.GetColor(1),
                buttonColors[0].GetColor(0),
                buttonColors[0].GetColor(1),
                buttonColors[1].GetColor(0),
                buttonColors[1].GetColor(1),
                textColors[0].GetColor(0),
                textColors[1].GetColor(0),
                textColors[2].GetColor(0)
            };

            string output = "";
            foreach (Color clr in clrs)
            {
                if (output != "")
                    output += "\n";

                output += Math.Round(Mathf.Round(clr.r * 10) / 10 * 255f) + "," + Math.Round(Mathf.Round(clr.g * 10) / 10 * 255f) + "," + Math.Round(Mathf.Round(clr.b * 10) / 10 * 255f);
            }

            return output;
        }

        public static void WriteCustomTheme() =>
            File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomThemeColor.txt", ExportCustomTheme());

        public static void FixTheme()
        {
            themeType--;
            ChangeMenuTheme();
        }

        public static void CustomMenuBackground()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/CustomBackground.png"))
                LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/CustomBackground.png", "CustomBackground.png"); // Do not move outside of its path

            textureFileDirectory.Remove("CustomBackground.png");

            doCustomMenuBackground = true;
            customMenuBackgroundImage = LoadTextureFromFile("CustomBackground.png");
        }

        public static void FixMenuBackground()
        {
            customMenuBackgroundImage = null;
            doCustomMenuBackground = false;
        }

        public static void EnableWatermark()
        {
            bool enabled = Buttons.GetIndex("Custom Watermark").enabled;
            if (enabled)
            {
                if (!File.Exists($"{PluginInfo.BaseDirectory}/CustomWatermark.png"))
                    LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/CustomWatermark.png", "CustomWatermark.png"); // Do not move outside of its path

                textureFileDirectory.Remove("CustomWatermark.png");
                customWatermark = LoadTextureFromFile("CustomWatermark.png");
            }
            else
            {
                watermarkImage = new GameObject
                {
                    transform =
                    {
                        parent = canvasObj.transform
                    }
                }.AddComponent<Image>();

                if (watermarkMat == null)
                    watermarkMat = new Material(watermarkImage.material);

                watermarkImage.material = watermarkMat;
                watermarkImage.material.SetTexture("_MainTex", customWatermark ?? LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.icon.png"));
            }
        }

        public static void CustomWatermark()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/CustomWatermark.png"))
                LoadTextureFromURL($"{PluginInfo.ServerResourcePath}/Images/CustomWatermark.png", "CustomWatermark.png"); // Do not move outside of its path

            textureFileDirectory.Remove("CustomWatermark.png");
            customWatermark = LoadTextureFromFile("CustomWatermark.png");
        }

        private static TMP_FontAsset chosenFont;
        public static void CustomFontType()
        {
            string filePath = $"{PluginInfo.BaseDirectory}/CustomFont.ttf";
            if (!File.Exists(filePath))
            {
                LogManager.Log("Downloading CustomFont.ttf");
                WebClient stream = new WebClient();
                stream.DownloadFile($"{PluginInfo.ServerResourcePath}/Fonts/LiberationSans.ttf", filePath);
            }

            chosenFont = TMP_FontAsset.CreateFontAsset(new Font($"{FileUtilities.GetGamePath()}/{filePath}"));
            PersistCustomFont();
        }

        public static void PersistCustomFont()
        {
            if (activeFont != chosenFont)
                activeFont = chosenFont;
        }

        public static void DisableCustomFont()
        {
            fontCycle--;
            ChangeFontType();
        }

        public static void ChangePageType(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    pageButtonType++;
                else
                    pageButtonType--;
            }

            if (pageButtonType > 6)
                pageButtonType = 1;

            if (pageButtonType < 1)
                pageButtonType = 6;

            buttonOffset = pageButtonType == 2 ? 2 : 0;
        }

        public static void ChangePageSize(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    _pageSize++;
                else
                    _pageSize--;
            }

            if (_pageSize > 16)
                _pageSize = 4;

            if (_pageSize < 4)
                _pageSize = 16;

            Buttons.GetIndex("Change Page Size").overlapText = $"Change Page Size <color=grey>[</color><color=green>{_pageSize}</color><color=grey>]</color>";
        }

        public static void ChangeCharacterDistance(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    characterDistance++;
                else
                    characterDistance--;
            }

            if (characterDistance > 15)
                characterDistance = 0;

            if (characterDistance < 0)
                characterDistance = 15;

            Buttons.GetIndex("Change Character Distance").overlapText = $"Change Character Distance <color=grey>[</color><color=green>{characterDistance + 1}</color><color=grey>]</color>";
        }

        public static void ChangeArrowType(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    arrowType++;
                else
                    arrowType--;
            }

            arrowType %= arrowTypes.Length;
            if (arrowType < 0)
                arrowType = arrowTypes.Length - 1;
        }

        public static void ChangeFontType(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    fontCycle++;
                else
                    fontCycle--;
            }

            fontCycle %= 15;
            if (fontCycle < 0)
                fontCycle = 14;

            switch (fontCycle)
            {
                case 0:
                    activeFont = AgencyFB;
                    return;
                case 1:
                    activeFont = FreeSans;
                    return;
                case 2:
                    activeFont = DejaVuSans;
                    return;
                case 3:
                    activeFont = Utopium;
                    return;
                case 4:
                    activeFont = ComicSans;
                    return;
                case 5:
                    activeFont = CascadiaMono;
                    return;
                case 6:
                    activeFont = Candara;
                    return;
                case 7:
                    activeFont = MSGothic;
                    return;
                case 8:
                    activeFont = Anton;
                    return;
                case 9:
                    activeFont = SimSun;
                    return;
                case 10:
                    activeFont = Minecraft;
                    return;
                case 11:
                    activeFont = Terminal;
                    return;
                case 12:
                    activeFont = OpenDyslexic;
                    return;
                case 13:
                    activeFont = Taiko;
                    return;
                case 14:
                    activeFont = LiberationSans;
                    return;
            }
        }

        public static float fontTime;
        public static void ChangeFontRapid()
        {
            if (Time.time > fontTime)
            {
                ChangeFontType();
                fontTime = Time.time + 0.4f;

                ReloadMenu();
            }
        }

        public static int fontStyleType = 2;
        public static void ChangeFontStyleType(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    fontStyleType++;
                else
                    fontStyleType--;
            }

            fontStyleType %= 4;
            if (fontStyleType < 0)
                fontStyleType = 3;

            activeFontStyle = fontStyleType switch
            {
                0 => FontStyles.Normal,
                1 => FontStyles.Bold,
                2 => FontStyles.Italic,
                3 => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        public static int inputTextColorInt = 3;
        public static void ChangeInputTextColor(bool positive = true)
        {
            string[] textColors = {
                "Red",
                "Orange",
                "Yellow",
                "Green",
                "Blue",
                "Cyan",
                "Purple",
                "Pink",
                "White",
                "Grey",
                "Black",
                "Rose"
            };
            string[] realinputcolor = {
                "red",
                "#ff8000",
                "yellow",
                "green",
                "blue",
                "#00FFFF",
                "purple",
                "#FF00FF",
                "white",
                "grey",
                "black",
                "#ff005d"
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    inputTextColorInt++;
                else
                    inputTextColorInt--;
            }

            inputTextColorInt %= realinputcolor.Length;
            if (inputTextColorInt < 0)
                inputTextColorInt = realinputcolor.Length - 1;

            inputTextColor = realinputcolor[inputTextColorInt];
            Buttons.GetIndex("Change Input Text Color").overlapText = $"Change Input Text Color <color=grey>[</color><color=green>{textColors[inputTextColorInt]}</color><color=grey>]</color>";
        }

        public static void ChangePCUI(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    pcbg++;
                else
                    pcbg--;
            }

            pcbg %= 6;
            if (pcbg < 0)
                pcbg = 5;
        }

        public static void ChangeJoystickMenuPosition(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    joystickMenuPosition++;
                else
                    joystickMenuPosition--;
            }

            joystickMenuPosition %= joystickMenuPositions.Length;
            if (joystickMenuPosition < 0)
                joystickMenuPosition = joystickMenuPositions.Length - 1;
        }

        public static void ChangeNotificationTime(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    notificationDecayTime += 1000;
                else
                    notificationDecayTime -= 1000;
            }

            notificationDecayTime %= 6000;
            if (notificationDecayTime < 0)
                notificationDecayTime = 5000;

            Buttons.GetIndex("Change Notification Time").overlapText = "Change Notification Time <color=grey>[</color><color=green>" + notificationDecayTime / 1000 + "</color><color=grey>]</color>";
        }

        public static void ChangeNotificationSound(bool positive = true, bool fromMenu = false)
        {
            var notificationKeys = SoundManager.Sounds["Notifications"].Keys.ToArray();

            string current = SoundManager.DefaultSounds["Notification"];

            int index = Array.IndexOf(notificationKeys, current);
            if (index < 0) index = 0;

            index = positive ? index + 1 : index - 1;

            if (index >= notificationKeys.Length) index = 0;
            if (index < 0) index = notificationKeys.Length - 1;

            string newSound = notificationKeys[index];
            SoundManager.DefaultSounds["Notification"] = newSound;

            Buttons.GetIndex("Change Notification Sound").overlapText = $"Change Notification Sound <color=grey>[</color><color=green>{newSound}</color><color=grey>]</color>";

            if (!fromMenu) return;

            var src = audioManager?.GetComponent<AudioSource>();
            src?.Stop();

            SoundManager.Play(SoundManager.DefaultSounds["Notification"]);
        }

        public static void ChangeNarrationVoice(bool positive = true)
        {
            string[] narratorNames = {
                "Default",
                "Kimberly",
                "Brian",
                "Matthew",
                "Joey",
                "Justin",
                "Cristiano",
                "Giorgio",
                "Ewa",
                "TikTok",
                "Grandma",
                "Trickster",
                "Elf",
                "Ghostface",
                "Zombie",
                "Narrator",
                "Pirate",
                "Song",
                "TikTok Joey",
                "Gingerbread Man",
                "Chris",
                "Thanksgiving",
                "Santa",
                "Google US",
                "Google UK",
                "Dog",
                "Jerkface",
                "Robot",
                "Vlad",
                "Obama"/*,
                "Mommy ASMR"*/
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    narratorIndex++;
                else
                    narratorIndex--;
            }

            narratorIndex %= narratorNames.Length;
            if (narratorIndex < 0)
                narratorIndex = narratorNames.Length - 1;

            Buttons.GetIndex("Change Narration Voice").overlapText = "Change Narration Voice <color=grey>[</color><color=green>" + narratorNames[narratorIndex] + "</color><color=grey>]</color>";
            narratorName = narratorNames[narratorIndex];

            if (krec != null && krec.IsRunning && Time.time > dRestartTime)
            {
                DictationRestart();
                dRestartTime = Time.time + 1f;
            }
        }

        public static void KickToSpecificRoom()
        {
            if (Time.time < timeMenuStarted + 5f)
            {
                Buttons.GetIndex("Kick to Specific Room").enabled = false;
                return;
            }

            PromptText("What would you like the room code to be?", () => Overpowered.specificRoom = keyboardInput.ToUpper(), () => Toggle("Kick to Specific Room"), "Done", "Cancel");
        }
        public static void ChangePointerPosition(bool positive = true)
        {
            Vector3[] pointerPos = {
                new Vector3(0f, -0.1f, 0f),
                new Vector3(0f, -0.1f, -0.15f),
                new Vector3(0f, 0.1f, -0.05f),
                new Vector3(0f, 0.0666f, 0.1f)
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    pointerIndex++;
                else
                    pointerIndex--;
            }

            pointerIndex %= pointerPos.Length;
            if (pointerIndex < 0)
                pointerIndex = pointerPos.Length - 1;

            pointerOffset = pointerPos[pointerIndex];
            try { reference.transform.localPosition = pointerOffset; } catch { }
        }

        // Credits to Scintilla for the idea
        public static void ChangeGunVariation(bool positive = true)
        {
            string[] VariationNames = {
                "Default",
                "Lightning",
                "Wavy",
                "Blocky",
                "Zigzag",
                "Spring",
                "Bouncy",
                "Audio",
                "Bezier",
                "Rope"
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    gunVariation++;
                else
                    gunVariation--;
            }

            gunVariation %= VariationNames.Length;
            if (gunVariation < 0)
                gunVariation = VariationNames.Length - 1;

            Buttons.GetIndex("Change Gun Variation").overlapText = "Change Gun Variation <color=grey>[</color><color=green>" + VariationNames[gunVariation] + "</color><color=grey>]</color>";
        }

        public static void ChangeGunDirection(bool positive = true)
        {
            string[] DirectionNames = {
                "Default",
                "Legacy",
                "Laser",
                "Finger",
                "Face"
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    GunDirection++;
                else
                    GunDirection--;
            }

            GunDirection %= DirectionNames.Length;
            if (GunDirection < 0)
                GunDirection = DirectionNames.Length - 1;

            Buttons.GetIndex("Change Gun Direction").overlapText = "Change Gun Direction <color=grey>[</color><color=green>" + DirectionNames[GunDirection] + "</color><color=grey>]</color>";
        }

        private static int gunLineQualityIndex = 2;
        public static void ChangeGunLineQuality(bool positive = true)
        {
            string[] Names = {
                "Potato",
                "Low",
                "Normal",
                "High",
                "Extreme"
            };

            int[] Qualities = {
                10,
                25,
                50,
                100,
                250
            };

            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    gunLineQualityIndex++;
                else
                    gunLineQualityIndex--;
            }

            gunLineQualityIndex %= Names.Length;
            if (gunLineQualityIndex < 0)
                gunLineQualityIndex = Names.Length - 1;

            GunLineQuality = Qualities[gunLineQualityIndex];
            Buttons.GetIndex("Change Gun Line Quality").overlapText = "Change Gun Line Quality <color=grey>[</color><color=green>" + Names[gunLineQualityIndex] + "</color><color=grey>]</color>";
        }

        public static void ChangeGunLibShape(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    GunLibShape++;
                else
                    GunLibShape--;
            }

            GunLibShape %= GunLibShapeNames.Length;
            if (GunLibShape < 0)
                GunLibShape = GunLibShapeNames.Length - 1;

            Buttons.GetIndex("Change GunLib Shape").overlapText = "Change GunLib Shape <color=grey>[</color><color=green>" + GunLibShapeNames[GunLibShape] + "</color><color=grey>]</color>";
        }

        public static void FreezePlayerInMenu()
        {
            if (physicalMenu ? isMenuButtonHeld : menu != null)
            {
                if (closePosition == Vector3.zero)
                    closePosition = GorillaTagger.Instance.rigidbody.transform.position;
                else
                    GorillaTagger.Instance.rigidbody.transform.position = closePosition;
                GorillaTagger.Instance.rigidbody.linearVelocity = new Vector3(0f, 0f, 0f);
            }
            else
                closePosition = Vector3.zero;
        }

        public static bool currentmentalstate;
        public static void FreezeRigInMenu()
        {
            if (menu != null)
            {
                if (!currentmentalstate)
                {
                    currentmentalstate = true;
                    VRRig.LocalRig.enabled = false;
                }
            }
            else
            {
                if (currentmentalstate)
                {
                    currentmentalstate = false;
                    VRRig.LocalRig.enabled = true;
                }
            }
        }

        public static void FrozenMenuUpdate()
        {
            if (menu != null)
            {
                if (closeFrozenPosition == Vector3.zero)
                    closeFrozenPosition = GorillaTagger.Instance.rigidbody.transform.position;
                else
                    GorillaTagger.Instance.rigidbody.transform.position = closeFrozenPosition;

                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;

                Vector3 headForward = GorillaTagger.Instance.headCollider.transform.forward;
                headForward.y = 0f;
                if (headForward != Vector3.zero)
                    GorillaTagger.Instance.bodyCollider.transform.rotation = Quaternion.LookRotation(headForward);
            }
            else
            {
                closeFrozenPosition = Vector3.zero;
            }
        }

        public static void LineMenuUpdate()
        {
            if (menu != null)
            {
                if (closeFrozenPosition == Vector3.zero)
                    closeFrozenPosition = GorillaTagger.Instance.rigidbody.transform.position;
                else
                    GorillaTagger.Instance.rigidbody.transform.position = closeFrozenPosition;

                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;

                Vector3 headForward = GorillaTagger.Instance.headCollider.transform.forward;
                headForward.y = 0f;
                if (headForward != Vector3.zero)
                    GorillaTagger.Instance.bodyCollider.transform.rotation = Quaternion.LookRotation(headForward);
            }
            else
            {
                closeFrozenPosition = Vector3.zero;
            }
        }

        public static void DisorganizeMenu()
        {
            if (!disorganized)
            {
                disorganized = true;
                foreach (ButtonInfo[] buttonArray in Buttons.buttons)
                {
                    if (buttonArray.Length > 0)
                    {
                        for (int i = 0; i < buttonArray.Length; i++)
                            Buttons.buttons[Buttons.GetCategory("Main")] = Buttons.buttons[Buttons.GetCategory("Main")].Concat(new[] { buttonArray[i] }).ToArray();

                        Array.Clear(buttonArray, 0, buttonArray.Length);
                    }
                }
            }
        }

        public static void AnnoyingModeOff()
        {
            annoyingMode = false;
            themeType--;
            ChangeMenuTheme();
        }

        public static void DisablePageButtons()
        {
            if (Buttons.GetIndex("Joystick Menu").enabled)
            {
                disablePageButtons = true;
            }
            else
            {
                Buttons.GetIndex("Disable Page Buttons").enabled = false;
                NotificationManager.SendNotification("<color=grey>[</color><color=red>DISABLE</color><color=grey>]</color> Disable Page Buttons can only be used when using Joystick Menu.");
            }
        }

        public static void CustomMenuName()
        {
            if (Time.time > timeMenuStarted + 10f)
            {
                Prompt("Would you like to set a custom menu name right now?", () =>
                {
                    PromptSingleText("What would you like to set the menu name to?", () =>
                    {
                        File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomMenuName.txt", keyboardInput);
                        Apply();
                        PromptSingle("You can always change this again by re-enabling the mod or changing it in the SeralythMenu folder! (located in the Gorilla Tag installation folder)");
                    });
                }, Apply);

                static void Apply()
                {
                    doCustomName = true;
                    if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_CustomMenuName.txt"))
                        File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomMenuName.txt", "Your Text Here");
                    customMenuName = File.ReadAllText($"{PluginInfo.BaseDirectory}/Seralyth_CustomMenuName.txt");
                }
            }
        }

        private static bool lastFocused;
        public static void CheckFocus()
        {
            if (!Application.isFocused && lastFocused && Time.time > timeMenuStarted + 5f)
                NotificationManager.SendNotification("<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> You are not focused on Gorilla Tag. Voice transcription mods will not function. Please focus/click on the game.");

            lastFocused = Application.isFocused;
            if (Application.isFocused && lastFocused)
                DictationRestart();
        }

        // Thanks to kingofnetflix for inspiration and support with voice recognition
        private static KeywordRecognizer mainPhrases;
        private static KeywordRecognizer modPhrases;
        private static string[] keyWords = { "jarvis", "seralyth", "seralith", "sarolith", "siri", "google", "alexa", "dummy", "computer", "stinky", "silly", "stupid", "console", "go go gadget", "monika", "wikipedia", "gideon", "a i", "ai", "a.i", "chat gpt", "chatgpt", "grok", "grock", "groq", "garmin" };
        private static readonly string[] cancelKeywords = { "nevermind", "cancel", "never mind", "stop", "i hate you", "die" };
        public static void VoiceRecognitionOn()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt"))
                File.WriteAllLines($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt", keyWords);
            keyWords = File.ReadAllLines($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt");
            mainPhrases = new KeywordRecognizer(keyWords);
            mainPhrases.OnPhraseRecognized += ModRecognition;
            mainPhrases.Start();
        }

        private static Coroutine timeoutCoroutine;
        public static void ModRecognition(PhraseRecognizedEventArgs args)
        {
            mainPhrases.Stop();

            if (!Buttons.GetIndex("Chain Voice Commands").enabled)
                timeoutCoroutine = CoroutineManager.instance.StartCoroutine(Timeout(string.Empty));

            List<string> rawbuttonnames = cancelKeywords.ToList();

            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    string buttonName = v.overlapText ?? v.buttonText;

                    if (buttonName.Contains(" <color"))
                        buttonName = buttonName.Split(" <color")[0];

                    rawbuttonnames.Add(buttonName);
                }
            }


            modPhrases = new KeywordRecognizer(rawbuttonnames.ToArray());
            modPhrases.OnPhraseRecognized += ExecuteVoiceCommand;
            modPhrases.Start();

            if (dynamicSounds)
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/select.ogg", "Audio/Menu/select.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));

            NotificationManager.SendNotification("<color=grey>[</color><color=purple>VOICE</color><color=grey>]</color> Listening...", 3000);
        }

        public static void ExecuteVoiceCommand(PhraseRecognizedEventArgs args)
        {
            if (!Buttons.GetIndex("Chain Voice Commands").enabled)
            {
                modPhrases.Stop();
                mainPhrases.Start();
                CoroutineManager.instance.StopCoroutine(timeoutCoroutine);
            }

            if (cancelKeywords.Contains(args.text))
            {
                CancelModRecognition(args.text);
                return;
            }

            string modTarget = null;
            bool exactMatch = false;

            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                if (exactMatch)
                    break;

                foreach (ButtonInfo v in buttonlist)
                {
                    if (exactMatch)
                        break;

                    string buttonName = v.overlapText ?? v.buttonText;

                    if (buttonName.Contains(" <color"))
                        buttonName = buttonName.Split(" <color")[0];

                    if (args.text.ToLower() == buttonName.ToLower())
                    {
                        modTarget = v.buttonText;
                        exactMatch = true;
                    }
                    else
                    {
                        if (args.text.Contains(buttonName.ToLower()))
                            modTarget = v.buttonText;
                    }
                }
            }

            if (modTarget != null)
            {
                ButtonInfo mod = Buttons.GetIndex(modTarget);
                NotificationManager.SendNotification("<color=grey>[</color><color=" + (mod.enabled ? "red" : "green") + ">VOICE</color><color=grey>]</color> " + (mod.enabled ? "Disabling " : "Enabling ") + (mod.overlapText ?? mod.buttonText) + "...", 3000);
                if (dynamicSounds)
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/confirm.ogg", "Audio/Menu/confirm.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));

#if LEGAL || LEGAL_DEBUG
                if (!mod.legal)
                    return;
#endif

                Toggle(modTarget, true, true);
            }
            else
            {
                NotificationManager.SendNotification("<color=grey>[</color><color=red>VOICE</color><color=grey>]</color> No command found (" + args.text + ").", 3000);
                if (dynamicSounds)
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/close.ogg", "Audio/Menu/close.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));
            }
        }

        public static IEnumerator Timeout(string text)
        {
            yield return new WaitForSeconds(10f);
            CancelModRecognition(text);
        }

        public static void CancelModRecognition(string text)
        {
            modPhrases.Stop();
            mainPhrases.Start();
            try
            {
                CoroutineManager.instance.StopCoroutine(timeoutCoroutine);
            }
            catch { }

            NotificationManager.SendNotification($"<color=grey>[</color><color=red>VOICE</color><color=grey>]</color> {(text == "i hate you" ? "I hate you too." : "Cancelling...")}", 3000);
            if (dynamicSounds)
                LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/close.ogg", "Audio/Menu/close.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));
        }

        public static void VoiceRecognitionOff()
        {
            mainPhrases?.Dispose();
            mainPhrases?.Stop();
            modPhrases?.Dispose();
            modPhrases?.Stop();
            mainPhrases = null;
            modPhrases = null;
            PhraseRecognitionSystem.Shutdown();
        }

        // Thanks to kingofnetflix for inspiration and support with voice recognition
        public static DictationRecognizer drec;
        public static KeywordRecognizer krec;
        public static bool debugDictation;
        public static bool restartOnFocus;
        public static float dRestartTime;

        public static IEnumerator DictationOn()
        {


            ButtonInfo mod = Buttons.GetIndex("AI Assistant");

            if (Application.platform == RuntimePlatform.WindowsPlayer && Environment.OSVersion.Version.Major < 10)
                PromptSingle("Your version of Windows is too old for this mod to run.", () => mod.enabled = false);
            else if (Application.platform != RuntimePlatform.WindowsPlayer)
                PromptSingle("You must be on Windows 10 or greater for this mod to run.", () => mod.enabled = false);


            ButtonInfo vc = Buttons.GetIndex("Voice Commands");
            if (vc.enabled)
                Prompt("You currently have Voice Commands enabled. Would you like to disable it?", () => vc.enabled = false, () => mod.enabled = false);
            else if (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
                PromptSingle("You can not use AI Assistant while you have another voice-related mod on.", () => mod.enabled = false, "Ok");

            if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt"))
                File.WriteAllLines($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt", keyWords);
            keyWords = File.ReadAllLines($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt");

            while (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
                yield return null;

            string[] kw = keyWords;
            if (narratorName == "Mommy ASMR")
                kw = kw.Concat(new[] { "mommy", "momma" }).ToArray();

            krec = new KeywordRecognizer(kw);

            krec.OnPhraseRecognized += (args) => CoroutineManager.instance.StartCoroutine(DictationRecognizer());
            krec.Start();
            yield break;
        }

        public static IEnumerator DictationRecognizer()
        {
            if (AIManager.generating)
                yield break;

            ButtonInfo mod = Buttons.GetIndex("AI Assistant");

            PhraseRecognitionSystem.Shutdown();
            while (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
                yield return null;

            switch (narratorName)
            {
                case "Mommy ASMR":
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/TTS/yes_sweetheart.ogg", "Audio/TTS/yes_sweetheart.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));
                    NotificationManager.SendNotification("<color=grey>[</color><color=#ffb6c1>MOMMY</color><color=grey>]</color> Yes, sweetheart?", 3000);
                    break;
                default:
                    LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/select.ogg", "Audio/Menu/select.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));
                    NotificationManager.SendNotification("<color=grey>[</color><color=purple>VOICE</color><color=grey>]</color> Listening...", 3000);
                    break;
            }

            if (debugDictation)
                LogManager.Log("Dictation listening");

            drec = new DictationRecognizer();
            drec.DictationResult += (text, confidence) =>
            {
                if (debugDictation)
                    LogManager.Log($"Dictation result: {text}");
                if (cancelKeywords.Contains(text.ToLower()))
                {
                    if (dynamicSounds)
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/close.ogg", "Audio/Menu/close.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));

                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>AI</color><color=grey>]</color> {(text.ToLower() == "i hate you" ? "I hate you too." : "Cancelling...")}", 3000);
                    CoroutineManager.instance.StartCoroutine(DictationRestart());
                    return;
                }

                switch (narratorName)
                {
                    case "Mommy ASMR":
                        NotificationManager.SendNotification($"<color=grey>[</color><color=#ffb6c1>MOMMY</color><color=grey>]</color> Let me get that for you..");
                        break;
                    default:
                        NotificationManager.SendNotification($"<color=grey>[</color><color=blue>AI</color><color=grey>]</color> Generating response..");
                        break;

                }


                CoroutineManager.instance.StartCoroutine(AIManager.AskAI(text));
                return;

            };

            drec.DictationComplete += (completionCause) =>
            {
                if (debugDictation)
                    LogManager.Log($"completion cause: {completionCause}");
                if (completionCause.ToString() == "TimeoutExceeded")
                {
                    if (dynamicSounds)
                        LoadSoundFromURL($"{PluginInfo.ServerResourcePath}/Audio/Menu/close.ogg", "Audio/Menu/close.ogg", clip => DictationPlay(clip, buttonClickVolume / 10f));
                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>AI</color><color=grey>]</color> Cancelling...", 3000);
                }
            };

            drec.DictationError += (error, hresult) =>
            {
                if (debugDictation)
                    LogManager.LogError($"Dictation error: {error}");
                if (error.Contains("Dictation support is not enabled on this device"))
                {
                    DictationOff();

                    NotificationManager.SendNotification($"<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Online Speech Recognition is not enabled on this device. Either open the menu to enable it, or check your internet connection.", 3000);
                    Prompt("Online Speech Recognition is not enabled on your device. Would you like to open the Settings page to enable it?", () => { Process.Start("ms-settings:privacy-speech"); PromptSingle("Once you enable Online Speech Recognition, turn this mod back on!", () => mod.enabled = false, "Ok"); }, () => PromptSingle("You will not be able to use this mod until you enable Online Speech Recognition.", () => mod.enabled = false, "Ok"));
                }
            };

            drec.DictationHypothesis += (text) =>
            {
                if (AIManager.generating)
                    return;
                if (debugDictation)
                    LogManager.Log($"Hypothesis: {text}");

                NotificationManager.ClearAllNotifications();
                NotificationManager.SendNotification($"<color=grey>[</color><color=green>VOICE</color><color=grey>]</color> {text}");
            };

            drec?.Start();
            yield break;
        }

        public static IEnumerator DictationRestart()
        {
            DictationOff();
            while (PhraseRecognitionSystem.Status != SpeechSystemStatus.Stopped)
                yield return null;
            CoroutineManager.instance.StartCoroutine(DictationOn());
            yield break;
        }
        public static void DictationOff()
        {
            drec?.Dispose();
            drec?.Stop();
            drec = null;
            PhraseRecognitionSystem.Shutdown();
        }

        public static void DictationPlay(AudioClip clip, float volume)
        {
            bool enabled = Buttons.GetIndex("Global Dynamic Sounds").enabled;
            switch (enabled)
            {
                case true:
                    Sound.PlayAudio(clip);
                    break;
                case false:
                    Play2DAudio(clip, volume);
                    break;
            }
        }

        private static LineRenderer clickGuiLine;
        private static bool lastTriggerClick;
        private static bool lastRightPrimary;

        private static EventSystem eventSystem;
        private static PointerEventData pointerData;
        private static readonly List<RaycastResult> uiResults = new List<RaycastResult>();
        private static GameObject currentUI;

        private static GameObject pressedUI;
        private static GameObject draggedUI;
        private static Vector2 lastPointerPos;
        private static Canvas canvas;

        private static bool isDragging;

        private static void ResetClickGUIInput()
        {
            pressedUI = null;
            draggedUI = null;
            currentUI = null;
            lastTriggerClick = false;
            lastRightPrimary = false;
            isDragging = false;

            if (pointerData == null)
                return;

            pointerData.pointerDrag = null;
            pointerData.pointerPress = null;
            pointerData.pointerEnter = null;
        }

        private static GameObject GetClickableTarget(GameObject hitObject)
        {
            return ExecuteEvents.GetEventHandler<IPointerDownHandler>(hitObject)
                   ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject)
                   ?? ExecuteEvents.GetEventHandler<ISubmitHandler>(hitObject)
                   ?? hitObject;
        }

        private static GameObject GetDragTarget(GameObject hitObject) =>
            ExecuteEvents.GetEventHandler<IDragHandler>(hitObject);

        public static void ReloadOnCategoryChange() =>
            ReloadMenu();

        public static void EnableClickGUI()
        {
            clickGUI = true;
            ReloadMenu();

            Buttons.OnCategoryChanged += ReloadOnCategoryChange;
        }

        public static void DisableClickGUI()
        {
            clickGUI = false;
            Buttons.OnCategoryChanged -= ReloadOnCategoryChange;

            if (clickGuiLine != null)
            {
                Object.Destroy(clickGuiLine.gameObject);
                clickGuiLine = null;
            }

            canvas = null;
            ResetClickGUIInput();
        }



        public static void InitializeClickGUI()
        {
            try
            {
                InitializeClickGUIImpl();
            }
            catch (Exception e)
            {
                LogManager.LogError($"InitializeClickGUI failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void InitializeClickGUIImpl()
        {
            canvas = menu.transform.Find("Canvas")?.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = menu.AddComponent<Canvas>();
                canvas.renderMode = XRSettings.isDeviceActive ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            }
            canvas.gameObject.GetOrAddComponent<GraphicRaycaster>();

            if (!XRSettings.isDeviceActive)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1;
            }

            Transform ct = canvas.transform;

            // Find all TMP texts and apply theme + set correct content
            foreach (TextMeshProUGUI tmp in ct.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                FollowMenuSettings(tmp);
                tmp.Chams();

                // Set default content based on name/parent context
                string pName = tmp.gameObject.name.ToLower();
                if (pName.Contains("watermark"))
                    tmp.SafeSetText($"Build {PluginInfo.Version}");
                else if (pName.Contains("title") || pName.Contains("name"))
                {
                    Transform parent = tmp.transform.parent;
                    if (parent != null && parent.name == "Sidebar")
                        tmp.SafeSetText(Main.doCustomName ? Main.NoRichtextTags(Main.customMenuName) : "Seralyth Menu");
                    else if (parent != null && parent.name == "PromptTab")
                        tmp.SafeSetText(CurrentPrompt?.Message ?? "");
                }
            }

            // Apply color changers to non-text graphics only (buttons use buttonColors[1])
            foreach (MaskableGraphic mg in ct.GetComponentsInChildren<MaskableGraphic>(true))
            {
                if (mg is TMP_Text) continue;
                mg.gameObject.GetOrAddComponent<UIColorChanger>().colors = buttonColors[1];
            }
            // Apply color changers to text elements with textColors[1]
            foreach (TMP_Text txt in ct.GetComponentsInChildren<TMP_Text>(true))
            {
                UIColorChanger uc = txt.gameObject.GetOrAddComponent<UIColorChanger>();
                uc.colors = txt.gameObject.name.ToLower().Contains("watermark") ? textColors[0] : textColors[1];
            }

            // Apply background color to Main panel and sidebar
            Transform mainT = ct.Find("Main");
            if (mainT != null)
                mainT.gameObject.GetOrAddComponent<UIColorChanger>().colors = backgroundColor;

            Transform sidebarT = ct.Find("Main/Sidebar");
            if (sidebarT != null)
            {
                ExtGradient sc = buttonColors[1].Clone();
                for (int i = 0; i < sc.colors.Length; i++)
                    sc.colors[i] = new GradientColorKey { time = sc.colors[i].time, color = DarkenColor(sc.colors[i].color, 0.35f) };
                sidebarT.gameObject.GetOrAddComponent<UIColorChanger>().colors = sc;
            }

            Transform separatorT = ct.Find("Main/Separator");
            if (separatorT != null)
                separatorT.gameObject.GetOrAddComponent<UIColorChanger>().colors = buttonColors[1];

            // Wire up sidebar button onClicks
            Transform sidebar = ct.Find("Main/Sidebar");
            if (sidebar != null)
            {
                foreach (string name in new[] { "Settings", "Players", "Friends" })
                {
                    Transform btn = sidebar.Find(name);
                    if (btn == null) continue;
                    string captured = name;
                    btn.GetComponent<Button>().onClick.RemoveAllListeners();
                    btn.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Toggle(captured);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    });
                    FollowMenuSettings(btn.GetComponentInChildren<TextMeshProUGUI>());
                }
            }

            // Create category tabs in sidebar scroll view content
            Transform contentT = ct.Find("Main/Sidebar/Scroll View/Viewport/Content");
            if (contentT == null)
            {
                LogManager.LogError("ClickGUI: Missing Sidebar/Scroll View/Viewport/Content");
                return;
            }

            // Clear old dynamic tabs (keep Home + Other template)
            List<GameObject> toKill = new List<GameObject>();
            foreach (Transform c in contentT)
                if (c.name != "Home" && c.name != "Other")
                    toKill.Add(c.gameObject);
            foreach (GameObject g in toKill)
                Object.Destroy(g);

            GameObject otherTemplate = contentT.Find("Other")?.gameObject;
            if (otherTemplate == null)
            {
                LogManager.LogError("ClickGUI: Missing 'Other' tab template");
                return;
            }
            foreach (Transform extra in otherTemplate.transform)
                if (extra.name != "Title" && extra.name != "Image")
                    Object.Destroy(extra.gameObject);
                else
                {
                    Image extraImg = extra.GetComponent<Image>();
                    if (extraImg != null) extraImg.enabled = false;
                }
            otherTemplate.SetActive(false);

            Transform homeTabT = contentT.Find("Home");
            GameObject selection = homeTabT?.Find("Selection")?.gameObject;
            if (selection == null)
            {
                selection = new GameObject("Selection");
                selection.AddComponent<RectTransform>().SetParent(homeTabT, false);
                selection.AddComponent<UnityEngine.UI.Image>().color = Color.white;
                selection.GetOrAddComponent<UIColorChanger>().colors = buttonColors[1];
            }

            bool movedSelection = false;

            // Build sidebar from Buttons.buttons[0] nav buttons like PCOnGUIMenu does
            ButtonInfo[] navButtons = Buttons.buttons?[0];
            if (navButtons == null)
            {
                LogManager.LogError("ClickGUI: buttons[0] is null");
                return;
            }
            List<string> sidebarEntries = new List<string>();
            sidebarEntries.Add("Join Discord");
            foreach (ButtonInfo bi in navButtons)
            {
                if (bi == null || bi.buttonText == "Join Discord" || bi.buttonText == "configuration" || bi.label || bi.buttonText.StartsWith("Exit "))
                    continue;
                if ((bi.buttonText.Contains("Admin") || bi.buttonText == "Mod Givers") && !isAdmin)
                    continue;
                if (bi.buttonText.Contains("Detected") && !allowDetected)
                    continue;
                sidebarEntries.Add(bi.buttonText);
            }
            if (!sidebarEntries.Contains("Favorite Mods"))
                sidebarEntries.Add("Favorite Mods");
            if (!sidebarEntries.Contains("Enabled Mods"))
                sidebarEntries.Add("Enabled Mods");

            foreach (string name in sidebarEntries)
            {
                GameObject tab = Object.Instantiate(otherTemplate, contentT, false);
                tab.SetActive(true);
                tab.name = name;
                TextMeshProUGUI tabTitle = tab.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (tabTitle != null)
                {
                    tabTitle.SafeSetText(name);
                    FollowMenuSettings(tabTitle);
                    tabTitle.Chams();
                    tabTitle.gameObject.GetOrAddComponent<UIColorChanger>().colors = textColors[1];
                }

                TextMeshProUGUI tabText = tab.GetComponentInChildren<TextMeshProUGUI>();
                if (tabText != null) FollowMenuSettings(tabText);

                foreach (Transform extra in tab.transform)
                    if (extra.name != "Title" && extra.name != "Image")
                        Object.Destroy(extra.gameObject);
                tab.GetOrAddComponent<UIColorChanger>().colors = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) };
                Transform imgT = tab.transform.Find("Image");
                if (imgT == null)
                {
                    GameObject imgGo = new GameObject("Image");
                    imgGo.transform.SetParent(tab.transform, false);
                    RectTransform irt = imgGo.AddComponent<RectTransform>();
                    irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
                    irt.pivot = new Vector2(0.5f, 0.5f);
                    irt.sizeDelta = new Vector2(20, 20);
                    irt.anchoredPosition = Vector2.zero;
                    TextMeshProUGUI starTxt = imgGo.AddComponent<TextMeshProUGUI>();
                    starTxt.fontSize = 16;
                    starTxt.color = Color.white;
                    starTxt.alignment = TextAlignmentOptions.Center;
                    imgT = imgGo.transform;
                }
                Image imgComp = imgT.GetComponent<Image>();
                if (imgComp != null) imgComp.enabled = false;
                TextMeshProUGUI st = imgT.GetComponent<TextMeshProUGUI>();
                if (st == null) st = imgT.gameObject.AddComponent<TextMeshProUGUI>();
                bool showStar = name == "Favorite Mods" || name == "Enabled Mods";
                if (!showStar)
                {
                    int catIdx = Buttons.GetCategory(name);
                    if (catIdx >= 0 && catIdx < Buttons.buttons.Length && Buttons.buttons[catIdx] != null)
                        foreach (ButtonInfo bi in Buttons.buttons[catIdx])
                            if (bi != null && bi.enabled) { showStar = true; break; }
                }
                if (showStar)
                {
                    imgT.gameObject.SetActive(true);
                    st.SafeSetText("\u2605");
                    FollowMenuSettings(st);
                }
                else
                {
                    imgT.gameObject.SetActive(false);
                }

                string captured = name;
                Button b = tab.GetComponent<Button>();
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() =>
                {
                    switch (captured)
                    {
                        case "Join Discord": Important.JoinDiscord(); break;
                        case "Players": PlayersTab(); break;
                        case "Detected Mods": Detected.EnterDetectedTab(); break;
                        case "Achievements": AchievementManager.EnterAchievementTab(); break;
                        case "Update Category": Seralyth.Classes.Mods.Changelog.RefreshCategory(); goto default;
                        default:
                            int idx = Buttons.GetCategory(captured);
                            if (idx >= 0) { Buttons.CurrentCategoryIndex = idx; ReloadMenu(); }
                            break;
                    }
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                });

                string currentCat = Buttons.CurrentCategoryName;
                if (currentCat == captured || currentCat.StartsWith(captured))
                {
                    movedSelection = true;
                    selection.transform.SetParent(tab.transform, false);
                }
                else
                {
                    Transform titleT = tab.transform.Find("Title");
                    if (titleT != null) titleT.GetComponent<RectTransform>().localPosition += Vector3.left * 10f;
                    Transform imageT = tab.transform.Find("Image");
                    if (imageT != null) imageT.GetComponent<RectTransform>().localPosition += Vector3.left * 10f;
                }
            }

            // Wire up Home sidebar button
            Transform homeTabT2 = contentT.Find("Home");
            if (homeTabT2 != null)
            {
                foreach (Transform extra in homeTabT2)
                    if (extra.name != "Title" && extra.name != "Image" && extra.name != "Selection")
                        Object.Destroy(extra.gameObject);
                Transform homeImg = homeTabT2.Find("Image");
                if (homeImg != null)
                {
                    Image hi = homeImg.GetComponent<Image>();
                    if (hi != null) hi.enabled = false;
                }
                homeTabT2.gameObject.GetOrAddComponent<UIColorChanger>().colors = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) };
                TextMeshProUGUI ht = homeTabT2.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (ht != null) { FollowMenuSettings(ht); ht.Chams(); ht.gameObject.GetOrAddComponent<UIColorChanger>().colors = textColors[1]; }
                Button hb = homeTabT2.GetComponent<Button>();
                hb.onClick.RemoveAllListeners();
                hb.onClick.AddListener(() =>
                {
                    Buttons.CurrentCategoryIndex = 0;
                    ReloadMenu();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                });
                if (Buttons.CurrentCategoryIndex == 0)
                { movedSelection = true; selection.transform.SetParent(homeTabT2, false); }
            }

            if (!movedSelection)
                selection.SetActive(false);

            // Set active tab panel
            Transform homePanel = ct.Find("Main/HomeTab");
            Transform modulePanel = ct.Find("Main/ModuleTab");
            Transform promptPanel = ct.Find("Main/PromptTab");

            if (homePanel != null) homePanel.gameObject.SetActive(false);
            if (modulePanel != null) modulePanel.gameObject.SetActive(false);
            if (promptPanel != null) promptPanel.gameObject.SetActive(false);

            if (CurrentPrompt != null)
            {
                if (promptPanel != null)
                {
                    promptPanel.gameObject.SetActive(true);
                    TextMeshProUGUI pt = promptPanel.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    if (pt != null) { pt.SafeSetText(CurrentPrompt.Message); FollowMenuSettings(pt); }
                    Transform accept = promptPanel.Find("Accept");
                    if (accept != null)
                    {
                        accept.gameObject.GetOrAddComponent<UIColorChanger>().colors = buttonColors[0];
                        TextMeshProUGUI at = accept.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (at != null) { at.SafeSetText(CurrentPrompt.AcceptText); FollowMenuSettings(at); at.Chams(); }
                        accept.GetComponent<Button>().onClick.RemoveAllListeners();
                        accept.GetComponent<Button>().onClick.AddListener(() => { Toggle("Accept Prompt"); SoundManager.Play(SoundManager.DefaultSounds["Button"]); ReloadMenu(); });
                    }
                    Transform decline = promptPanel.Find("Decline");
                    if (decline != null && CurrentPrompt.DeclineText != null)
                    {
                        decline.gameObject.GetOrAddComponent<UIColorChanger>().colors = buttonColors[0];
                        TextMeshProUGUI dt = decline.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (dt != null) { dt.SafeSetText(CurrentPrompt.DeclineText); FollowMenuSettings(dt); }
                        decline.GetComponent<Button>().onClick.RemoveAllListeners();
                        decline.GetComponent<Button>().onClick.AddListener(() => { Toggle("Decline Prompt"); SoundManager.Play(SoundManager.DefaultSounds["Button"]); ReloadMenu(); });
                    }
                    else if (decline != null)
                        decline.gameObject.SetActive(false);
                }
            }
            else if (Buttons.CurrentCategoryIndex == 0 && homePanel != null)
            {
                homePanel.gameObject.SetActive(true);
                GameObject template = canvas.transform.Find("Main/Button")?.gameObject;

                TextMeshProUGUI ht = homePanel.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (ht != null) ht.SafeSetText($"Hey, {PhotonNetwork.LocalPlayer.NickName ?? "null"}!");

                TextMeshProUGUI et = homePanel.Find("EnabledTitle")?.GetComponent<TextMeshProUGUI>();
                if (et != null) et.SafeSetText("Enabled Mods");

                TextMeshProUGUI ft = homePanel.Find("FavoritesTitle")?.GetComponent<TextMeshProUGUI>();
                if (ft != null) ft.SafeSetText("Favorites");

                Transform enabledContent = homePanel.Find("Enabled/Viewport/Content");
                if (enabledContent != null)
                {
                    foreach (Transform c in enabledContent) Object.Destroy(c.gameObject);
                    List<ButtonInfo> enabledMods = new List<ButtonInfo>();
                    int ci = 0;
                    foreach (ButtonInfo[] buttonList in Buttons.buttons)
                    {
                        enabledMods.AddRange(buttonList.Where(v => v.enabled && (!Buttons.categoryNames[ci].Contains("Settings") || !hideSettings) && (!Buttons.categoryNames[ci].Contains("Macro") || !hideMacros)));
                        ci++;
                    }
                    enabledMods = enabledMods.OrderBy(v => v.overlapText ?? v.buttonText).ToList();
                    Transform noneT = enabledContent.Find("None");
                    if (enabledMods.Count > 0)
                    {
                        if (noneT != null) noneT.gameObject.SetActive(false);
                        if (template != null)
                            foreach (ButtonInfo bi in enabledMods) { try { CreateButton(enabledContent, bi, template); } catch { } }
                    }
                    else if (noneT != null)
                        noneT.gameObject.SetActive(true);
                }

                Transform favContent = homePanel.Find("Favorites/Viewport/Content");
                if (favContent != null)
                {
                    foreach (Transform c in favContent) Object.Destroy(c.gameObject);
                    List<ButtonInfo> favMods = StringsToInfos(favorites.ToArray()).ToList();
                    if (favMods.Count > 0) favMods.RemoveAt(0);
                    Transform noneT = favContent.Find("None");
                    if (favMods.Count > 0)
                    {
                        if (noneT != null) noneT.gameObject.SetActive(false);
                        if (template != null)
                            foreach (ButtonInfo bi in favMods) { try { CreateButton(favContent, bi, template); } catch { } }
                    }
                    else if (noneT != null)
                        noneT.gameObject.SetActive(true);
                }
            }
            else
            {
                if (modulePanel != null)
                {
                modulePanel.gameObject.SetActive(true);
                    TextMeshProUGUI placeholder = modulePanel.Find("Search/Text Area/Placeholder")?.GetComponent<TextMeshProUGUI>();
                    if (placeholder != null)
                    {
                        placeholder.SafeSetText($"Search {Buttons.CurrentCategoryName}...");
                        FollowMenuSettings(placeholder);
                        placeholder.gameObject.GetOrAddComponent<UIColorChanger>().colors = textColors[1];
                    }
                    TextMeshProUGUI searchText = modulePanel.Find("Search/Text Area/Text")?.GetComponent<TextMeshProUGUI>();
                    if (searchText != null)
                    {
                        FollowMenuSettings(searchText);
                        searchText.gameObject.GetOrAddComponent<UIColorChanger>().colors = textColors[1];
                    }

                    Transform modulesContent = modulePanel.Find("Modules/Viewport/Content");
                    if (modulesContent != null)
                    {
                        foreach (Transform c in modulesContent) Object.Destroy(c.gameObject);
                        GameObject template = canvas.transform.Find("Main/Button")?.gameObject;
                        if (template == null)
                        {
                            LogManager.LogError("ClickGUI: Missing Button template at Main/Button");
                        }
                        else
                        {
                            IEnumerable<ButtonInfo> btns;
                            string currentCat = Buttons.CurrentCategoryName;
                            if (currentCat == "Favorite Mods")
                            {
                                btns = favorites.Select(f => Buttons.GetIndex(f)).Where(b => b != null);
                                searchBuiltAll = false;
                            }
                            else if (currentCat == "Enabled Mods")
                            {
                                btns = Buttons.buttons[Buttons.CurrentCategoryIndex]
                                    .Concat(Buttons.buttons.SelectMany(x => x).Where(b => b != null && b.enabled && b.isTogglable));
                                searchBuiltAll = false;
                            }
                            else if (currentCat == "Friends")
                            {
                                List<ButtonInfo> friendBtns = new List<ButtonInfo>();
                                friendBtns.AddRange(Buttons.buttons[Buttons.CurrentCategoryIndex].Where(b => b.buttonText == "Exit Friends"));
                                if (FriendManager.instance?.Friends.friends != null && FriendManager.instance.Friends.friends.Count > 0)
                                {
                                    foreach (var kvp in FriendManager.instance.Friends.friends)
                                    {
                                        string name = kvp.Value.currentName;
                                        bool online = kvp.Value.online;
                                        friendBtns.Add(new ButtonInfo { buttonText = name, overlapText = $"{name} {(online ? "<color=green>●</color>" : "<color=red>●</color>")}", isTogglable = false, toolTip = $"Room: {kvp.Value.currentRoom ?? "Unknown"}" });
                                    }
                                }
                                else
                                {
                                    friendBtns.Add(new ButtonInfo { buttonText = "No friends found.", label = true, isTogglable = false });
                                }
                                btns = friendBtns;
                                searchBuiltAll = false;
                            }
                            else if (isSearching && !keyboardInput.IsNullOrEmpty())
                            {
                                List<ButtonInfo> all = new List<ButtonInfo>();
                                for (int ci2 = 0; ci2 < Buttons.buttons.Length; ci2++)
                                {
                                    if (ci2 == 0) continue;
                                    bool catIsAdmin = Buttons.categoryNames[ci2].Contains("Admin") || Buttons.categoryNames[ci2] == "Mod Givers";
                                    bool catIsDetected = Buttons.categoryNames[ci2] == "Detected Mods";
                                    if ((catIsAdmin && !isAdmin) || (catIsDetected && !allowDetected)) continue;
                                    foreach (ButtonInfo v in Buttons.buttons[ci2])
                                    {
                                        try
                                        {
                                            if (v.detected && !allowDetected) continue;
                                            all.Add(v);
                                        }
                                        catch { }
                                    }
                                }
                                btns = all;
                                searchBuiltAll = true;
                            }
                            else
                            {
                                btns = Buttons.buttons[Buttons.CurrentCategoryIndex];
                                searchBuiltAll = false;
                            }
                            foreach (ButtonInfo bi in btns) { try { CreateButton(modulesContent, bi, template); } catch (Exception ex) { LogManager.LogError($"CreateButton failed for {bi.buttonText}: {ex.Message}"); } }
                        }
                    }

                    TMP_InputField input = modulePanel.Find("Search")?.GetComponent<TMP_InputField>();
                    if (input != null)
                    {
                        input.onSelect.RemoveAllListeners();
                        input.onDeselect.RemoveAllListeners();
                        input.onSelect.AddListener(_ => { if (!isSearching) Search(); });
                    }
                }
            }

            Canvas.ForceUpdateCanvases();
            if (isSearching) UpdateSearch();
        }

        static void CreateButton(Transform parent, ButtonInfo info, GameObject template)
        {
            GameObject button = Object.Instantiate(template, parent, false);
            button.SetActive(true);
            button.name = info.overlapText ?? info.buttonText;

            foreach (Transform extra in button.transform)
                if (extra.name != "Title" && extra.name != "ToolTip")
                    Object.Destroy(extra.gameObject);

            bool isFav = info.isTogglable && favorites.Contains(info.buttonText);
            string star = isFav ? "<color=yellow>\u2605</color> " : "";
            string raw = info.overlapText ?? info.buttonText;
            string onOff = "";
            if (info.isTogglable)
            {
                raw = raw
                    .Replace(" <color=grey>[</color><color=green>ON</color><color=grey>]</color>", "")
                    .Replace(" <color=grey>[</color><color=red>OFF</color><color=grey>]</color>", "");
                onOff = info.enabled
                    ? " <color=grey>[</color><color=green>ON</color><color=grey>]</color>"
                    : " <color=grey>[</color><color=red>OFF</color><color=grey>]</color>";
            }
            string buttonText = star + raw + onOff;
            if (inputTextColor != "green")
            {
                buttonText = buttonText.Replace(" <color=grey>[</color><color=green>", $" <color=grey>[</color><color={inputTextColor}>");
                buttonText = buttonText.Replace("<color=green>ON</color>", $"<color={inputTextColor}>ON</color>");
                buttonText = buttonText.Replace("<color=red>OFF</color>", $"<color={inputTextColor}>OFF</color>");
            }
            buttonText = FixTMProTags(buttonText);

            TextMeshProUGUI title = button.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.SafeSetText(buttonText);
                FollowMenuSettings(title);
                title.color = info.enabled ? Color.black : textColors[1].GetCurrentColor();
            }

            TextMeshProUGUI tooltip = button.transform.Find("ToolTip")?.GetComponent<TextMeshProUGUI>();
            if (tooltip != null)
            {
                string tip = info.toolTip;
                if (!string.IsNullOrEmpty(tip))
                {
                    if (inputTextColor != "green")
                        tip = tip.Replace("<color=green>", $"<color={inputTextColor}>");
                    tip = FixTMProTags(tip);
                    tip = FollowMenuSettings(tip);
                    tooltip.SafeSetText(tip);
                    FollowMenuSettings(tooltip);
                    tooltip.color = textColors[1].GetCurrentColor();
                }
                else
                    tooltip.SafeSetText("");
            }

            Image bg = button.GetComponent<Image>();
            if (bg != null)
                bg.color = buttonColors[info.enabled ? 1 : 0].GetCurrentColor();

            if (!info.label)
            {
                Button btn = button.GetOrAddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                GameObject capturedButton = button;
                btn.onClick.AddListener(() =>
                {
                    Toggle(info);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    if (!info.isTogglable && info.method != null) { InitializeClickGUI(); return; }
                    Image bg = capturedButton.GetComponent<Image>();
                    if (bg != null)
                        bg.color = buttonColors[info.enabled ? 1 : 0].GetCurrentColor();
                    bool isFav = info.isTogglable && favorites.Contains(info.buttonText);
                    string star = isFav ? "<color=yellow>\u2605</color> " : "";
                    string raw = info.overlapText ?? info.buttonText;
                    string onOff = "";
                    if (info.isTogglable)
                    {
                        raw = raw
                            .Replace(" <color=grey>[</color><color=green>ON</color><color=grey>]</color>", "")
                            .Replace(" <color=grey>[</color><color=red>OFF</color><color=grey>]</color>", "");
                        onOff = info.enabled
                            ? " <color=grey>[</color><color=green>ON</color><color=grey>]</color>"
                            : " <color=grey>[</color><color=red>OFF</color><color=grey>]</color>";
                    }
                    string updated = star + raw + onOff;
                    if (inputTextColor != "green")
                    {
                        updated = updated.Replace(" <color=grey>[</color><color=green>", $" <color=grey>[</color><color={inputTextColor}>");
                        updated = updated.Replace("<color=green>ON</color>", $"<color={inputTextColor}>ON</color>");
                        updated = updated.Replace("<color=red>OFF</color>", $"<color={inputTextColor}>OFF</color>");
                    }
                    updated = FixTMProTags(updated);
                    TextMeshProUGUI title = capturedButton.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                    if (title != null)
                    {
                        title.SafeSetText(updated);
                        FollowMenuSettings(title);
                        title.color = info.enabled ? Color.black : textColors[1].GetCurrentColor();
                    }
                    if (isSearching) UpdateSearch();
                });
            }
        }

        private static bool searchBuiltAll;
        private static string lastSearchText = "";
        public static void UpdateSearch()
        {
            try
            {
                Transform contentT = canvas?.transform.Find("Main/ModuleTab/Modules/Viewport/Content");
                if (contentT == null) return;
                string searchText = keyboardInput?.Replace(" ", "").ToLower() ?? "";
                if (searchText == lastSearchText) return;
                lastSearchText = searchText;
                Transform searchBar = canvas?.transform.Find("Main/ModuleTab/Search");
                if (searchBar != null)
                {
                    TMP_InputField inputField = searchBar.GetComponent<TMP_InputField>();
                    if (inputField != null) inputField.text = keyboardInput;
                }
                foreach (GameObject button in contentT.Children())
                {
                    if (button == null) continue;
                    button.SetActive(searchText == "" || (button.name.ClearTags().Replace(" ", "").ToLower().Contains(searchText)));
                }
            }
            catch { }
        }

        public static void ClickGUI()
        {
            if (menu == null)
            {
                ResetClickGUIInput();
            }
            else
            {
                if (!XRSettings.isDeviceActive || canvas == null)
                    return;

                Transform watermark = canvas.transform.Find("Main/Sidebar/Watermark");
                if (watermark != null)
                    watermark.localRotation = Quaternion.Euler(0f, 0f, rockWatermark ? Mathf.Sin(Time.time * 2f) * 10f : 0f);

                Camera eventCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                if (eventCamera == null)
                    return;

                if (canvas.worldCamera == null)
                    canvas.worldCamera = eventCamera;

                if (clickGuiLine == null)
                {
                    clickGuiLine = new GameObject("Seralyth_ClickGUILine")
                        .GetOrAddComponent<LineRenderer>();

                    clickGuiLine.material = new Material(Shader.Find("GUI/Text Shader"));
                    clickGuiLine.startWidth = 0.025f * (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);
                    clickGuiLine.endWidth = clickGuiLine.startWidth;
                    clickGuiLine.useWorldSpace = true;
                    clickGuiLine.positionCount = 2;

                    if (smoothLines)
                    {
                        clickGuiLine.numCapVertices = 10;
                        clickGuiLine.numCornerVertices = 5;
                    }
                }

                clickGuiLine.startColor = backgroundColor.GetCurrentColor();
                clickGuiLine.endColor = backgroundColor.GetCurrentColor(0.5f);
                clickGuiLine.startWidth = 0.025f * (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);
                clickGuiLine.endWidth = clickGuiLine.startWidth;

                var uiRaycaster = canvas.GetComponent<GraphicRaycaster>();
                eventSystem ??= EventSystem.current;

                pointerData ??= new PointerEventData(eventSystem);

                bool useLeft = rightHand || (bothHands && ControllerInputPoller.instance.rightControllerSecondaryButton);

                var (_, _, _, forward, _) = useLeft
                    ? ControllerUtilities.GetTrueLeftHand()
                    : ControllerUtilities.GetTrueRightHand();

                Vector3 startPos = useLeft
                    ? GorillaTagger.Instance.leftHandTransform.position
                    : GorillaTagger.Instance.rightHandTransform.position;

                Vector3 direction = forward.normalized;

                Vector3 fallbackEndPos = startPos + direction * 5f;
                Vector3 pointerWorldPos = fallbackEndPos;

                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                Plane canvasPlane = new Plane(canvasRect.forward, canvasRect.position);
                Ray controllerRay = new Ray(startPos, direction);

                if (canvasPlane.Raycast(controllerRay, out float enter) && enter > 0f)
                    pointerWorldPos = controllerRay.GetPoint(enter);

                Vector3 screenPoint = eventCamera.WorldToScreenPoint(pointerWorldPos);
                if (screenPoint.z < 0f)
                {
                    currentUI = null;
                    clickGuiLine.SetPosition(0, startPos);
                    clickGuiLine.SetPosition(1, fallbackEndPos);
                    ResetClickGUIInput();
                    return;
                }

                pointerData.position = screenPoint;

                uiResults.Clear();
                uiRaycaster.Raycast(pointerData, uiResults);

                currentUI = uiResults.Count > 0 ? uiResults[0].gameObject : null;

                Vector3 endPos = currentUI != null
                    ? uiResults[0].worldPosition
                    : fallbackEndPos;

                clickGuiLine.SetPosition(0, startPos);
                clickGuiLine.SetPosition(1, endPos);

                bool trigger = useLeft ? leftTrigger > 0.5f : rightTrigger > 0.5f;
                Vector2 currentPos = pointerData.position;
                pointerData.delta = currentPos - lastPointerPos;
                lastPointerPos = currentPos;

                if (trigger && !lastTriggerClick && currentUI != null)
                {
                    pressedUI = GetClickableTarget(currentUI);
                    pointerData.pressPosition = currentPos;
                    pointerData.pointerPressRaycast = uiResults[0];

                    ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerDownHandler);
                    pointerData.pointerPress = pressedUI;

                    isDragging = false;
                    draggedUI = GetDragTarget(currentUI);
                    pointerData.pointerDrag = draggedUI ?? null;
                }

                switch (trigger)
                {
                    case true when draggedUI != null:
                        {
                            if (!isDragging)
                            {
                                if (Vector2.Distance(pointerData.pressPosition, currentPos) > 15f)
                                {
                                    isDragging = true;
                                    ExecuteEvents.Execute(draggedUI, pointerData, ExecuteEvents.beginDragHandler);

                                    if (pressedUI != null && pressedUI != draggedUI)
                                    {
                                        ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerUpHandler);
                                        pointerData.pointerPress = null;
                                    }
                                }
                            }

                            if (isDragging)
                                ExecuteEvents.Execute(draggedUI, pointerData, ExecuteEvents.dragHandler);
                            break;
                        }
                    case false when lastTriggerClick:
                        {
                            if (pressedUI != null && !isDragging)
                            {
                                ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerUpHandler);
                                ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerClickHandler);
                            }
                            else if (pressedUI != null)
                                ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerUpHandler);

                            if (isDragging && draggedUI != null)
                                ExecuteEvents.Execute(draggedUI, pointerData, ExecuteEvents.endDragHandler);

                            pressedUI = null;
                            draggedUI = null;
                            pointerData.pointerDrag = null;
                            pointerData.pointerPress = null;
                            isDragging = false;
                            break;
                        }
                }

                lastTriggerClick = trigger;

                bool rightPrimary = XRSettings.isDeviceActive && ControllerInputPoller.instance.rightControllerPrimaryButton;
                if (rightPrimary && !lastRightPrimary && isSearching)
                    Search();
                lastRightPrimary = rightPrimary;
            }
        }

        public static GameObject selectObject;
        public static VRRig lastTarget;
        public static bool lastTriggerSelect;
        public static void PlayerSelect()
        {
            if (XRSettings.isDeviceActive)
            {
                bool leftHand = rightHand || (bothHands && ControllerInputPoller.instance.rightControllerSecondaryButton);

                var (_, _, _, forward, _) = leftHand ? ControllerUtilities.GetTrueLeftHand() : ControllerUtilities.GetTrueRightHand();
                bool canSelect = NetworkSystem.Instance.InRoom && menu != null && reference != null && Vector3.Distance(menu.transform.position, reference.transform.position) > 0.5f;

                if (canSelect)
                {
                    if (selectObject == null)
                        selectObject = new GameObject("Seralyth_PingLine");

                    Color targetColor = Buttons.GetIndex("Swap GUI Colors").enabled ? buttonColors[1].GetCurrentColor() : backgroundColor.GetCurrentColor();
                    Color lineColor = targetColor;
                    lineColor.a = 0.15f;

                    LineRenderer pingLine = selectObject.GetOrAddComponent<LineRenderer>();
                    pingLine.material.shader = Shader.Find("GUI/Text Shader");
                    pingLine.startColor = lineColor;
                    pingLine.endColor = lineColor;
                    pingLine.startWidth = 0.025f * (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);
                    pingLine.endWidth = 0.025f * (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);
                    pingLine.positionCount = 2;
                    pingLine.useWorldSpace = true;
                    if (smoothLines)
                    {
                        pingLine.numCapVertices = 10;
                        pingLine.numCornerVertices = 5;
                    }

                    Vector3 StartPosition = leftHand ? GorillaTagger.Instance.leftHandTransform.position : GorillaTagger.Instance.rightHandTransform.position;
                    Vector3 Direction = forward;

                    Physics.SphereCast(StartPosition + Direction / 4f * (scaleWithPlayer ? GTPlayer.Instance.scale : 1f), 0.15f, Direction, out var Ray, 512f, NoInvisLayerMask());
                    Vector3 EndPosition = Ray.point == Vector3.zero ? StartPosition + (Direction * 512f) : Ray.point;

                    pingLine.SetPosition(0, StartPosition);
                    pingLine.SetPosition(1, EndPosition);

                    VRRig rigTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (Ray.collider != null && rigTarget != null && !rigTarget.IsLocal())
                    {
                        if (lastTarget != null && lastTarget != rigTarget)
                        {
                            lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                            if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                                lastTarget.mainSkin.material.color = lastTarget.playerColor;

                            lastTarget = null;
                        }

                        if (lastTarget == null)
                        {
                            Visuals.FixRigMaterialESPColors(rigTarget);

                            rigTarget.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                            rigTarget.mainSkin.material.color = targetColor;

                            GorillaTagger.Instance.StartVibration(leftHand, GorillaTagger.Instance.tagHapticStrength / 2f, 0.05f);

                            lastTarget = rigTarget;
                        }
                        else
                            lastTarget.mainSkin.material.color = targetColor;

                        bool trigger = leftHand ? leftTrigger > 0.5f : rightTrigger > 0.5f;

                        if (trigger && !lastTriggerSelect)
                        {
                            VRRig.LocalRig.PlayHandTapLocal(50, leftHand, 0.4f);
                            GorillaTagger.Instance.StartVibration(leftHand, GorillaTagger.Instance.tagHapticStrength / 2f, GorillaTagger.Instance.tagHapticDuration / 2f);

                            NavigatePlayer(GetPlayerFromVRRig(rigTarget));
                            ReloadMenu();

                            NotificationManager.SendNotification($"<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Selected player {GetPlayerFromVRRig(rigTarget).NickName}.");
                        }

                        lastTriggerSelect = trigger;
                    }
                    else
                    {
                        if (lastTarget != null)
                        {
                            lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                            if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                                lastTarget.mainSkin.material.color = lastTarget.playerColor;

                            lastTarget = null;
                        }
                    }
                }
                else
                {
                    if (selectObject != null)
                    {
                        Object.Destroy(selectObject);
                        selectObject = null;
                    }

                    if (lastTarget != null)
                    {
                        lastTarget.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                        if (lastTarget.mainSkin.material.name.Contains("gorilla_body"))
                            lastTarget.mainSkin.material.color = lastTarget.playerColor;

                        lastTarget = null;
                    }

                    lastTriggerSelect = false;
                }
            }
        }

        public static IEnumerator MenuIntroCoroutine()
        {
            if (Time.time < timeMenuStarted)
                yield return new WaitForSeconds(1f);

            float fps = 1f / Time.unscaledDeltaTime;
            yield return new WaitUntil(() => { fps = Mathf.Lerp(fps, 1f / Time.unscaledDeltaTime, 0.1f); return fps > 30f; });

            GameObject menuIntro = LoadObject<GameObject>("Intro");

            menuIntro.transform.position = GorillaTagger.Instance.bodyCollider.transform.position;
            menuIntro.transform.rotation = GorillaTagger.Instance.bodyCollider.transform.rotation;

            VideoPlayer videoPlayer = menuIntro.transform.Find("Video").GetComponent<VideoPlayer>();
            ParticleSystem particleSystem = menuIntro.transform.Find("Particles").GetComponent<ParticleSystem>();

            Color backgroundColor = Color.white;
            Fun.HueShift(Color.white);

            var main = particleSystem.main; // ????
            main.startColor = new ParticleSystem.MinMaxGradient(
                Main.backgroundColor.GetColor(0)
            );

            void EndImmediately()
            {
                Fun.HueShift(Color.clear);
                Object.Destroy(menuIntro);
            }

            float timeout = 0f;

            while (!videoPlayer.isPrepared)
            {
                timeout += Time.deltaTime;
                if (timeout > 5f)
                {
                    EndImmediately();
                    yield break;
                }
                yield return null;
            }

            bool videoEnded = false;
            videoPlayer.Play();
            videoPlayer.loopPointReached += (_) => videoEnded = true;

            yield return new WaitUntil(() => videoEnded);

            float fadeEnd = Time.time + 1f;
            Color transparentColor = backgroundColor;
            transparentColor.a = 0f;

            while (Time.time < fadeEnd)
            {
                float t = 1f - (fadeEnd - Time.time);
                Fun.HueShift(Color.Lerp(backgroundColor, transparentColor, t));
                videoPlayer.gameObject.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.clear, t);
                main.startColor = new ParticleSystem.MinMaxGradient(
                    Color.Lerp(main.startColor.color, Color.clear, t)
                );

                yield return null;
            }

            EndImmediately();
        }

        public static void MenuIntro() =>
            CoroutineManager.instance.StartCoroutine(MenuIntroCoroutine());


        public static void ResetVoiceCommandsKeywords()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt"))
                File.WriteAllLines($"{PluginInfo.BaseDirectory}/Seralyth_Keywords.txt", keyWords);
        }

        public static void ResetSystemPrompt()
        {
            if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_SystemPrompt.txt"))
                File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_SystemPrompt.txt", AIManager.SystemPrompt);
        }

        public static string SavePreferencesToText()
        {
            string seperator = ";;";

            string enabledtext = "";
            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    if (!v.detected && v.enabled && v.buttonText != "Save Preferences")
                    {
                        if (enabledtext == "")
                            enabledtext += v.buttonText;
                        else
                            enabledtext += seperator + v.buttonText;
                    }
                }
            }

            string favoritetext = "";
            foreach (string fav in favorites)
            {
                if (favoritetext == "")
                    favoritetext += fav;
                else
                    favoritetext += seperator + fav;
            }

            string[] settings = {
                Movement.platformMode.ToString(),
                Movement.platformShape.ToString(),
                Movement.flySpeedCycle.ToString(),
                Movement.longarmCycle.ToString(),
                Movement.speedboostCycle.ToString(),
                Projectiles.projMode.ToString(),
                Movement.timerPowerIndex.ToString(),
                Projectiles.shootCycle.ToString(),
                pointerIndex.ToString(),
                Advantages.tagAuraIndex.ToString(),
                notificationDecayTime.ToString(),
                fontStyleType.ToString(),
                arrowType.ToString(),
                pcbg.ToString(),
                Important.reconnectDelay.ToString(),
                Safety.fpsSpoofValue.ToString(),
                SoundManager.DefaultSounds["Button"],
                buttonClickVolume.ToString(),
                Safety.antiReportRangeIndex.ToString(),
                Advantages.tagRangeIndex.ToString(),
                Sound.BindMode.ToString(),
                Movement.driveInt.ToString(),
                langInd.ToString(),
                inputTextColorInt.ToString(),
                Movement.pullPowerInt.ToString(),
                SoundManager.DefaultSounds["Notification"],
                Visuals.PerformanceModeStepIndex.ToString(),
                gunVariation.ToString(),
                GunDirection.ToString(),
                narratorIndex.ToString(),
                Movement.predInt.ToString(),
                gunLineQualityIndex.ToString(),
                Projectiles.projDebounceIndex.ToString(),
                Projectiles.red.ToString(),
                Projectiles.green.ToString(),
                Projectiles.blue.ToString(),
                Safety.rankIndex.ToString(),
                Overpowered.snowballScale.ToString(),
                Overpowered.lagIndex.ToString(),
                Fun.blockDebounceIndex.ToString(),
                Fun.nameCycleIndex.ToString(),
                menuScaleIndex.ToString(),
                Sound.soundId.ToString(),
                Fun.targetQuestScore.ToString(),
                notificationScaleIndex.ToString(),
                overlayScaleIndex.ToString(),
                arraylistScaleIndex.ToString(),
                ((int)MathF.Ceiling(playTime)).ToString(),
                PhotonNetwork.LocalPlayer?.UserId ?? "null",
                _pageSize.ToString(),
                Overpowered.snowballMultiplicationFactor.ToString(),
                menuButtonIndex.ToString(),
                Safety.targetElo.ToString(),
                Safety.targetBadge.ToString(),
                Movement.playspaceAbuseIndex.ToString(),
                Movement.wallWalkStrengthIndex.ToString(),
                Fun.headSpinIndex.ToString(),
                Movement.macroPlaybackRangeIndex.ToString(),
                joystickMenuPosition.ToString(),
                Movement.multiplicationAmount.ToString(),
                Fun.targetFOV.ToString(),
                Projectiles.targetProjectileIndex.ToString(),
                Movement.fakeLagDelayIndex.ToString(),
                Projectiles.snowballIndex.ToString(),
                characterDistance.ToString(),
                Overpowered.lagTypeIndex.ToString(),
                Overpowered.masterVisualizationType.ToString(),
                Movement.targetHz.ToString(),
                Safety.pingSpoofValue.ToString(),
                Fun.soundboardVolumeIndex.ToString(),
                Fun.soundboardSpeedIndex.ToString(),
                SoundManager.DefaultSoundpack,
                Sound.disableLocalSoundboard.ToString(),
                Seralyth.Classes.Mods.StumpUpdateDisplay.AutoScrollEnabled.ToString(),
                Seralyth.Classes.Mods.StumpUpdateDisplay.LastSeenDllTimestamp.ToString(),
                GunLibLine.ToString(),
                GunLibTrail.ToString(),
                GunLibShape.ToString(),
                categoryDisplayMode.ToString(),
            };

            string settingstext = string.Join(seperator, settings);

            string bindingtext = "";
            foreach (KeyValuePair<string, List<string>> Bind in ModBindings)
            {
                if (bindingtext != "")
                    bindingtext += "~~";

                string toAppend = Bind.Key;
                foreach (string modName in Bind.Value)
                    toAppend += seperator + modName;

                bindingtext += toAppend;
            }

            string quickActionString = string.Join(seperator, quickActions);

            string rebindingtext = "";
            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    if (v.rebindKey != null || v.pcBindKey != null)
                    {
                        string entry = v.buttonText + ";" + (v.rebindKey ?? "") + ";" + (v.pcBindKey ?? "");
                        if (rebindingtext == "")
                            rebindingtext += entry;
                        else
                            rebindingtext += seperator + entry;
                    }
                }
            }

            string skipButtonString = string.Join(seperator, skipButtons);

            string finaltext =
                enabledtext + "\n" +
                favoritetext + "\n" +
                settingstext + "\n" +
                pageButtonType + "\n" +
                themeType + "\n" +
                fontCycle + "\n" +
                bindingtext + "\n" +
                quickActionString + "\n" +
                rebindingtext + "\n" +
                skipButtonString;

            return finaltext;
        }

        public static void SavePreferences()
        {
            LogManager.Log("Saving menuButtonIndex: " + menuButtonIndex);
            File.WriteAllText($"{PluginInfo.BaseDirectory}/Seralyth_Preferences.txt", SavePreferencesToText());
        }

        public static int loadingPreferencesFrame;
        public static bool isLoadingPreferences;
        public static void LoadPreferencesFromText(string text)
        {
            loadingPreferencesFrame = Time.frameCount;
            isLoadingPreferences = true;

            Panic();
            string[] textData = text.Split("\n");

            string[] activebuttons = textData[0].Split(";;");
            for (int index = 0; index < activebuttons.Length; index++)
                Toggle(activebuttons[index]);

            string[] favoritesarray = textData[1].Split(";;");
            favorites.Clear();
            foreach (string favorite in favoritesarray)
                favorites.Add(favorite);

            string[] data = textData[2].Split(";;");

            try
            {
                Movement.platformMode = int.Parse(data[0]);
                Movement.ChangePlatformType();

                Movement.platformShape = int.Parse(data[1]);
                Movement.ChangePlatformShape();

                Movement.flySpeedCycle = int.Parse(data[2]);
                Movement.ChangeFlySpeed();

                Movement.longarmCycle = int.Parse(data[3]);
                Movement.ChangeArmLength();

                Movement.speedboostCycle = int.Parse(data[4]);
                Movement.ChangeSpeedBoostAmount();

                Projectiles.projMode = int.Parse(data[5]);
                Projectiles.ChangeProjectile();

                Movement.timerPowerIndex = int.Parse(data[6]);
                Movement.ChangeTimerSpeed();

                Projectiles.shootCycle = int.Parse(data[7]);
                Projectiles.ChangeShootSpeed();

                pointerIndex = int.Parse(data[8]);
                ChangePointerPosition();

                Advantages.tagAuraIndex = int.Parse(data[9]);
                Advantages.ChangeTagAuraRange();

                notificationDecayTime = int.Parse(data[10]);
                ChangeNotificationTime();

                fontStyleType = int.Parse(data[11]);
                ChangeFontStyleType();

                arrowType = int.Parse(data[12]);
                ChangeArrowType();

                pcbg = int.Parse(data[13]);
                ChangePCUI();

                Important.reconnectDelay = int.Parse(data[14]);
                ChangeReconnectTime();

                Safety.fpsSpoofValue = string.IsNullOrWhiteSpace(data[15]) ? 85 : int.Parse(data[15]);
                Safety.ChangeFPSSpoofValue();

                SoundManager.DefaultSounds["Button"] = data[16];
                Buttons.GetIndex("Change Button Sound").overlapText = $"Change Button Sound <color=grey>[</color><color=green>{SoundManager.DefaultSounds["Button"]}</color><color=grey>]</color>";

                buttonClickVolume = int.Parse(data[17]);
                ChangeButtonVolume();

                Safety.antiReportRangeIndex = int.Parse(data[18]);
                Safety.ChangeAntiReportRange();

                Advantages.tagRangeIndex = int.Parse(data[19]);
                Advantages.ChangeTagReachDistance();

                Sound.BindMode = int.Parse(data[20]);
                Sound.SoundBindings();

                Movement.driveInt = int.Parse(data[21]);
                Movement.ChangeDriveSpeed();

                langInd = int.Parse(data[22]);
                ChangeMenuLanguage();

                inputTextColorInt = int.Parse(data[23]);
                ChangeInputTextColor();

                Movement.pullPowerInt = int.Parse(data[24]);
                Movement.ChangePullModPower();

                SoundManager.DefaultSounds["Notification"] = data[25];
                Buttons.GetIndex("Change Notification Sound").overlapText = $"Change Notification Sound <color=grey>[</color><color=green>{SoundManager.DefaultSounds["Notification"]}</color><color=grey>]</color>";

                Visuals.PerformanceModeStepIndex = int.Parse(data[26]);
                Visuals.ChangePerformanceModeVisualStep();

                gunVariation = int.Parse(data[27]);
                ChangeGunVariation();

                GunDirection = int.Parse(data[28]);
                ChangeGunDirection();

                narratorIndex = int.Parse(data[29]);
                ChangeNarrationVoice();

                Movement.predInt = int.Parse(data[30]);
                Movement.ChangePredictionAmount();

                gunLineQualityIndex = int.Parse(data[31]);
                ChangeGunLineQuality();

                Projectiles.projDebounceIndex = int.Parse(data[32]);
                Projectiles.ChangeProjectileDelay();

                Projectiles.red = int.Parse(data[33]);
                Projectiles.IncreaseRed();

                Projectiles.green = int.Parse(data[34]);
                Projectiles.IncreaseGreen();

                Projectiles.blue = int.Parse(data[35]);
                Projectiles.IncreaseBlue();

                Safety.rankIndex = int.Parse(data[36]);
                Safety.ChangeRankedTier();

                Overpowered.snowballScale = int.Parse(data[37]);
                Overpowered.ChangeSnowballScale();

                Overpowered.lagIndex = int.Parse(data[38]);
                Overpowered.ChangeLagPower();

                Fun.blockDebounceIndex = int.Parse(data[39]);
                Fun.ChangeBlockDelay();

                Fun.nameCycleIndex = int.Parse(data[40]);

                menuScaleIndex = int.Parse(data[41]);
                ChangeMenuScale();

                Sound.soundId = int.Parse(data[42]);
                Sound.IncreaseSoundID();

                Fun.targetQuestScore = int.Parse(data[43]);
                Fun.ChangeCustomQuestScore();

                notificationScaleIndex = int.Parse(data[44]);
                ChangeNotificationScale();

                overlayScaleIndex = int.Parse(data[45]);
                ChangeOverlayScale();

                arraylistScaleIndex = int.Parse(data[46]);
                ChangeArraylistScale();

                playTime = int.Parse(data[47]);

                Important.oldId = data[48];

                _pageSize = int.Parse(data[49]);
                ChangePageSize();

                Overpowered.snowballMultiplicationFactor = int.Parse(data[50]);
                Overpowered.ChangeSnowballMultiplicationFactor();

                Safety.targetElo = int.Parse(data[52]);
                Safety.ChangeELOValue();

                Safety.targetBadge = int.Parse(data[53]);
                Safety.ChangeBadgeTier();

                Movement.playspaceAbuseIndex = int.Parse(data[54]);
                Movement.ChangePlayspaceAbuseSpeed();

                Movement.wallWalkStrengthIndex = int.Parse(data[55]);
                Movement.ChangeWallWalkStrength();

                Fun.headSpinIndex = int.Parse(data[56]);
                Fun.ChangeHeadSpinSpeed();

                Movement.macroPlaybackRangeIndex = int.Parse(data[57]);
                Movement.ChangeMacroPlaybackRange();

                joystickMenuPosition = int.Parse(data[58]);
                ChangeJoystickMenuPosition();

                Movement.multiplicationAmount = int.Parse(data[59]);
                Movement.MultiplicationAmount();

                Fun.targetFOV = int.Parse(data[60]);
                Fun.ChangeTargetFOV();

                Projectiles.targetProjectileIndex = int.Parse(data[61]);
                Projectiles.ChangeProjectileIndex();

                Movement.fakeLagDelayIndex = int.Parse(data[62]);
                Movement.ChangeFakeLagStrength();

                Projectiles.snowballIndex = int.Parse(data[63]);
                Projectiles.ChangeGrowingProjectile();

                characterDistance = int.Parse(data[64]);
                ChangeCharacterDistance();

                Overpowered.lagTypeIndex = int.Parse(data[65]);
                Overpowered.ChangeLagType();

                Overpowered.masterVisualizationType = int.Parse(data[66]);
                Overpowered.MasterVisualizationType();

                Movement.targetHz = int.Parse(data[67]);
                Movement.ChangeTinnitusHz();

                Safety.pingSpoofValue = int.Parse(data[68]);
                Safety.ChangePingSpoofValue();

                Fun.soundboardVolumeIndex = float.Parse(data[69]);
                Fun.ChangeSoundboardVolume();

                Fun.soundboardSpeedIndex = float.Parse(data[70]);
                Fun.ChangeSoundboardSpeed();

                SoundManager.DefaultSoundpack = data[71];
                Buttons.GetIndex("Change Menu Soundpack").overlapText = $"Change Menu Soundpack <color=grey>[</color><color=green>{SoundManager.DefaultSoundpack}</color><color=grey>]</color>";

                Sound.disableLocalSoundboard = bool.Parse(data[72]);

                if (data.Length > 73)
                    Seralyth.Classes.Mods.StumpUpdateDisplay.AutoScrollEnabled = bool.Parse(data[73]);

                if (data.Length > 74 && long.TryParse(data[74], out long dllTs))
                    Seralyth.Classes.Mods.StumpUpdateDisplay.LastSeenDllTimestamp = dllTs;

                if (data.Length > 75)
                    GunLibLine = bool.Parse(data[75]);

                if (data.Length > 76)
                    GunLibTrail = bool.Parse(data[76]);

                if (data.Length > 77)
                {
                    GunLibShape = int.Parse(data[77]);
                    ChangeGunLibShape();
                }

                if (data.Length > 78)
                {
                    categoryDisplayMode = int.Parse(data[78]);
                    ChangeCategoryDisplay();
                }
            }
            catch { LogManager.Log("Save file out of date"); }

            if (int.TryParse(data[51], out int parsedMenuButton))
                menuButtonIndex = parsedMenuButton;
            else
                LogManager.Log("Failed to parse menuButtonIndex from data[51]: " + (data.Length > 51 ? data[51] : "MISSING"));

            {
                string[] menuButtonNames = { "Primary", "Secondary", "Grip", "Trigger", "Joystick" };
                if (menuButtonIndex >= 0 && menuButtonIndex < menuButtonNames.Length)
                    Buttons.GetIndex("Change Menu Button").overlapText = "Change Menu Button <color=grey>[</color><color=green>" + menuButtonNames[menuButtonIndex] + "</color><color=grey>]</color>";
                else
                {
                    menuButtonIndex = 1;
                    Buttons.GetIndex("Change Menu Button").overlapText = "Change Menu Button <color=grey>[</color><color=green>Secondary</color><color=grey>]</color>";
                }
            }

            LogManager.Log("Loaded menuButtonIndex: " + menuButtonIndex + " from data[51]: " + (data.Length > 51 ? data[51] : "MISSING"));


            pageButtonType = int.Parse(textData[3]);
            Toggle("Change Page Type");
            themeType = int.Parse(textData[4]);
            Toggle("Change Menu Theme");
            fontCycle = int.Parse(textData[5]);
            Toggle("Change Font Type");

            try
            {
                foreach (string Bindings in textData[6].Split("~~"))
                {
                    if (Bindings.Contains(";;"))
                    {
                        string[] BindData = Bindings.Split(";;");
                        string BindName = BindData[0];

                        List<string> Binds = new List<string>();

                        for (int i = 1; i < BindData.Length; i++)
                        {
                            string ModName = BindData[i];
                            if (Buttons.GetIndex(ModName) != null)
                                Binds.Add(ModName);
                        }

                        ModBindings[BindName] = Binds;
                    }
                }
            }
            catch { }

            try
            {
                quickActions.Clear();
                foreach (string quickAction in textData[7].Split(";;"))
                {
                    ButtonInfo button = Buttons.GetIndex(quickAction);
                    if (button != null)
                        quickActions.Add(quickAction);
                }
            }
            catch { }

            try
            {
                foreach (string bind in textData[8].Split(";;"))
                {
                    string[] parts = bind.Split(";");
                    string rebindText = parts[0];
                    ButtonInfo button = Buttons.GetIndex(rebindText);
                    if (button != null)
                    {
                        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                            button.rebindKey = parts[1];
                        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                            button.pcBindKey = parts[2];
                    }
                }
            }
            catch { }

            try
            {
                skipButtons.Clear();
                foreach (string skipButton in textData[9].Split(";;"))
                {
                    ButtonInfo button = Buttons.GetIndex(skipButton);
                    if (button != null)
                        skipButtons.Add(skipButton);
                }
            }
            catch { }

            isLoadingPreferences = false;
            hasLoadedPreferences = true;
        }

        public static void LoadPreferences()
        {
            try
            {
                if (!File.Exists($"{PluginInfo.BaseDirectory}/Seralyth_Preferences.txt"))
                {
                    hasLoadedPreferences = true;
                    return;
                }

                try { UpdateSoundPreferences(); }
                catch (Exception ex) { LogManager.Log("UpdateSoundPreferences failed: " + ex.Message); }

                string text = File.ReadAllText($"{PluginInfo.BaseDirectory}/Seralyth_Preferences.txt");
                LoadPreferencesFromText(text);
            }
            catch (Exception e) { LogManager.Log("Error loading preferences: " + e.Message); }
        }

        public static void Panic()
        {
            AnnoyingModeOff();
            foreach (ButtonInfo[] buttonlist in Buttons.buttons)
            {
                foreach (ButtonInfo v in buttonlist)
                {
                    if (v.enabled)
                        Toggle(v.buttonText);
                }
            }
        }

        public enum ControllerBinding
        {
            None,
            LeftTrigger,
            RightTrigger,
            LeftGrip,
            RightGrip,
            LeftPrimaryButton,
            RightPrimaryButton,
            LeftSecondaryButton,
            RightSecondaryButton,
            JoystickClick,
            LeftOverride
        }

        public static readonly Dictionary<ControllerBinding, Key> pcBindings = new Dictionary<ControllerBinding, Key>
        {
            { ControllerBinding.RightPrimaryButton, Key.E },
            { ControllerBinding.RightSecondaryButton, Key.R },
            { ControllerBinding.LeftPrimaryButton, Key.F },
            { ControllerBinding.LeftSecondaryButton, Key.G },
            { ControllerBinding.LeftGrip, Key.LeftBracket },
            { ControllerBinding.RightGrip, Key.RightBracket },
            { ControllerBinding.LeftTrigger, Key.Minus },
            { ControllerBinding.RightTrigger, Key.Equals },
            { ControllerBinding.JoystickClick, Key.Enter },
            { ControllerBinding.LeftOverride, Key.LeftAlt }
        };

        public static void LoadPCControls()
        {
            string fileName = $"{PluginInfo.BaseDirectory}/Seralyth_PCControls.txt";

            if (File.Exists(fileName))
            {
                string data = File.ReadAllText(fileName);
                string[] lines = data.Split('\n');
                pcBindings.Clear();

                foreach (string line in lines)
                {
                    string finalLine = line.Trim();

                    if (!finalLine.Contains(" - "))
                        continue;

                    string[] splitData = finalLine.Split(" - ");

                    if (Enum.TryParse(splitData[1], out ControllerBinding binding) && Enum.TryParse(splitData[0], out Key key))
                        pcBindings[binding] = key;
                }
            }
            else
            {
                var lines = new List<string>();

                foreach (var pair in pcBindings)
                    lines.Add($"{pair.Value} - {pair.Key}");

                File.WriteAllLines(fileName, lines);
            }
        }


        public static void ChangeReconnectTime(bool positive = true)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    Important.reconnectDelay++;
                else
                    Important.reconnectDelay--;
            }

            if (Important.reconnectDelay > 5)
                Important.reconnectDelay = 1;
            if (Important.reconnectDelay < 1)
                Important.reconnectDelay = 5;

            Buttons.GetIndex("Change Reconnect Time").overlapText = "Change Reconnect Time <color=grey>[</color><color=green>" + Important.reconnectDelay + "</color><color=grey>]</color>";
        }

        public static void ChangeButtonSound(bool positive = true, bool fromMenu = false)
        {
            var buttonKeys = SoundManager.Sounds["Buttons"].Keys.ToArray();

            int index = Array.IndexOf(buttonKeys, SoundManager.DefaultSounds["Button"]);
            if (index < 0) index = 0;

            index = positive ? index + 1 : index - 1;

            if (index >= buttonKeys.Length) index = 0;
            if (index < 0) index = buttonKeys.Length - 1;

            string newSound = buttonKeys[index];
            SoundManager.DefaultSounds["Button"] = newSound;

            Buttons.GetIndex("Change Button Sound").overlapText = $"Change Button Sound <color=grey>[</color><color=green>{newSound}</color><color=grey>]</color>";

            if (!fromMenu) return;
            if (VRRig.LocalRig == null) return;
            if (VRRig.LocalRig.leftHandPlayer != null) VRRig.LocalRig.leftHandPlayer.Stop();
            if (VRRig.LocalRig.rightHandPlayer != null) VRRig.LocalRig.rightHandPlayer.Stop();

            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        public static void ChangeButtonVolume(bool positive = true, bool fromMenu = false)
        {
            if (!Settings.isLoadingPreferences)
            {
                if (positive)
                    buttonClickVolume++;
                else
                    buttonClickVolume--;
            }

            buttonClickVolume %= 11;
            if (buttonClickVolume < 0)
                buttonClickVolume = 10;

            Buttons.GetIndex("Change Button Volume").overlapText = "Change Button Volume <color=grey>[</color><color=green>" + buttonClickVolume + "</color><color=grey>]</color>";

            if (fromMenu)
            {
                VRRig.LocalRig.leftHandPlayer.Stop();
                VRRig.LocalRig.rightHandPlayer.Stop();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
        }

        public static void ChangeMenuSoundpack(bool positive = true, bool fromMenu = false)
        {
            var packKeys = SoundManager.Soundpacks.Keys.ToArray();

            int index = Array.IndexOf(packKeys, SoundManager.DefaultSoundpack);
            if (index < 0) index = 0;

            index = positive ? index + 1 : index - 1;

            if (index >= packKeys.Length) index = 0;
            if (index < 0) index = packKeys.Length - 1;

            string newPack = packKeys[index];
            SoundManager.DefaultSoundpack = newPack;

            Buttons.GetIndex("Change Menu Soundpack").overlapText = $"Change Menu Soundpack <color=grey>[</color><color=green>{newPack}</color><color=grey>]</color>";

            if (!fromMenu) return;
            if (VRRig.LocalRig == null) return;
            if (VRRig.LocalRig.leftHandPlayer != null) VRRig.LocalRig.leftHandPlayer.Stop();
            if (VRRig.LocalRig.rightHandPlayer != null) VRRig.LocalRig.rightHandPlayer.Stop();

            SoundManager.Play("Default");
        }

        // === ButtonHelper Name Arrays ===

        public static readonly string[] ThemeNames = {
            "Seralyth", "Blue Magenta", "Dark Mode", "Strobe", "Kman", "Rainbow",
            "Player Material", "Lava", "Rock", "Ice", "Water", "Minty",
            "Pink", "Purple", "Magenta Cyan", "Red Fade", "Orange Fade",
            "Yellow Fade", "Green Fade", "Blue Fade", "Purple Fade", "Magenta Fade",
            "Banana", "Pride", "Trans", "MLM or Gay", "Steal (old)", "Silence",
            "Transparent", "King", "Scoreboard", "Scoreboard (banned)", "Rift",
            "Blurple Dark", "ShibaGT Gold", "ShibaGT Genesis", "wyvern",
            "Steal (new)", "USA Menu (lol)", "Watch", "AZ Menu", "ImGUI",
            "Clean Dark", "Discord Light Mode (lmfao)", "The Hub", "EPILEPTIC",
            "Discord Blurple", "VS Zero", "Weed theme", "Pastel Rainbow",
            "Rift Light", "Rose (Solace)", "Tenacity (Solace)", "e621 (by iiDk)",
            "Catppuccin Mocha", "Rexon", "Tenacity (Minecraft)", "Mint Blue (Opal v2)",
            "Pink Blood (Opal v2)", "Purple Fire (Opal v2)", "Deep Ocean (Opal v2)",
            "Bad Apple (thanks random person in vc for idea)", "coolkidd", "Old ShibaGT RGB", "Old-ish ShibaGT RGB"
        };

        public static readonly string[] LanguageNames = {
            "English", "Español", "Français", "Deutsch", "日本語", "Italiano",
            "Português", "Nederlands", "Русский", "Polski", "svenska", "dansk"
        };

        private static readonly string[] LanguageCodenames = {
            "en", "es", "fr", "de", "ja", "it", "pt", "nl", "ru", "pl", "sw", "da"
        };

        public static readonly string[] MenuButtonNames = {
            "Primary", "Secondary", "Grip", "Trigger", "Joystick"
        };

        public static readonly string[] InputColorNames = {
            "Red", "Orange", "Yellow", "Green", "Blue", "Cyan",
            "Purple", "Pink", "White", "Grey", "Black", "Rose"
        };

        private static readonly string[] InputColorValues = {
            "red", "#ff8000", "yellow", "green", "blue", "#00FFFF",
            "purple", "#FF00FF", "white", "grey", "black", "#ff005d"
        };

        public static readonly string[] NarratorNames = {
            "Default", "Kimberly", "Brian", "Matthew", "Joey", "Justin",
            "Cristiano", "Giorgio", "Ewa", "TikTok", "Grandma", "Trickster",
            "Elf", "Ghostface", "Zombie", "Narrator", "Pirate", "Song",
            "TikTok Joey", "Gingerbread Man", "Chris", "Thanksgiving", "Santa",
            "Google US", "Google UK", "Dog", "Jerkface", "Robot", "Vlad", "Obama"
        };

        public static readonly string[] GunQualityNames = {
            "Potato", "Low", "Normal", "High", "Extreme"
        };

        private static readonly int[] GunQualityValues = { 10, 25, 50, 100, 250 };

        public static readonly string[] GunVariationNames = {
            "Default", "Lightning", "Wavy", "Blocky", "Zigzag",
            "Spring", "Bouncy", "Audio", "Bezier", "Rope"
        };

        public static readonly string[] GunDirectionNames = {
            "Default", "Legacy", "Laser", "Finger", "Face"
        };

        public static readonly string[] GunLibShapeNames = {
            "Disabled", "Circle", "Square", "Triangle", "Star"
        };

        public static readonly Vector3[] PointerPositions = {
            new Vector3(0f, -0.1f, 0f),
            new Vector3(0f, -0.1f, -0.15f),
            new Vector3(0f, 0.1f, -0.05f),
            new Vector3(0f, 0.0666f, 0.1f)
        };

        public static readonly string[] FontTypeNames = {
            "Agency FB", "FreeSans", "DejaVu Sans", "Utopium", "Comic Sans",
            "Cascadia Mono", "Candara", "MS Gothic", "Anton", "SimSun",
            "Minecraft", "Terminal", "OpenDyslexic", "Taiko", "Liberation Sans"
        };

        // === ButtonHelper Apply Methods ===

        public static void ApplyMenuLanguage(int index)
        {
            langInd = index;
            TranslationManager.translateCache.Clear();
            TranslationManager.language = LanguageCodenames[langInd];
            translate = langInd != 0;
        }

        public static void ApplyMenuButton(int index)
        {
            menuButtonIndex = index;
        }

        public static void ApplyMenuTheme(int index)
        {
            themeType = index;
            ChangeMenuTheme(true);
        }

        public static void ApplyMenuScale(int index)
        {
            menuScaleIndex = index;
            menuScale = index / 10f;
        }

        public static void ApplyNotificationScale(int index)
        {
            notificationScaleIndex = index;
            notificationScale = index * 5;
        }

        public static void ApplyArraylistScale(int index)
        {
            arraylistScaleIndex = index;
            arraylistScale = index * 5;
        }

        public static void ApplyOverlayScale(int index)
        {
            overlayScaleIndex = index;
            overlayScale = index * 5;
        }

        public static void ApplyPageSize(int index)
        {
            _pageSize = index;
        }

        public static void ApplyCharacterDistance(int index)
        {
            characterDistance = index;
        }

        public static void ApplyPageType(int index)
        {
            pageButtonType = index;
            buttonOffset = pageButtonType == 2 ? 2 : 0;
        }

        public static void ApplyArrowType(int index)
        {
            arrowType = index;
        }

        public static void ApplyFontType(int index)
        {
            fontCycle = index;
            switch (fontCycle)
            {
                case 0: activeFont = AgencyFB; return;
                case 1: activeFont = FreeSans; return;
                case 2: activeFont = DejaVuSans; return;
                case 3: activeFont = Utopium; return;
                case 4: activeFont = ComicSans; return;
                case 5: activeFont = CascadiaMono; return;
                case 6: activeFont = Candara; return;
                case 7: activeFont = MSGothic; return;
                case 8: activeFont = Anton; return;
                case 9: activeFont = SimSun; return;
                case 10: activeFont = Minecraft; return;
                case 11: activeFont = Terminal; return;
                case 12: activeFont = OpenDyslexic; return;
                case 13: activeFont = Taiko; return;
                case 14: activeFont = LiberationSans; return;
            }
        }

        public static void ApplyFontStyleType(int index)
        {
            fontStyleType = index;
            activeFontStyle = fontStyleType switch
            {
                0 => FontStyles.Normal,
                1 => FontStyles.Bold,
                2 => FontStyles.Italic,
                3 => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        public static void ApplyInputTextColor(int index)
        {
            inputTextColorInt = index;
            inputTextColor = InputColorValues[index];
        }

        public static void ApplyPCUI(int index)
        {
            pcbg = index;
        }

        public static void ApplyJoystickMenuPosition(int index)
        {
            joystickMenuPosition = index;
        }

        public static void ApplyNotificationTime(int index)
        {
            notificationDecayTime = index * 1000;
        }

        public static void ApplyNotificationSound(int index)
        {
            var keys = SoundManager.Sounds["Notifications"].Keys.ToArray();
            if (index >= 0 && index < keys.Length)
                SoundManager.DefaultSounds["Notification"] = keys[index];
        }

        public static void ApplyNarrationVoice(int index)
        {
            narratorIndex = index;
            narratorName = NarratorNames[index];

            if (krec != null && krec.IsRunning && Time.time > dRestartTime)
            {
                DictationRestart();
                dRestartTime = Time.time + 1f;
            }
        }

        public static void ApplyPointerPosition(int index)
        {
            pointerIndex = index;
            pointerOffset = PointerPositions[index];
            try { reference.transform.localPosition = pointerOffset; } catch { }
        }

        public static void ApplyGunLineQuality(int index)
        {
            gunLineQualityIndex = index;
            GunLineQuality = GunQualityValues[index];
        }

        public static void ApplyGunVariation(int index)
        {
            gunVariation = index;
        }

        public static void ApplyGunDirection(int index)
        {
            GunDirection = index;
        }

        public static void ApplyButtonSound(int index)
        {
            var keys = SoundManager.Sounds["Buttons"].Keys.ToArray();
            if (index >= 0 && index < keys.Length)
                SoundManager.DefaultSounds["Button"] = keys[index];
        }

        public static void ApplyButtonVolume(int index)
        {
            buttonClickVolume = index;
        }

        public static void PreviewButtonVolume(bool positive)
        {
            if (VRRig.LocalRig == null) return;
            VRRig.LocalRig.leftHandPlayer?.Stop();
            VRRig.LocalRig.rightHandPlayer?.Stop();
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        public static void ApplyMenuSoundpack(int index)
        {
            var keys = SoundManager.Soundpacks.Keys.ToArray();
            if (index >= 0 && index < keys.Length)
                SoundManager.DefaultSoundpack = keys[index];
        }
    }
}
