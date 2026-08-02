/*
 * Seralyth Menu  Mods/Experimental.cs
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
using GorillaTagScripts.VirtualStumpCustomMaps;
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
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.RandomUtilities;
using static Seralyth.Utilities.RigUtilities;
using Console = Seralyth.Classes.Menu.Console;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Seralyth.Mods
{
    public class AssetEntry
    {
        public string file;
        public string prefabName;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }

    public class ChangeAsset
    {
        public static ChangeAsset Instance = new ChangeAsset();
        public int IncrementalValue;

        public static AssetEntry[] Assets = new AssetEntry[]
        {
            new AssetEntry
            {
                file = "consolehamburburassets", prefabName = "burger", position = Vector3.zero,
                rotation = Vector3.zero, scale = Vector3.one
            },
            new AssetEntry
            {
                file = "consolehamburburassets", prefabName = "carti", position = new Vector3(0f, -0.1f, 0f),
                rotation = Vector3.zero, scale = Vector3.one * 0.5f
            },
            new AssetEntry
            {
                file = "consolehamburburassets", prefabName = "shrek", position = new Vector3(0f, 0f, 0.5f),
                rotation = Vector3.zero, scale = Vector3.one * 0.3f
            },
        };

        public static void ChangeValue(bool positive = true)
        {
            if (positive)
            {
                Instance.IncrementalValue++;
                if (Instance.IncrementalValue >= Assets.Length)
                    Instance.IncrementalValue = 0;
            }
            else
            {
                Instance.IncrementalValue--;
                if (Instance.IncrementalValue < 0)
                    Instance.IncrementalValue = Assets.Length - 1;
            }

            Buttons.GetIndex("Asset: ").overlapText =
                "Asset: <color=grey>[</color><color=green>" + Assets[Instance.IncrementalValue].prefabName +
                "</color><color=grey>]</color>";
        }
    }

    public static class Experimental
    {
        public static void PLACEHOLDER()
        {
            NotificationManager.SendNotification("<color=grey>[</color><color=yellow>PLACEHOLDER</color><color=grey>]</color> This button does nothing yet.");
        }

        public static void FixDuplicateButtons()
        {
            int duplicateButtons = 0;
            List<string> previousNames = new List<string>();
            foreach (ButtonInfo[] buttonn in Buttons.buttons)
            {
                foreach (ButtonInfo button in buttonn)
                {
                    if (previousNames.Contains(button.buttonText))
                    {
                        string buttonText = button.overlapText ?? button.buttonText;
                        button.overlapText = buttonText;
                        button.buttonText += "X";
                        duplicateButtons++;
                    }

                    previousNames.Add(button.buttonText);
                }
            }

            NotificationManager.SendNotification(
                "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Successfully fixed " +
                duplicateButtons + " broken buttons.");
        }

        private static readonly Dictionary<Renderer, Material> oldMats = new Dictionary<Renderer, Material>();

        public static void BetterFPSBoost()
        {
            foreach (Renderer v in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                try
                {
                    if (v.material.shader.name == "GorillaTag/UberShader")
                    {
                        oldMats.Add(v, v.material);
                        Material replacement = new Material(Shader.Find("GorillaTag/UberShader"))
                        {
                            color = v.material.color
                        };
                        v.material = replacement;
                    }
                }
                catch (Exception exception)
                {
                    LogManager.LogError(string.Format("mat error {1} - {0}", exception.Message, exception.StackTrace));
                }
            }
        }

        public static void DisableBetterFPSBoost()
        {
            foreach (KeyValuePair<Renderer, Material> v in oldMats)
                v.Key.material = v.Value;
        }

        public static void OnlySerializeNecessary()
        {
            SerializePatch.OverrideSerialization = () =>
            {
                SendSerialize(VRRig.LocalRig.GetPhotonView());
                //SendSerialize(GorillaTagger.Instance.myVRRig.reliableView);
                return false;
            };
        }

        public static void DumpSoundData()
        {
            string text = "Handtap Sound Data\n(from GorillaLocomotion.GTPlayer.Instance.materialData)";
            int i = 0;
            foreach (GTPlayer.MaterialData oneshot in GTPlayer.Instance.materialData)
            {
                try
                {
                    text += "\n====================================\n";
                    text += i + " ; " + oneshot.matName + " ; " + oneshot.slidePercent + "% ; " +
                            (oneshot.audio == null ? "none" : oneshot.audio.name);
                }
                catch
                {
                    LogManager.Log("Failed to log sound");
                }

                i++;
            }

            text += "\n====================================\n";
            text += "Text file generated with Seralyth Menu";
            string fileName = $"{PluginInfo.BaseDirectory}/SoundData.txt";

            File.WriteAllText(fileName, text);

            string filePath = FileUtilities.GetGamePath() + "/" + fileName;
            Process.Start(filePath);
        }

        public static void DumpCosmeticData()
        {
            string text = "Cosmetic Data\n(from CosmeticsController.instance.allCosmetics)";
            foreach (CosmeticsController.CosmeticItem hat in CosmeticsController.instance.allCosmetics)
            {
                try
                {
                    text += "\n====================================\n";
                    text += hat.itemName + " ; " + hat.displayName + " (override " + hat.overrideDisplayName + ") ; " +
                            hat.cost + "SR ; canTryOn = " + hat.canTryOn;
                }
                catch
                {
                    LogManager.Log("Failed to log hat");
                }
            }

            text += "\n====================================\n";
            text += "Text file generated with Seralyth Menu";
            string fileName = $"{PluginInfo.BaseDirectory}/CosmeticData.txt";

            File.WriteAllText(fileName, text);

            string filePath = FileUtilities.GetGamePath() + "/" + fileName;
            Process.Start(filePath);
        }

        public static void DecryptableCosmeticData()
        {
            string text = "";
            foreach (CosmeticsController.CosmeticItem hat in CosmeticsController.instance.allCosmetics)
            {
                try
                {
                    text += hat.itemName + ";;" + hat.overrideDisplayName + ";;" + hat.cost + "\n";
                }
                catch
                {
                    LogManager.Log("Failed to log hat");
                }
            }

            string fileName = $"{PluginInfo.BaseDirectory}/DecryptableCosmeticData.txt";

            File.WriteAllText(fileName, text);

            string filePath = FileUtilities.GetGamePath() + "/" + fileName;
            Process.Start(filePath);
        }

        public static void DumpRPCData()
        {
            string text = "RPC Data\n(from PhotonNetwork.PhotonServerSettings.RpcList)";
            int i = 0;
            foreach (string name in PhotonNetwork.PhotonServerSettings.RpcList)
            {
                try
                {
                    text += "\n====================================\n";
                    text += i + " ; " + name;
                }
                catch
                {
                    LogManager.Log("Failed to log RPC");
                }

                i++;
            }

            text += "\n====================================\n";
            text += "Text file generated with Seralyth Menu";
            string fileName = $"{PluginInfo.BaseDirectory}/RPCData.txt";

            File.WriteAllText(fileName, text);

            string filePath = FileUtilities.GetGamePath() + "/" + fileName;
            Process.Start(filePath);
        }

        public static void BlankPage()
        {
            Buttons.buttons[Buttons.GetCategory("Temporary Category")] = Array.Empty<ButtonInfo>();
            Buttons.CurrentCategoryName = "Temporary Category";
        }

        public static void CopyCustomGamemodeScript()
        {
            NotificationManager.SendNotification(
                "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Copied map script to your clipboard.",
                5000);
            GUIUtility.systemCopyBuffer = CustomGameMode.LuaScript;
        }

        public static void CopyCustomMapID()
        {
            string id = CustomMapManager.currentRoomMapModId._id.ToString();
            NotificationManager.SendNotification(
                "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> " + id, 5000);
            GUIUtility.systemCopyBuffer = id;
        }

        public static int restartIndex;
        public static float restartDelay;
        public static Vector3 restartPosition;
        public static string restartRoom;

        public static void SafeRestartGame()
        {
            string restartDataPath = $"{PluginInfo.BaseDirectory}/RestartData.txt";
            switch (restartIndex)
            {
                case 0:
                    if (File.Exists(restartDataPath))
                    {
                        string data = File.ReadAllText(restartDataPath);
                        restartRoom = data.Split(";")[0];
                        List<string> positionData = data.Split(";")[1].Split(",").ToList();
                        restartPosition = new Vector3(float.Parse(positionData[0]), float.Parse(positionData[1]),
                            float.Parse(positionData[2]));
                        restartIndex = 3;
                    }
                    else
                    {
                        restartRoom = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "";
                        restartPosition = GTPlayer.Instance.transform.position;
                        restartIndex = 1;
                    }

                    restartDelay = Time.time + 6f;
                    break;
                case 1:
                    Settings.SavePreferences();
                    File.WriteAllText(restartDataPath,
                        restartRoom + $";{restartPosition.x},{restartPosition.y},{restartPosition.z}");
                    restartIndex = 2;
                    break;
                case 2:
                    if (File.Exists(restartDataPath) && Time.time > restartDelay)
                    {
                        Important.RestartGame();
                        restartIndex = 4;
                    }

                    break;
                case 3:
                    if (!PhotonNetwork.InRoom && restartRoom != "")
                    {
                        if (Important.queueCoroutine == null && Time.time > restartDelay)
                            Important.QueueRoom(restartRoom);
                    }
                    else
                    {
                        TeleportPlayer(restartPosition);
                        File.Delete(restartDataPath);
                        NotificationManager.SendNotification(
                            "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Restarted game with information.");
                        restartIndex = 4;
                        Buttons.GetIndex("Safe Restart Game").enabled = false;
                        Settings.SavePreferences();
                    }

                    break;
            }
        }

        private static float adminEventDelay;

        public static void AdminKickGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("kick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                    }
                }
            }
        }

        public static void AdminFemboyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("sb", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId,
                            "https://files.hamburbur.org/ilikefemboys.mp3");
                    }
                }
            }
        }

        public static List<string> platExcluded = new List<string>();

        public static void AdminPlatToggleGun(bool exclude)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        string id = GetPlayerFromVRRig(gunTarget).UserId;
                        adminEventDelay = Time.time + 0.1f;
                        if (exclude)
                        {
                            if (!platExcluded.Contains(id))
                            {
                                platExcluded.Add(id);
                                NotificationManager.SendNotification(
                                    "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Player is now excluded.");
                            }
                            else
                                NotificationManager.SendNotification(
                                    "<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Player is already excluded!");
                        }
                        else
                        {
                            if (platExcluded.Contains(id))
                            {
                                platExcluded.Remove(id);
                                NotificationManager.SendNotification(
                                    "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Player is now included.");
                            }
                            else
                                NotificationManager.SendNotification(
                                    "<color=grey>[</color><color=red>ERROR</color><color=grey>]</color> Player is already included!");
                        }
                    }
                }
            }
        }

        private static int allocatedTSEFId;

        public static void TSEF()
        {
            allocatedTSEFId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "travis", "TravisScott", allocatedTSEFId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedTSEFId,
                new Vector3(-65f, 2f, -55f));
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedTSEFId, Vector3.one * 0.4f);
            if (isassetsbig)
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedTSEFId, Vector3.one * 3.5f);

            Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, allocatedTSEFId,
                Quaternion.Euler(0f, 20f, 0f));
        }

        public static void UTSEF()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedTSEFId);
        }

        
        public static bool isassetsbig;

        public static void BigAssets()
        {
            isassetsbig = true;
        }

        public static void NoBigAssets()
        {
            isassetsbig = false;
        }

        private static int allocated1xId = -1;

        public static void Forsaken()
        {
            var allocatedForsakenId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "1x", "1x", allocated1xId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocated1xId,
                new Vector3(-3.719f, -8.54f, -8.415412f));
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocated1xId, Vector3.one * 0.4f);
            if (isassetsbig)
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocated1xId, Vector3.one * 3.5f);
        }

        public static void UForsaken()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocated1xId);
        }

        private static int allocatedMiniTravisId = -1;

        public static void MiniTravis()
        {
            allocatedMiniTravisId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "minitravis", "travisscott",
                allocatedMiniTravisId);
            Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedMiniTravisId, 1);
            Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, allocatedMiniTravisId,
                new Vector3(-0.6f, 0.2f, 0f));
            Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, allocatedMiniTravisId,
                new Vector3(80f, 160f, 180f));
        }

        public static void UMiniTravis() =>
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedMiniTravisId);

        private static int allocatedRSwordId = -1;
        private static bool lastVelTooHighRS;
        private static float pauseSfx;
        private static float slashDelay;

        public static void RSword()
        {
            if (allocatedRSwordId < 0)
            {
                allocatedRSwordId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "rbsword", "Sword", allocatedRSwordId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedRSwordId, 2);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedRSwordId, "Sword", "Music");

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedRSwordId, Vector3.one * 5);

                RPCProtection();
            }

            if (!Console.consoleAssets.TryGetValue(allocatedRSwordId, out Console.ConsoleAsset asset))
                return;

            Transform rayPoint = asset.assetObject.transform.Find("Sword/HitBox");

            Physics.SphereCast(rayPoint.position, 0.1f, rayPoint.forward, out RaycastHit Ray, 0.7f, NoInvisLayerMask());

            if (Time.time > slashDelay && Ray.collider != null)
                try
                {
                    VRRig Target = Ray.collider.GetComponentInParent<VRRig>();
                    if (Target != null && !Target.isLocal)
                    {
                        slashDelay = Time.time + 0.5f;
                        pauseSfx = Time.time + 1f;
                        Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedRSwordId, "Sword/SFX",
                            $"Slash{Random.Range(1, 3)}");
                        Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedRSwordId, "Sword",
                            "Particles");

                        NetPlayer player = Target.Creator;
                        Console.ExecuteCommand("silkick", player.ActorNumber, player.UserId);
                    }
                }
                catch
                {
                }

            bool velTooHigh =
                (GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) -
                 GorillaTagger.Instance.rigidbody.linearVelocity).magnitude > 10f;

            if (velTooHigh && !lastVelTooHighRS && Time.time > pauseSfx)
            {
                pauseSfx = Time.time + 0.3f;
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedRSwordId, "Sword/SFX",
                    $"Swing{Random.Range(1, 3)}");
            }

            lastVelTooHighRS = velTooHigh;
        }

        public static void URSword()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedRSwordId);
            allocatedRSwordId = -1;
        }

        public static void AdminKickAll() =>
            Console.ExecuteCommand("kickall", ReceiverGroup.All);

        public static void AdminCrashGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("crash", GetPlayerFromVRRig(gunTarget).ActorNumber);
                    }
                }
            }
        }

        public static void AdminCrashAll() =>
            Console.ExecuteCommand("crash", ReceiverGroup.Others);

        public static void AdminLagSpikeGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.5f;
                        Console.ExecuteCommand("sleep", GetPlayerFromVRRig(gunTarget).ActorNumber, 1000);
                    }
                }
            }
        }

        public static void AdminLagGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("sleep", GetPlayerFromVRRig(lockTarget).ActorNumber, 50);
                        RPCProtection();
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static void AdminLagSpikeAll() =>
            Console.ExecuteCommand("sleep", ReceiverGroup.Others, 1000);

        public static void AdminLagAll()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.1f;
                Console.ExecuteCommand("sleep", ReceiverGroup.Others, 50);
                RPCProtection();
            }
        }

        public static void AdminGiveFlyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.rightThumb.calcT > 0.5f)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("vel", GetPlayerFromVRRig(lockTarget).ActorNumber,
                                lockTarget.headMesh.transform.forward * Movement._flySpeed);
                            RPCProtection();
                        }
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static bool AdminPlatformsLastLeft;
        public static bool AdminPlatformsLastRight;

        public static void AdminGivePlatforms()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.leftMiddle.calcT > 0.5f && !AdminPlatformsLastLeft)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("platf", GetPlayerFromVRRig(lockTarget).ActorNumber,
                                lockTarget.leftHandTransform.position - new Vector3(0f, 0.2f, 0f),
                                new Vector3(0.1f, 0.5f, 0.3f), lockTarget.leftHandTransform.eulerAngles,
                                Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f, 10f);
                            RPCProtection();
                        }

                        if (lockTarget.rightMiddle.calcT > 0.5f && !AdminPlatformsLastRight)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("platf", GetPlayerFromVRRig(lockTarget).ActorNumber,
                                lockTarget.rightHandTransform.position - new Vector3(0f, 0.2f, 0f),
                                new Vector3(0.1f, 0.5f, 0.3f), lockTarget.rightHandTransform.eulerAngles,
                                Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f, 10f);
                            RPCProtection();
                        }

                        AdminPlatformsLastLeft = lockTarget.leftMiddle.calcT > 0.5f;
                        AdminPlatformsLastRight = lockTarget.rightMiddle.calcT > 0.5f;
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static void AdminGiveTriggerFlyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        if (lockTarget.rightIndex.calcT > 0.5f)
                        {
                            adminEventDelay = Time.time + 0.1f;
                            Console.ExecuteCommand("vel", GetPlayerFromVRRig(lockTarget).ActorNumber,
                                lockTarget.headMesh.transform.forward * Movement._flySpeed);
                            RPCProtection();
                        }
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static Vector3 speedLastVel;

        public static void AdminGiveSpeedGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(lockTarget).ActorNumber,
                            (lockTarget.bodyTransform.position - speedLastVel) * 6f);
                        speedLastVel = lockTarget.bodyTransform.position;
                        RPCProtection();
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        speedLastVel = gunTarget.bodyTransform.position;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static void AdminGiveLowGravity()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(lockTarget).ActorNumber,
                            (lockTarget.bodyTransform.position - speedLastVel) * 5f + Vector3.up * 0.5f);
                        speedLastVel = lockTarget.bodyTransform.position;
                        RPCProtection();
                    }
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        gunLocked = true;
                        speedLastVel = gunTarget.bodyTransform.position;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                gunLocked = false;
            }
        }

        public static void AdminVibrateGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("vibrate", GetPlayerFromVRRig(gunTarget).ActorNumber, 3, 1f);
                    }
                }
            }
        }

        public static void AdminVibrateAll() =>
            Console.ExecuteCommand("vibrate", ReceiverGroup.Others, 3, 1f);

        public static void AdminBMuteGun(bool mute)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.5f;
                        Console.ExecuteCommand(mute ? "mute" : "unmute", ReceiverGroup.All,
                            GetPlayerFromVRRig(gunTarget).UserId);
                    }
                }
            }
        }

        public static void AdminBlockGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 5f;
                        Console.ExecuteCommand("block", GetPlayerFromVRRig(gunTarget).ActorNumber, 300L);
                    }
                }
            }
        }

        public static void AdminABlockGun(bool Silent)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 5f;
                        Console.ExecuteCommand("notify", ReceiverGroup.All,
                            GetPlayerFromVRRig(gunTarget).NickName + " has been blocked" + (Silent
                                ? ""
                                : " by " + ServerData.Administrators[PhotonNetwork.LocalPlayer.UserId]) + ".");
                        Console.ExecuteCommand("block", GetPlayerFromVRRig(gunTarget).ActorNumber, 300L);
                        RPCProtection();
                    }
                }
            }
        }

        public static void AdminBMuteAll(bool mute) =>
            Console.ExecuteCommand(mute ? "muteall" : "unmuteall", ReceiverGroup.All);

        public static void AdminButtonPressGun(string key)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.8f;
                        Console.ExecuteCommand("controller", GetPlayerFromVRRig(gunTarget).ActorNumber, key, 1f, 1f);
                        RPCProtection();
                    }
                }
            }
        }

        public static void FlipMenuGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("toggle", GetPlayerFromVRRig(gunTarget).ActorNumber, "Right Hand");
                    }
                }
            }
        }

        public static void AdminEnableGun(bool enable, string mod)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("forceenable", GetPlayerFromVRRig(gunTarget).ActorNumber, mod, enable);
                    }
                }
            }
        }

        private static float jumpscareDelay;

        public static void AdminJumpscareGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > jumpscareDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        jumpscareDelay = Time.time + 0.2f;
                        Console.ExecuteCommand("toggle", GetPlayerFromVRRig(gunTarget).ActorNumber, "Jumpscare");
                    }
                }
            }
        }

        public static void AdminJumpscareAll() =>
            Console.ExecuteCommand("toggle", ReceiverGroup.Others, "Jumpscare");

        public static bool muted;

        public static void AdminMute()
        {
            if (leftTrigger > 0.5f && !muted)
            {
                Console.ExecuteCommand("forceenable", ReceiverGroup.Others, "Mute Microphone", true);
                muted = true;
            }
            else if (leftTrigger < 0.5f && muted)
            {
                Console.ExecuteCommand("forceenable", ReceiverGroup.Others, "Mute Microphone", false);
                muted = false;
            }

        }

        private static readonly Dictionary<VRRig, Coroutine> freezePool = new Dictionary<VRRig, Coroutine>();

        private static IEnumerator FreezeCoroutine(VRRig rig)
        {
            Console.ExecuteCommand("forceenable", GetPlayerFromVRRig(rig).ActorNumber, "Zero Gravity", true);
            Vector3 pos = rig.transform.position;
            while (VRRigCache.ActiveRigs.Contains(rig))
            {
                Console.ExecuteCommand("tp", GetPlayerFromVRRig(rig).ActorNumber, pos);
                yield return new WaitForSeconds(0.1f);
            }
        }

        public static void AdminFreezeGun(bool freeze)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        switch (freeze)
                        {
                            case true when !freezePool.ContainsKey(gunTarget):
                                freezePool.Add(gunTarget,
                                    CoroutineManager.instance.StartCoroutine(FreezeCoroutine(gunTarget)));
                                break;
                            case false when freezePool.ContainsKey(gunTarget):
                                CoroutineManager.instance.StopCoroutine(freezePool[gunTarget]);
                                Console.ExecuteCommand("forceenable", GetPlayerFromVRRig(gunTarget).ActorNumber,
                                    "Zero Gravity", false);
                                freezePool.Remove(gunTarget);
                                break;
                        }
                    }
                }
            }
        }

        public static void AdminTeleportGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("tp", ReceiverGroup.Others, NewPointer.transform.position);
                }
            }
        }

        public static void AdminFlingGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(gunTarget).ActorNumber,
                            new Vector3(0f, 50f, 0f));
                    }
                }
            }
        }

        public static void AdminCrashBypassGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        if (ServerData.Administrators.ContainsKey(GetPlayerFromVRRig(gunTarget).UserId))
                            return;
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(gunTarget).ActorNumber,
                            new Vector3(0f, 1000000f, 0f));
                    }
                }
            }
        }

        public static void AdminLockdownGun(bool enable)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("togglemenu", GetPlayerFromVRRig(gunTarget).ActorNumber, enable);
                    }
                }
            }
        }

        private static readonly List<int> FullActorNumbers = new List<int>();

        public static void FullToggleMenu(int actorNumber, bool enable)
        {
            if (enable)
            {
                if (!FullActorNumbers.Contains(actorNumber))
                {
                    Console.ExecuteCommand("forceenable", actorNumber, "Disable Autosave", true);
                    Console.ExecuteCommand("forceenable", actorNumber, "Load Preferences");
                    FullActorNumbers.Add(actorNumber);
                }
            }
            else
            {
                if (FullActorNumbers.Contains(actorNumber))
                {
                    Console.ExecuteCommand("toggle", actorNumber, "Save Preferences");
                    Console.ExecuteCommand("forceenable", actorNumber, "Disable Autosave", true);
                    Console.ExecuteCommand("forceenable", actorNumber, "Panic", true);
                    FullActorNumbers.Remove(actorNumber);
                }
            }

            Console.ExecuteCommand("togglemenu", actorNumber, enable);
        }

        public static void AdminFullLockdownGun(bool enable)
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        FullToggleMenu(GetPlayerFromVRRig(gunTarget).ActorNumber, enable);
                    }
                }
            }
        }

        private static bool lastInRoom2;
        private static int lastPlayerCount2 = -1;

        public static void AdminLockdownAll(bool enable)
        {
            if (PhotonNetwork.InRoom && (!lastInRoom2 || PhotonNetwork.PlayerList.Length != lastPlayerCount2))
                Console.ExecuteCommand("togglemenu", ReceiverGroup.Others, enable);

            lastInRoom2 = PhotonNetwork.InRoom;
            lastPlayerCount2 = PhotonNetwork.PlayerList.Length;
            if (!PhotonNetwork.InRoom)
                lastPlayerCount2 = -1;
        }

        public static void AdminFullLockdownAll(bool enable)
        {
            foreach (NetPlayer Player in NetworkSystem.Instance.PlayerListOthers)
                FullToggleMenu(Player.ActorNumber, enable);
        }

        private static float stdell;
        private static VRRig thestrangled;
        private static VRRig thestrangledleft;

        public static void AdminStrangle()
        {
            if (leftGrab)
            {
                if (thestrangledleft == null)
                {
                    foreach (var rig in VRRigCache.ActiveRigs.Where(rig => !rig.isLocal).Where(rig =>
                                 Vector3.Distance(rig.headMesh.transform.position,
                                     GorillaTagger.Instance.leftHandTransform.position) < 0.2f))
                    {
                        thestrangledleft = rig;
                        if (PhotonNetwork.InRoom)
                            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                        else
                            VRRig.LocalRig.PlayHandTapLocal(89, true, 999999f);
                    }
                }
                else
                {
                    if (Time.time > stdell)
                    {
                        stdell = Time.time + 0.05f;
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            GorillaTagger.Instance.leftHandTransform.position);
                    }
                }
            }
            else
            {
                if (thestrangledleft != null)
                {
                    try
                    {
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            GorillaTagger.Instance.leftHandTransform.position);
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangledleft).ActorNumber,
                            GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0));
                    }
                    catch
                    {
                    }

                    thestrangledleft = null;
                    if (PhotonNetwork.InRoom)
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, true, 999999f);
                    else
                        VRRig.LocalRig.PlayHandTapLocal(89, true, 999999f);
                }
            }

            if (rightGrab)
            {
                if (thestrangled == null)
                {
                    foreach (var rig in VRRigCache.ActiveRigs.Where(rig => !rig.isLocal).Where(rig =>
                                 Vector3.Distance(rig.headMesh.transform.position,
                                     GorillaTagger.Instance.rightHandTransform.position) < 0.2f))
                    {
                        thestrangled = rig;
                        if (PhotonNetwork.InRoom)
                            GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false,
                                999999f);
                        else
                            VRRig.LocalRig.PlayHandTapLocal(89, false, 999999f);
                    }
                }
                else
                {
                    if (Time.time > adminEventDelay)
                    {
                        adminEventDelay = Time.time + 0.05f;
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            GorillaTagger.Instance.rightHandTransform.position);
                    }
                }
            }
            else
            {
                if (thestrangled != null)
                {
                    try
                    {
                        Console.ExecuteCommand("tp", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            GorillaTagger.Instance.rightHandTransform.position);
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(thestrangled).ActorNumber,
                            GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0));
                    }
                    catch
                    {
                    }

                    thestrangled = null;
                    if (PhotonNetwork.InRoom)
                        GorillaTagger.Instance.myVRRig.SendRPC("RPC_PlayHandTap", RpcTarget.All, 89, false, 999999f);
                    else
                        VRRig.LocalRig.PlayHandTapLocal(89, false, 999999f);
                }
            }
        }

        public static void AdminObjectGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("platf", ReceiverGroup.All, NewPointer.transform.position);
                }
            }
        }

        public static void AdminRandomObjectGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("platf", ReceiverGroup.All, NewPointer.transform.position, RandomVector3(),
                        RandomVector3(360f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
                }
            }
        }

        private static float lastnetscale = 1f;
        private static float scalenetdel;
        private static int lastplayercount;

        public static void AdminNetworkScale()
        {
            if (Time.time > scalenetdel && (!Mathf.Approximately(lastnetscale, VRRig.LocalRig.scaleFactor) ||
                                            PhotonNetwork.PlayerList.Length != lastplayercount))
            {
                Console.ExecuteCommand("scale", ReceiverGroup.All, VRRig.LocalRig.scaleFactor);
                scalenetdel = Time.time + 0.05f;
                lastnetscale = VRRig.LocalRig.scaleFactor;
                lastplayercount = PhotonNetwork.PlayerList.Length;
            }
        }

        public static void UnAdminNetworkScale() =>
            Console.ExecuteCommand("scale", ReceiverGroup.All, 1f);

        public static void LightningGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("strike", ReceiverGroup.All, NewPointer.transform.position);
                }
            }
        }

        public static void LightningAura()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("strike", ReceiverGroup.All,
                    GorillaTagger.Instance.headCollider.transform.position + new Vector3(
                        MathF.Cos((float)Time.frameCount / 30), 1f, MathF.Sin((float)Time.frameCount / 30)));
            }
        }

        public static void LightningRain()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.1f;
                Physics.Raycast(
                    GorillaTagger.Instance.headCollider.transform.position +
                    new Vector3(Random.Range(-10f, 10f), 10f, Random.Range(-10f, 10f)), Vector3.down, out var Ray, 512f,
                    NoInvisLayerMask());
                VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                if (gunTarget && !gunTarget.IsLocal())
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("kick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                }
                else
                    Console.ExecuteCommand("strike", ReceiverGroup.All, Ray.point);
            }
        }

        private static Vector3 whereOriginalPlayerPos = Vector3.zero;
        private static Vector3 originalMePosition = Vector3.zero;

        public static void AdminFearGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (gunLocked && lockTarget != null)
                {
                    TeleportPlayer(lockTarget.transform.position + lockTarget.transform.forward);
                    if (Time.time > adminEventDelay)
                        adminEventDelay = Time.time + 0.1f;
                }

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        originalMePosition = GorillaTagger.Instance.bodyCollider.transform.position;
                        whereOriginalPlayerPos = gunTarget.transform.position;

                        int actorNumber = GetPlayerFromVRRig(gunTarget).ActorNumber;
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(0f, 16f, 0f), new Vector3(10f, 1f, 10f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(0f, 24f, 0f), new Vector3(10f, 1f, 10f));

                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(4f, 20f, 0f), new Vector3(1f, 10f, 10f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(-4f, 20f, 0f), new Vector3(1f, 10f, 10f));

                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(0f, 20f, 4f), new Vector3(10f, 10f, 1f));
                        Console.ExecuteCommand("platf", new[] { actorNumber, PhotonNetwork.LocalPlayer.ActorNumber },
                            new Vector3(0f, 20f, -4f), new Vector3(10f, 10f, 1f));

                        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(platform, 60f);
                        platform.GetComponent<Renderer>().material.color = Color.black;
                        platform.transform.position = new Vector3(0f, 20f, 0f);
                        platform.transform.localScale = new Vector3(10f, 1f, 10f);

                        gunLocked = true;
                        lockTarget = gunTarget;
                    }
                }
            }
            else
            {
                if (gunLocked)
                {
                    gunLocked = false;

                    TeleportPlayer(originalMePosition);
                    Console.ExecuteCommand("tpnv", GetPlayerFromVRRig(lockTarget).ActorNumber, whereOriginalPlayerPos);
                    Console.ExecuteCommand("unmuteall", GetPlayerFromVRRig(lockTarget).ActorNumber);
                }
            }
        }

        public static void EnableNoAdminIndicator()
        {
            Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
            lastplayercount = -1;
        }

        public static void NoAdminIndicator()
        {
            if (!PhotonNetwork.InRoom)
                lastplayercount = -1;

            if (PhotonNetwork.PlayerList.Length != lastplayercount && PhotonNetwork.InRoom)
            {
                Console.ExecuteCommand("nocone", ReceiverGroup.All, true);
                lastplayercount = PhotonNetwork.PlayerList.Length;
            }
        }

        public static void AdminIndicatorBack() =>
            Console.ExecuteCommand("nocone", ReceiverGroup.All, false);

        public static void EnableAdminMenuUserTags()
        {
            if (!userTagHooked)
            {
                userTagHooked = true;
                PhotonNetwork.NetworkingClient.EventReceived += AdminUserTagSys;
            }
        }

        private static bool lastInRoom;
        private static int lastPlayerCount = -1;

        public static bool userTagHooked;

        public static void AdminUserTagSys(EventData data)
        {
            try
            {
                Player sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (data.Code == Console.ConsoleByte && sender != PhotonNetwork.LocalPlayer)
                {
                    object[] args = (object[])data.CustomData;
                    string command = (string)args[0];
                    switch (command)
                    {
                        case "confirmusing":
                            if (Buttons.GetIndex("Menu User Name Tags").enabled &&
                                ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            {
                                VRRig vrrig = GetVRRigFromPlayer(sender);
                                if (!nametags.TryGetValue(vrrig, out var nametag))
                                {
                                    GameObject go = new GameObject("Seralyth_MenuUserNametag");
                                    go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                                    TextMeshPro textMesh = go.AddComponent<TextMeshPro>();
                                    textMesh.fontSize = 4.8f;
                                    textMesh.alignment = TextAlignmentOptions.Center;

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = ToTitleCase((string)args[2]);

                                    nametags.Add(vrrig, go);
                                }
                                else
                                {
                                    TextMeshPro textMesh = nametag.GetComponent<TextMeshPro>();

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    if (Visuals.nameTagChams)
                                        textMesh.Chams();
                                    textMesh.color = userColor;
                                    textMesh.text = ToTitleCase((string)args[2]);
                                }
                            }

                            if (Buttons.GetIndex("Conduct Menu Users").enabled)
                            {
                                if (!onConduct.ContainsKey(sender.UserId))
                                {
                                    bool add = ServerData.Administrators.ContainsKey(sender.UserId);
                                    string txt = sender.NickName + " - " + ToTitleCase((string)args[2]);
                                    if (add)
                                        txt = "<color=red>" + txt + "</color>";
                                    onConduct.Add(sender.UserId, txt);
                                }
                            }

                            if (Buttons.GetIndex("Admin Find User").enabled)
                                isUserFound = true;
                            break;
                    }
                }
            }
            catch
            {
            }
        }

        private static readonly Dictionary<VRRig, GameObject> nametags = new Dictionary<VRRig, GameObject>();

        public static void AdminMenuUserTags()
        {
            if (PhotonNetwork.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount))
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            lastInRoom = PhotonNetwork.InRoom;
            lastPlayerCount = PhotonNetwork.PlayerList.Length;
            if (!PhotonNetwork.InRoom)
                lastPlayerCount = -1;

            foreach (KeyValuePair<VRRig, GameObject> nametag in nametags.ToList())
            {
                if (!VRRigCache.ActiveRigs.Contains(nametag.Key))
                {
                    Object.Destroy(nametag.Value);
                    nametags.Remove(nametag.Key);
                }
                else
                {
                    nametag.Value.GetComponent<TextMeshPro>().fontStyle = activeFontStyle;
                    nametag.Value.GetComponent<TextMeshPro>().font = activeFont;

                    if (Visuals.nameTagChams)
                        nametag.Value.GetComponent<TextMeshPro>().Chams();

                    nametag.Value.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f) * nametag.Key.scaleFactor;

                    nametag.Value.transform.position = Visuals.GetNameTagPosition(nametag.Key);
                    nametag.Value.transform.LookAt(Camera.main.transform.position);
                    nametag.Value.transform.Rotate(0f, 180f, 0f);
                }
            }
        }

        public static void DisableAdminMenuUserTags()
        {
            foreach (KeyValuePair<VRRig, GameObject> nametag in nametags)
                Object.Destroy(nametag.Value);

            nametags.Clear();
        }

        public static bool tracerTagHooked;

        public static void EnableAdminMenuUserTracers()
        {
            if (!tracerTagHooked)
            {
                tracerTagHooked = true;
                PhotonNetwork.NetworkingClient.EventReceived += AdminTracerSys;
            }
        }

        private static readonly Dictionary<VRRig, string> menuUsers = new Dictionary<VRRig, string>();

        public static void AdminTracerSys(EventData data)
        {
            try
            {
                Player sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(data.Sender);
                if (data.Code == Console.ConsoleByte && sender != PhotonNetwork.LocalPlayer)
                {
                    object[] args = (object[])data.CustomData;
                    string command = (string)args[0];
                    switch (command)
                    {
                        case "confirmusing":
                            if (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId))
                            {
                                VRRig vrrig = GetVRRigFromPlayer(sender);
                                if (!nametags.TryGetValue(vrrig, out var nametag))
                                {
                                    GameObject go = new GameObject("Seralyth_Nametag");
                                    go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                                    TextMeshPro textMesh = go.AddComponent<TextMeshPro>();
                                    textMesh.fontSize = 48;
                                    textMesh.alignment = TextAlignmentOptions.Center;

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = ToTitleCase((string)args[2]);

                                    nametags.Add(vrrig, go);
                                }
                                else
                                {
                                    TextMeshPro textMesh = nametag.GetComponent<TextMeshPro>();

                                    Color userColor = Color.red;
                                    if (args.Length > 2)
                                        userColor = Console.GetMenuTypeName((string)args[2]);

                                    textMesh.color = userColor;
                                    textMesh.text = ToTitleCase((string)args[2]);
                                }
                            }

                            break;
                    }
                }
            }
            catch
            {
            }
        }

        public static void MenuUserTracers()
        {
            if (PhotonNetwork.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount))
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            lastInRoom = PhotonNetwork.InRoom;
            lastPlayerCount = PhotonNetwork.PlayerList.Length;
            if (!PhotonNetwork.InRoom)
                lastPlayerCount = -1;

            if (Visuals.DoPerformanceCheck())
                return;

            bool followMenuTheme = Buttons.GetIndex("Follow Menu Theme").enabled;
            bool transparentTheme = Buttons.GetIndex("Transparent Theme").enabled;
            _ = Buttons.GetIndex("Hidden on Camera").enabled;
            float lineWidth = (Buttons.GetIndex("Thin Tracers").enabled ? 0.0075f : 0.025f) *
                              (scaleWithPlayer ? GTPlayer.Instance.scale : 1f);

            Color menuColor = backgroundColor.GetCurrentColor();

            foreach (KeyValuePair<VRRig, string> userData in menuUsers)
            {
                VRRig playerRig = userData.Key;
                if (playerRig.isLocal)
                    continue;

                Color lineColor = Console.GetMenuTypeName(userData.Value);

                LineRenderer line = Visuals.GetLineRender();

                if (followMenuTheme)
                    lineColor = menuColor;

                if (transparentTheme)
                    lineColor.a = 0.5f;

                line.startColor = lineColor;
                line.endColor = lineColor;
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
                line.SetPosition(1, playerRig.transform.position);
            }
        }

        public static readonly Dictionary<string, string> onConduct = new Dictionary<string, string>();

        public static void ConsoleOnConduct()
        {
            if (PhotonNetwork.InRoom && (!lastInRoom || PhotonNetwork.PlayerList.Length != lastPlayerCount) &&
                !Buttons.GetIndex("Menu User Name Tags").enabled)
                Console.ExecuteCommand("isusing", ReceiverGroup.All);

            string conductText = "";
            conductText += "<color=red>" + PhotonNetwork.LocalPlayer.NickName + " - " + ToTitleCase(Console.MenuName) +
                           "</color>\\n";
            foreach (KeyValuePair<string, string> item in onConduct)
            {
                if (GetPlayerFromID(item.Key) == null)
                    onConduct.Remove(item.Key);
                else
                    conductText += item.Value + "\\n";
            }

            GetObject("Environment Objects/LocalObjects_Prefab/TreeRoom/COCBodyText_TitleData")
                .GetComponent<TextMeshPro>().text = conductText;
        }

        public static float FindUserTime;
        public static bool isUserFound;

        public static void AdminFindUser()
        {
            if (Time.time < FindUserTime)
                return;

            if (!PhotonNetwork.InRoom)
            {
                Important.JoinRandom();
                isUserFound = false;
                FindUserTime = Time.time + 7f;
            }
            else
            {
                if (isUserFound)
                {
                    NotificationManager.SendNotification(
                        "<color=grey>[</color><color=green>SUCCESS</color><color=grey>]</color> Found menu user!");
                    Buttons.GetIndex("Admin Find User").enabled = false;
                    isUserFound = false;
                    return;
                }

                NotificationManager.SendNotification("Nobody found, searching for players.");
                NetworkSystem.Instance.ReturnToSinglePlayer();
                FindUserTime = Time.time + 2f;
            }
        }

        private static float thingdeb;

        public static void AdminPunchMod()
        {
            if (Time.time > thingdeb)
            {
                foreach (VRRig rig in VRRigCache.ActiveRigs)
                {
                    bool leftHand = Vector3.Distance(GorillaTagger.Instance.leftHandTransform.position,
                        rig.headMesh.transform.position) < 0.25f;
                    bool rightHand = Vector3.Distance(GorillaTagger.Instance.rightHandTransform.position,
                        rig.headMesh.transform.position) < 0.25f;

                    if (!rig.isLocal && (leftHand || rightHand))
                    {
                        Vector3 vel = rightHand
                            ? GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0)
                            : GTPlayer.Instance.LeftHand.velocityTracker.GetAverageVelocity(true, 0);

                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(rig).ActorNumber, vel);
                        thingdeb = Time.time + 0.1f;
                    }
                }
            }
        }

        public static string targetRoom;

        public static void GetTargetRoom() =>
            PromptText("What room would you like the users to join?", () => targetRoom = keyboardInput, null, "Done",
                "Cancel");

        public static void JoinGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("join", GetPlayerFromVRRig(gunTarget).ActorNumber, targetRoom.ToUpper());
                    }
                }
            }
        }

        public static void JoinAll() =>
            PromptText("What room would you like the users to join?",
                () => Console.ExecuteCommand("join", ReceiverGroup.Others, keyboardInput.ToUpper()), null, "Done",
                "Cancel");

        public static string targetNotification;

        public static void GetTargetNotification()
        {
            PromptText("What notification would you like to send?", () =>
            {
                targetNotification = keyboardInput;
                Buttons.GetIndex("NotifLabel").overlapText = "Notif: " + keyboardInput;
            }, null, "Done", "Cancel");
        }

        public static void NotifySelf() =>
            Console.ExecuteCommand("notify", PhotonNetwork.LocalPlayer.ActorNumber, targetNotification);

        public static void NotifyGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("notify", GetPlayerFromVRRig(gunTarget).ActorNumber, targetNotification);
                    }
                }
            }
        }

        public static void NotifyAll() =>
            Console.ExecuteCommand("notify", ReceiverGroup.All, targetNotification);

        public static void GetMenuUsers()
        {
            Console.indicatorDelay = Time.time + 2f;
            Console.ExecuteCommand("isusing", ReceiverGroup.All);
        }

        private static bool lastLasering;

        public static void AdminLaser()
        {
            if (leftPrimary || rightPrimary)
            {
                Vector3 dir = rightPrimary
                    ? VRRig.LocalRig.rightHandTransform.right
                    : -VRRig.LocalRig.leftHandTransform.right;
                Vector3 startPos =
                    (rightPrimary
                        ? VRRig.LocalRig.rightHandTransform.position
                        : VRRig.LocalRig.leftHandTransform.position) + dir * 0.1f;
                try
                {
                    Physics.Raycast(startPos + dir / 3f, dir, out var Ray, 512f, NoInvisLayerMask());
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                        Console.ExecuteCommand("silkick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                }
                catch
                {
                }

                if (Time.time > adminEventDelay)
                {
                    adminEventDelay = Time.time + 0.1f;
                    Console.ExecuteCommand("laser", ReceiverGroup.All, true, rightPrimary);
                }
            }

            bool isLasering = leftPrimary || rightPrimary;
            if (lastLasering && !isLasering)
                Console.ExecuteCommand("laser", ReceiverGroup.All, false, false);

            lastLasering = isLasering;
        }

        private static float beamDelay;

        public static void AdminBeam()
        {
            if (rightTrigger > 0.5f && Time.time > beamDelay)
            {
                beamDelay = Time.time + 0.05f;
                float h = Time.frameCount / 180f % 1f;
                Color color = Color.HSVToRGB(h, 1f, 1f);
                Console.ExecuteCommand("lr", ReceiverGroup.All, color.r, color.g, color.b, color.a, 0.5f,
                    GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 0.5f, 0f),
                    GorillaTagger.Instance.headCollider.transform.position + new Vector3(
                        Mathf.Cos((float)Time.frameCount / 30) * 100f, 0.5f,
                        Mathf.Sin((float)Time.frameCount / 30) * 100f), 0.1f);
            }
        }

        private static float startTimeTrigger;
        private static bool lastTriggerLaserSpam;

        public static void AdminFractals()
        {
            if (rightTrigger > 0.5f && !lastTriggerLaserSpam)
                startTimeTrigger = Time.time;

            lastTriggerLaserSpam = rightTrigger > 0.5f;

            if (rightTrigger > 0.5f && Time.time > beamDelay)
            {
                beamDelay = Time.time + 0.5f;
                float h = Time.frameCount / 180f % 1f;
                Color.HSVToRGB(h, 1f, 1f);
                Console.ExecuteCommand("lr", ReceiverGroup.All, "lr", 0f, 1f, 1f, 0.3f, 0.25f,
                    GorillaTagger.Instance.bodyCollider.transform.position,
                    GorillaTagger.Instance.headCollider.transform.position +
                    new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 1000f,
                    20f - (Time.time - startTimeTrigger));
            }
        }

        public static void FlyAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("vel", ReceiverGroup.Others, new Vector3(0f, 10f, 0f));
            }
        }

        public static void BouncyAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;

                var users = Console.userDictionary.Keys.Where(u => !u.IsLocal).ToList();

                foreach (var rig in users.Select(player => GetVRRigFromPlayer(player)))
                {
                    if (!Physics.Raycast(rig.bodyTransform.position - new Vector3(0f, 0.2f, 0f), Vector3.down,
                            out RaycastHit hit, 512f, GTPlayer.Instance.locomotionEnabledLayers)) continue;
                    if (!(hit.distance < 0.1f)) continue;
                    Vector3 surfaceNormal = hit.normal;
                    Vector3 bodyVelocity = rig.LatestVelocity();
                    Vector3 reflectedVelocity = Vector3.Reflect(bodyVelocity, surfaceNormal);
                    Vector3 finalVelocity = reflectedVelocity * 2f;
                    Console.ExecuteCommand("vel", rig.GetPlayer().ActorNumber, finalVelocity);
                }
            }
        }



        public static void AdminBringGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        adminEventDelay = Time.time + 0.1f;
                        Console.ExecuteCommand("tpnv", GetPlayerFromVRRig(gunTarget).ActorNumber,
                            GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 1.5f, 0f));
                    }
                }
            }
        }

        public static void BringAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others,
                    GorillaTagger.Instance.headCollider.transform.position + new Vector3(0f, 1.5f, 0f));
            }
        }

        public static void AdminOrganizeGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (GetGunInput(true) && Time.time > adminEventDelay)
                {
                    var users = Console.userDictionary.Keys.Where(u => !u.IsLocal).ToList();
                    if (users.Count == 1)
                    {
                        Console.ExecuteCommand("tpnv", users.FirstOrDefault().ActorNumber,
                            NewPointer.transform.position);
                        return;
                    }

                    float spacing = 0.8f;
                    for (int i = 0; i < users.Count; i++)
                    {
                        Console.ExecuteCommand("tpnv", users[i].ActorNumber,
                            NewPointer.transform.position - Vector3.right * ((users.Count - 1) * spacing / 2f) +
                            Vector3.right * (spacing * i));
                    }

                    adminEventDelay = Time.time + 0.05f;
                }
            }
        }

        public static void BringHandAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others,
                    ControllerUtilities.GetTrueRightHand().position + ControllerUtilities.GetTrueRightHand().forward);
            }
        }

        public static void BringHeadAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others,
                    GorillaTagger.Instance.headCollider.transform.position +
                    GorillaTagger.Instance.headCollider.transform.forward);
            }
        }

        public static void OrbitAllUsing()
        {
            if (Time.time > adminEventDelay)
            {
                adminEventDelay = Time.time + 0.05f;
                Console.ExecuteCommand("tpnv", ReceiverGroup.Others,
                    GorillaTagger.Instance.headCollider.transform.position +
                    new Vector3(Mathf.Cos(Time.frameCount / 20f), 0.5f, Mathf.Sin(Time.frameCount / 20f)));
            }
        }

        public static void ConfirmNotifyAllUsing() =>
            Console.ExecuteCommand("notify", ReceiverGroup.All,
                ServerData.Administrators[PhotonNetwork.LocalPlayer.UserId] == "kingofnetflix"
                    ? "Yes, I am kingofnetflix. I made the menu."
                    : "Yes, I am " + ServerData.Administrators[PhotonNetwork.LocalPlayer.UserId] +
                      ". I am a Console admin.");

        public static int[] oldCosmetics;
        public static int[] oldTryOn;

        public static void AdminSpoofCosmetics(bool forceRun = false)
        {
            if (PhotonNetwork.InRoom)
            {
                if (oldCosmetics != CosmeticsController.instance.currentWornSet.ToPackedIDArray() || forceRun)
                {
                    oldCosmetics = CosmeticsController.instance.currentWornSet.ToPackedIDArray();
                    string[] cosmetics = CosmeticsController.instance.currentWornSet.ToDisplayNameArray()
                        .Where(c => !string.Equals(c, "NOTHING", StringComparison.OrdinalIgnoreCase)).ToArray();

                    Console.ExecuteCommand("cosmetics", ReceiverGroup.Others, cosmetics);
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others,
                        CosmeticsController.instance.currentWornSet.ToPackedIDArray(),
                        CosmeticsController.instance.tryOnSet.ToPackedIDArray(), false);
                }
            }
        }


        private static int allocatedCoinId = -1;
        public static int coinChain;
        public static bool coinChainHeads;
        public static int coinHeads;
        public static int coinTails;
        private static bool lastFlipping;

        public static void CoinFlip()
        {
            if (rightGrab && rightTrigger > 0.5f)
            {
                if (allocatedCoinId == -1 && (rightPrimary || rightSecondary))
                {
                    allocatedCoinId = Console.GetFreeAssetID();
                    Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Coin", allocatedCoinId);
                    Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedCoinId, 2);
                    RPCProtection();
                }

                if (allocatedCoinId == -1) return;

                bool flipping = rightPrimary || rightSecondary;

                if (!flipping && lastFlipping)
                {
                    bool heads = Random.Range(0f, 1f) >= 0.5f;
                    if (heads != coinChainHeads)
                    {
                        coinChain = 0;
                        coinChainHeads = heads;
                    }

                    coinChain++;

                    if (heads) coinHeads++;
                    else coinTails++;

                    Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedCoinId, "CoinHolder",
                        heads ? "Heads" : "Tails");
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedCoinId, "CoinHolder", "Flip");
                }

                lastFlipping = flipping;
            }
            else
            {
                lastFlipping = false;

                if (allocatedCoinId != -1)
                {
                    Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedCoinId);
                    allocatedCoinId = -1;
                }
            }
        }


        public static void UCoinFlip()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedCoinId);
            allocatedCoinId = -1;
        }

        public static int assetId;
        public static bool hastwerked = false;

        public static void TwerkingCarti()
        {
            if (!hastwerked)
            {
                assetId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "carti",
                    assetId);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, assetId,
                    new Vector3(-76f, 1.7f, -80f));

                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, assetId, Quaternion.Euler(0f, 40f, 0f));

                if (!isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, assetId, Vector3.one * 5f);
                else
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, assetId, Vector3.one * 10f);
                hastwerked = true;
            }
        }

        public static void NoCarti()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);
            hastwerked = false;
        }



        private static int allocatedAxeId = -1;

        public static void Axe()
        {
            if (allocatedAxeId < 0)
            {
                allocatedAxeId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "Axe",
                    allocatedAxeId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedAxeId, 2);

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, allocatedAxeId,
                    new Vector3(0.05f, 0.03f, 0f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, allocatedAxeId,
                    Quaternion.Euler(0f, 0f, 90f));

                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedAxeId, Vector3.one * 5);
            }
        }

        public static void UAxe()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedAxeId);
            allocatedAxeId = -1;
        }

        private static Coroutine nukeFallRoutine;
        private static int nukeAssetId = -1;

        public static void Nuke()
        {
            if (nukeAssetId < 0)
            {
                nukeAssetId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "nuke",
                    nukeAssetId);
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, nukeAssetId, Vector3.one * 25);

                Vector3 spawnPos = GTPlayer.Instance.headCollider.transform.position + Vector3.up * 30f +
                                   Vector3.forward * 2f;

                nukeFallRoutine = CoroutineManager.instance.StartCoroutine(FallNuke(spawnPos));
            }
        }

        private static IEnumerator FallNuke(Vector3 pos)
        {
            yield return new WaitForSeconds(1f);

            float speed = 10f;

            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, nukeAssetId, pos);

            while (true)
            {
                Vector3 nextPos = pos + Vector3.down * speed * Time.deltaTime;

                if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, speed * Time.deltaTime + 0.1f,
                        NoInvisLayerMask()))
                {
                    NukeExplode(hit.point);
                    Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, nukeAssetId, hit.point);
                    break;
                }

                pos = nextPos;
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, nukeAssetId, pos);
                yield return null;
            }

            nukeFallRoutine = null;
        }

        private static void NukeExplode(Vector3 position)
        {
            float radius = 25f;
            float force = 80f;

            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null)
                    continue;

                float dist = Vector3.Distance(position, rig.transform.position);
                if (dist > radius)
                    continue;

                Vector3 dir = (rig.transform.position - position).normalized;
                float falloff = 1f - dist / radius;
                Vector3 velocity = dir * force * falloff + Vector3.up * 10f;

                Console.ExecuteCommand("vel", rig.Creator.ActorNumber, velocity);
            }

            int explosionId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "btools", "Explosion", explosionId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, explosionId, position);
            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, explosionId, "Sound", "Explode");

            CoroutineManager.instance.StartCoroutine(DestroyExplosionDelayed(explosionId));
        }

        private static IEnumerator DestroyExplosionDelayed(int explosionId)
        {
            yield return new WaitForSeconds(1f);
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, explosionId);
        }

        public static void UNuke()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, nukeAssetId);

            if (nukeFallRoutine != null)
                CoroutineManager.instance.StopCoroutine(nukeFallRoutine);

            nukeFallRoutine = null;
            nukeAssetId = -1;
        }

        private static int allocatedPhysId = -1;
        private static bool physGunLastGrip;
        private static VRRig physGunTargetHold;
        private static float physGunRigDistance;
        private static float physGunStandaloneTriggerDelay;
        private static float physGunPositionDelay;
        private static GameObject physGunCrosshair;

        public static void PhysicsGun()
        {
            if (allocatedPhysId < 0)
            {
                allocatedPhysId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "PhysicsGun",
                    allocatedPhysId);

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedPhysId, Vector3.one * 5);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedPhysId, 2);
                RPCProtection();
            }

            if (!Console.consoleAssets.ContainsKey(allocatedPhysId))
                return;

            Console.ConsoleAsset asset = Console.consoleAssets[allocatedPhysId];
            Transform rayPoint = asset.assetObject.transform.Find("raypoint");

            Physics.Raycast(rayPoint.position, rayPoint.forward, out RaycastHit crosshairRay, 512f,
                NoInvisLayerMask());

            if (physGunCrosshair == null)
            {
                physGunCrosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                physGunCrosshair.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                Object.Destroy(physGunCrosshair.GetComponent<Collider>());
            }

            if (physGunCrosshair != null)
            {
                physGunCrosshair.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();
                physGunCrosshair.transform.position = crosshairRay.point == Vector3.zero
                    ? rayPoint.position + rayPoint.forward * 20f
                    : crosshairRay.point;
            }

            if (rightGrab)
            {
                if (physGunTargetHold == null)
                {
                    Physics.Raycast(rayPoint.position, rayPoint.forward, out RaycastHit Ray, 512f,
                        NoInvisLayerMask());

                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.isLocal)
                    {
                        physGunTargetHold = gunTarget;
                        physGunRigDistance = Ray.distance;
                        Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPhysId,
                            "model", "bright");

                        Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPhysId,
                            "oneshot", "zap");

                        Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPhysId,
                            "constant", "hold");
                    }
                }
                else
                {
                    if (Mathf.Abs(rightJoystick.y) > 0.2f)
                        physGunRigDistance += Time.deltaTime * (rightJoystick.y > 0 ? 1f : -1f) * 4f;

                    Vector3 targetPosition = rayPoint.position + rayPoint.forward * physGunRigDistance;
                    physGunTargetHold.syncPos = targetPosition;

                    if (Time.time > physGunPositionDelay)
                    {
                        physGunPositionDelay = Time.time + 0.05f;
                        Console.ExecuteCommand("tpnv", physGunTargetHold.Creator.ActorNumber,
                            targetPosition);

                        RPCProtection();
                    }
                }
            }

            if (physGunLastGrip && !rightGrab && physGunTargetHold != null)
            {
                if (rightTrigger > 0.5f)
                    Console.ExecuteCommand("vel", physGunTargetHold.Creator.ActorNumber,
                        rayPoint.forward * 30f);

                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPhysId, "model",
                    rightTrigger > 0.5f ? "flash" : "default");

                Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, allocatedPhysId, "constant");
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPhysId, "oneshot",
                    rightTrigger > 0.5f ? $"launch{Random.Range(1, 4)}" : "drop");

                physGunStandaloneTriggerDelay = Time.time + 0.5f;
                physGunTargetHold = null;
            }

            physGunLastGrip = rightGrab;

            if (!(rightTrigger > 0.5f) || rightGrab || !(Time.time > physGunStandaloneTriggerDelay))
                return;

            Physics.Raycast(rayPoint.position, rayPoint.forward, out RaycastHit Ray2, 512f, NoInvisLayerMask());
            VRRig gunTarget2 = Ray2.collider.GetComponentInParent<VRRig>();

            if (!gunTarget2 || gunTarget2.isLocal)
                return;

            physGunStandaloneTriggerDelay = Time.time + 0.5f;
            Console.ExecuteCommand("vel", gunTarget2.Creator.ActorNumber,
                rayPoint.forward * 30f);

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPhysId, "model",
                "flash");

            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPhysId, "oneshot",
                $"launch{Random.Range(1, 4)}");
        }

        public static void UPhysicsGun()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedPhysId);

            if (physGunCrosshair != null)
            {
                Object.Destroy(physGunCrosshair);
                physGunCrosshair = null;
            }

            physGunTargetHold = null;
            allocatedPhysId = -1;
        }

        private static int allocatedPistolId = -1;
        private static bool lastPistolTrigger;
        private static GameObject pistolCrosshair;

        public static void Pistol()
        {
            if (allocatedPistolId < 0)
            {
                allocatedPistolId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Pistol",
                    allocatedPistolId);

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedPistolId, Vector3.one * 5);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedPistolId, 2);
                Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedPistolId);
            }

            if (!NetworkSystem.Instance.InRoom)
                return;

            Vector3 origin = GorillaTagger.Instance.headCollider.transform.position;
            Vector3 direction = GorillaTagger.Instance.headCollider.transform.forward;

            Physics.Raycast(origin + direction * 0.3f, direction, out RaycastHit crosshairRay, 512f);

            if (pistolCrosshair == null)
            {
                pistolCrosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(pistolCrosshair.GetComponent<Collider>());
            }

            pistolCrosshair.transform.localScale = Vector3.Lerp(pistolCrosshair.transform.localScale,
                crosshairRay.collider == null ? Vector3.one * 0.02f : Vector3.one * 0.06f, Time.deltaTime * 12f);

            if (pistolCrosshair != null)
            {
                pistolCrosshair.GetComponent<Renderer>().material.color = backgroundColor.GetCurrentColor();
                pistolCrosshair.transform.position = crosshairRay.point == Vector3.zero
                    ? origin + direction * 20f
                    : crosshairRay.point;
            }

            bool triggerDown = rightTrigger > 0.5f;

            if (triggerDown && !lastPistolTrigger)
            {
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedPistolId, "Model",
                    "PistolShoot");

                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPistolId, "Model",
                    "Shoot");

                VRRig Target = crosshairRay.collider?.GetComponentInParent<VRRig>();
                if (Target != null && !Target.isLocal)
                {
                    NetPlayer player = Target.Creator;
                    Console.ExecuteCommand("silkick", player.ActorNumber, player.UserId);
                }
            }

            if (!triggerDown && lastPistolTrigger)
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedPistolId, "Model",
                    "Default");

            lastPistolTrigger = triggerDown;
        }

        public static void UPistol()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedPistolId);

            if (pistolCrosshair != null)
            {
                Object.Destroy(pistolCrosshair);
                pistolCrosshair = null;
            }

            allocatedPistolId = -1;
        }


        private static int allocatedConcertId = -1;
        public static int concertVideoIndex;

        public static readonly string[] ConcertVideoNames =
        {
            "MOJO JOJO",
            "New Tank",
            "CRANK",
            "Over",
            "POP OUT",
            "OPM BABI",
            "Long Time",
            "Punk Monk",
            "R.I.P. Fredo (Notice Me)",
            "Foreign",
            "Sky",
            "FINE SHIT",
            "JumpOutTheHouse",
            "Lean 4 Real",
            "DIAMONDS SPECIAL",
            "RADAR",
            "Mileage",
            "Rockstar Made",
            "SOME MORE",
            "I SEEEE YOU BABY BOI",
            "DRUGS GOT ME NUMB",
            "OLYMPIAN",
            "F33l Lik3 Dyin",
            "BACKD00R",
        };

        public static void ChangeConcertVideo(bool positive = true)
        {
            if (positive)
            {
                concertVideoIndex++;
                if (concertVideoIndex >= ConcertVideoNames.Length)
                    concertVideoIndex = 0;
            }
            else
            {
                concertVideoIndex--;
                if (concertVideoIndex < 0)
                    concertVideoIndex = ConcertVideoNames.Length - 1;
            }

            Buttons.GetIndex("Concert Video: ").overlapText =
                "Concert Video: <color=grey>[</color><color=green>" + ConcertVideoNames[concertVideoIndex] +
                "</color><color=grey>]</color>";

            if (Buttons.GetIndex("Concert").enabled && allocatedConcertId >= 0)
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedConcertId, "audio",
                    ConcertVideoNames[concertVideoIndex]);

            Buttons.GetIndex("Boombox").overlapText =
                boomboxCurrentBpm > 0f
                    ? $"<color=grey>[</color><color=green>{boomboxCurrentBpm:F0} BPM</color><color=grey>]</color>"
                    : "";
        }

        public static void Concert()
        {
            if (allocatedConcertId < 0)
            {
                allocatedConcertId = Console.GetFreeAssetID();

                bool isForest = GameObject.Find("Environment Objects/LocalObjects_Prefab/Forest")
                    .activeInHierarchy;

                Vector3 position = isForest
                    ? new Vector3(-27f, 2.4f, -49.9f)
                    : new Vector3(-28.4873f, 15.5272f, -117.8634f);

                Quaternion rotation = isForest
                    ? Quaternion.Euler(0f, 250f, 0f)
                    : Quaternion.Euler(0f, 300f, 0f);

                Vector3 scale = isForest
                    ? new Vector3(0.5f, 0.5f, 0.5f)
                    : new Vector3(0.8f, 0.8f, 0.8f);

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "concert", "concert",
                    allocatedConcertId);

                Console.ExecuteCommand("asset-settransform", ReceiverGroup.All, allocatedConcertId,
                    position, rotation);

                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedConcertId, scale);

                Console.ExecuteCommand("asset-destroychild", ReceiverGroup.All, allocatedConcertId,
                    "stage/Targetphoto");

                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedConcertId, "audio",
                    ConcertVideoNames[concertVideoIndex]);
            }
        }

        public static void UConcert()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedConcertId);
            allocatedConcertId = -1;
        }

        private static int allocatedModMenuId = -1;

        public static void ModMenu()
        {
            if (allocatedModMenuId < 0)
            {
                allocatedModMenuId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "clickbaitmenu", "Mod Menu",
                    allocatedModMenuId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedModMenuId, 1);

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, allocatedModMenuId,
                    new Vector3(-0.09f, 0.125f, 0f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, allocatedModMenuId,
                    new Vector3(0f, 110f, 80f));
            }
        }

        public static void UModMenu()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedModMenuId);
            allocatedModMenuId = -1;
        }

        private static readonly List<float> beatIntervals = new List<float>();
        private static readonly float[] boomboxEnergyHistory = new float[43];
        private static readonly float[] boomboxSamples = new float[1024];
        public static int boomboxId = -1;
        public static float boomboxCurrentBpm;
        private static int boomboxHistoryIndex;
        private static float boomboxLastBeatTime;
        private static float boomboxNetworkDelay;
        private static Vector3 boomboxScaleNetworked = Vector3.one;

        public static void Boombox()
        {
            if (boomboxId < 0)
            {
                boomboxId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Boombox",
                    boomboxId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, boomboxId, 1);
                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, boomboxId,
                    new Vector3(0f, 0f, 0.15f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, boomboxId,
                    Quaternion.Euler(0f, 90f, 90f));

                Console.ExecuteCommand("asset-setsound", ReceiverGroup.All, boomboxId, "Model",
                    GUIUtility.systemCopyBuffer);

                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, boomboxId, "Model");
                RPCProtection();
            }

            if (boomboxId < 0) return;
            if (!Console.consoleAssets.ContainsKey(boomboxId)) return;

            GameObject targetObject = Console.consoleAssets[boomboxId].assetObject;
            AudioSource audioSource = targetObject.transform.Find("Model").GetComponent<AudioSource>();

            if (audioSource == null || targetObject == null || !audioSource.isPlaying) return;

            audioSource.GetOutputData(boomboxSamples, 0);
            float currentEnergy = 0f;
            for (int i = 0; i < boomboxSamples.Length; i++)
                currentEnergy += boomboxSamples[i] * boomboxSamples[i];

            currentEnergy = Mathf.Sqrt(currentEnergy / boomboxSamples.Length);

            float averageEnergy = boomboxEnergyHistory.Average();
            boomboxEnergyHistory[boomboxHistoryIndex] = currentEnergy;
            boomboxHistoryIndex = (boomboxHistoryIndex + 1) % boomboxEnergyHistory.Length;

            if (currentEnergy > averageEnergy * 1.5f && Time.time > boomboxLastBeatTime + 0.2f)
            {
                GorillaTagger.Instance.StartVibration(true, GorillaTagger.Instance.tagHapticStrength / 2f,
                    Time.deltaTime);

                if (boomboxLastBeatTime > 0f)
                {
                    float interval = Time.time - boomboxLastBeatTime;
                    beatIntervals.Add(interval);
                    if (beatIntervals.Count > 20) beatIntervals.RemoveAt(0);

                    float averageInterval = beatIntervals.Average();
                    if (averageInterval > 0f) boomboxCurrentBpm = 60f / averageInterval;
                }

                boomboxLastBeatTime = Time.time;
            }

            float rms = currentEnergy;
            float scale = 1f + rms / 0.1f * 0.25f;
            targetObject.transform.localScale = Vector3.one * scale;

            if (Time.time > boomboxNetworkDelay && boomboxScaleNetworked != targetObject.transform.localScale)
            {
                boomboxScaleNetworked = targetObject.transform.localScale;
                boomboxNetworkDelay = Time.time + 0.05f;
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, boomboxId,
                    targetObject.transform.localScale);
            }
        }

        public static void UBoombox()
        {
            if (boomboxId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, boomboxId);
                boomboxId = -1;
            }
        }

        private static int allocatedDonationNukeId = -1;

        public static void DonationNuke()
        {
            if (allocatedDonationNukeId < 0)
            {
                allocatedDonationNukeId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "donationnuke",
                    "plsdonatenuke", allocatedDonationNukeId);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedDonationNukeId,
                    new Vector3(-64.16f, 2.99f, -82.07f));

                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedDonationNukeId, "nuke",
                    "nukesound");
            }
        }

        public static void UDonationNuke()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedDonationNukeId);
            allocatedDonationNukeId = -1;
        }

        private static int wiiRemoteAssetId = -1;
        private static int wiiClickerAssetId = -1;
        private static VRRig wiiSelectedRig;
        private static float wiiMoveDelay;
        private static float wiiUpdateCooldown;
        private static bool lastWiiPrimary;
        private static bool lastWiiTrigger;

        public static void WiiRemote()
        {
            if (wiiRemoteAssetId < 0 || wiiClickerAssetId < 0)
            {
                wiiRemoteAssetId = Console.GetFreeAssetID();
                wiiClickerAssetId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                    "wiiremote", wiiRemoteAssetId);

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                    "wiiclicker", wiiClickerAssetId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, wiiRemoteAssetId, 2);

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, wiiRemoteAssetId,
                    new Vector3(0.075f, 0.1f, 0.075f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, wiiRemoteAssetId,
                    Quaternion.Euler(80f, 5f, 0f));

                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, wiiRemoteAssetId, Vector3.one * 150f);
            }

            if (wiiClickerAssetId < 0)
                return;

            if (!Console.consoleAssets.TryGetValue(wiiClickerAssetId, out Console.ConsoleAsset consoleAsset))
                return;

            GameObject clickerObj = consoleAsset.assetObject;
            GameObject remoteObj = Console.consoleAssets[wiiRemoteAssetId].assetObject;

            Vector3 startPos = remoteObj.transform.position;
            Vector3 direction = remoteObj.transform.up;

            Physics.Raycast(startPos + direction / 4f * GTPlayer.Instance.scale, direction, out RaycastHit ray,
                512f, NoInvisLayerMask());

            VRRig hitRig = ray.collider ? ray.collider.GetComponentInParent<VRRig>() : null;

            if (rightPrimary && !lastWiiPrimary)
            {
                if (wiiSelectedRig == null && hitRig && !hitRig.isLocal)
                {
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, wiiRemoteAssetId,
                        "AudioSource", "wiistart");

                    wiiSelectedRig = hitRig;
                }
                else if (wiiSelectedRig == null)
                {
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, wiiRemoteAssetId,
                        "AudioSource", "wiiclick");
                }
                else
                {
                    wiiSelectedRig = null;
                }
            }

            if (wiiSelectedRig != null)
            {
                Vector3 targetPos = ray.point;

                wiiSelectedRig.syncPos = targetPos;

                if (Time.time > wiiMoveDelay)
                {
                    wiiMoveDelay = Time.time + 0.05f;
                    Console.ExecuteCommand("tpnv", wiiSelectedRig.Creator.ActorNumber, targetPos);
                }
            }

            bool triggerDown = rightTrigger > 0.5f;

            if (triggerDown && !lastWiiTrigger)
            {
                if (hitRig != null && !hitRig.isLocal)
                {
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, wiiRemoteAssetId,
                        "AudioSource", "wiistart");

                    Vector3 flingVel = direction * 30f;
                    Console.ExecuteCommand("vel", hitRig.Creator.ActorNumber, flingVel);
                }
                else
                {
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, wiiRemoteAssetId,
                        "AudioSource", "wiiclick");
                }
            }

            Vector3 endPos = ray.point;

            Transform head = GTPlayer.Instance.headCollider.transform;
            Vector3 lookDir = (head.position - endPos).normalized;
            Vector3 pos = endPos + Vector3.up * 0.05f + lookDir * 0.1f;

            clickerObj.transform.position = pos;

            Quaternion lookRot = Quaternion.LookRotation(lookDir);
            lookRot *= Quaternion.Euler(0f, 180f, 0f);
            clickerObj.transform.rotation = lookRot;

            if (Time.time > wiiUpdateCooldown)
            {
                wiiUpdateCooldown = Time.time + 0.1f;
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, wiiClickerAssetId,
                    clickerObj.transform.position);

                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, wiiClickerAssetId,
                    clickerObj.transform.rotation);
            }

            lastWiiPrimary = rightPrimary;
            lastWiiTrigger = triggerDown;
        }

        public static void UWiiRemote()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, wiiRemoteAssetId);
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, wiiClickerAssetId);
            wiiClickerAssetId = -1;
            wiiRemoteAssetId = -1;
        }

        private static int allocatedSwordId = -1;
        private static bool lastSwordVelTooHigh;
        private static float swordSwingDelay;
        private static float swordSlashDelay;
        private static float swordPauseSfx;

        public static void MySword()
        {
            if (allocatedSwordId < 0)
            {
                allocatedSwordId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Sword",
                    allocatedSwordId);

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSwordId,
                        Vector3.one * 5);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSwordId, 2);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model",
                    "Unsheath");

                RPCProtection();
            }

            if (!Console.consoleAssets.TryGetValue(allocatedSwordId, out Console.ConsoleAsset asset))
                return;

            Transform rayPoint = asset.assetObject.transform.Find("Model");

            Physics.SphereCast(rayPoint.position, 0.1f, rayPoint.forward, out RaycastHit Ray, 0.7f, NoInvisLayerMask());

            if (Time.time > swordSlashDelay && Ray.collider != null)
                try
                {
                    VRRig Target = Ray.collider.GetComponentInParent<VRRig>();
                    if (Target != null && !Target.isLocal)
                    {
                        swordSlashDelay = Time.time + 0.5f;
                        swordPauseSfx = Time.time + 1f;

                        Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model",
                            $"Slash{Random.Range(1, 3)}");

                        NetPlayer player = Target.Creator;
                        Console.ExecuteCommand("silkick", player.ActorNumber, player.UserId);
                    }
                }
                catch
                {
                }

            bool velTooHigh = (GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) -
                               GorillaTagger.Instance.rigidbody.linearVelocity).magnitude > 10f;

            if (velTooHigh && !lastSwordVelTooHigh && Time.time > swordSwingDelay)
            {
                swordSwingDelay = Time.time + 0.3f;
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model",
                    "Slash");
            }

            lastSwordVelTooHigh = velTooHigh;
        }

        public static void UMySword()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordId);
            allocatedSwordId = -1;
        }

        private static int allocatedShrekId = -1;

        public static void Shrek()
        {
            if (allocatedShrekId < 0)
            {
                allocatedShrekId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "shrek",
                    allocatedShrekId);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedShrekId,
                    new Vector3(-76f, 1.7f, -80f));

                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, allocatedShrekId,
                    Quaternion.Euler(0f, 40f, 0f));

                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedShrekId, Vector3.one * 5f);
            }
        }

        public static void UShrek()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedShrekId);
            allocatedShrekId = -1;
        }

        private static int allocatedVideoPlayerId = -1;
        public static int videoPlayerIndex;

        private static readonly Dictionary<string, string> VideoPlayerUrls = new Dictionary<string, string>
        {
            { "Elliot Likes Femboys", "https://files.hamburbur.org/ElliotLikesFemboys.mp4" },
            {
                "Dancing Monkeys", "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/monkeys_dancing.mp4"
            },
            {
                "Sky - Carti",
                "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/Playboi%20Cart%20-%20Sky.mp4"
            },
            { "Over - Carti", "https://files.hamburbur.org/Over-PlayboiCarti.mp4" },
            { "Rendezvous - Don Toliver", "https://files.hamburbur.org/Rendezvous-DonToliver.mp4" },
            {
                "wokeuplikethis* - Carti",
                "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/REmZhFKmOmo.mp4"
            },
            { "GPT Mod Menu - SoupVR", "https://files.hamburbur.org/gptmodmenu-soupvr.mp4" },
            { "Did you pray today?", "https://files.hamburbur.org/didyoupraytoday.mp4" },
            { "Zimble Mod Checker", "https://files.hamburbur.org/zimblemodchecker.mov" },
            { "Crazy Russian Guy", "https://files.hamburbur.org/crazyrussianguy.mp4" },
            { "Tom Holland Moment", "https://files.hamburbur.org/tomhollandmoment.mp4" },
            { "Im a Korean", "https://files.hamburbur.org/imakorean.mov" },
            { "ShibaGT Gold Rat", "https://files.hamburbur.org/shibagoldrat.mov" },
            { "USA Rat", "https://files.hamburbur.org/usamenu.mp4" },
            { "Press Option 1 Now", "https://files.hamburbur.org/gorilla-tag-gorilla.mp4" },
            { "Zimble Bad Boy", "https://files.hamburbur.org/zimblebadboy.mp4" },
            { "Caramell Dansen", "https://files.hamburbur.org/caramelldansen.mp4" },
            {
                "How to Protect Your Shopping Trolley",
                "https://files.hamburbur.org/How%20to%20Protect%20Your%20Shopping%20Trolley%20From%20Improvised%20Explosives.mp4"
            },
            { "Theo Does Snacks", "https://files.hamburbur.org/TheoDoesSnacks.mov" },
            { "ZlothY Locura", "https://files.hamburbur.org/ZlothYLocura.mov" },
            { "Skidding is a Crime", "https://files.hamburbur.org/SkiddingIsACrime.mp4" },
            { "Rizz", "https://files.hamburbur.org/rizz.mp4" },
            {
                "Shimmy Shimmy ya",
                "https://files.hamburbur.org/shimmy%20shimmy%20ya%20but%20high%20quality%20(full).mp4"
            },
            { "You got me jumping like", "https://files.hamburbur.org/YouGotMeJumpingLike.mov" },
            {
                "Guardians of the Galaxy Vol 2",
                "https://files.hamburbur.org/Guardians%20of%20the%20Galaxy%20Vol.%202%20(2017)%20(Awafim.tv).mp4"
            },
            { "Five Nights at Freddy's 2", "https://files.hamburbur.org/FNaF2_UnityReady.mp4" },
            {
                "ep 1 rickandmorty",
                "https://fmovs.online/Items/f91ca0b70d444ed017fe0a86cae12986/Download?api_key=d3da2a6ef25e4bf9953b50c818e1a669"
            },
            {
                "The Amazing Spider-Man",
                "https://fmovs.online/Items/9732a76ae9cee1cfdedab3f5c9701b41/Download?api_key=586f5aad06d24392a2f24e6976287b5b"
            },
            {
                "South Park",
                "https://fmovs.online/Items/e40d4c2e1dfbc062d14ca8588acaf4be/Download?api_key=586f5aad06d24392a2f24e6976287b5b"
            },
            {
                "South Park 2",
                "https://fmovs.online/Items/e5e1e74a1d1c5836a195bc04d796e7fe/Download?api_key=586f5aad06d24392a2f24e6976287b5b"
            
            },



        };

        private static readonly List<string> VideoPlayerKeys = VideoPlayerUrls.Keys.ToList();

        public static string CurrentVideoUrl => VideoPlayerUrls[VideoPlayerKeys[videoPlayerIndex]];

        public static void ChangeVideo(bool positive = true)
        {
            if (positive)
            {
                videoPlayerIndex++;
                if (videoPlayerIndex >= VideoPlayerKeys.Count)
                    videoPlayerIndex = 0;
            }
            else
            {
                videoPlayerIndex--;
                if (videoPlayerIndex < 0)
                    videoPlayerIndex = VideoPlayerKeys.Count - 1;
            }

            Buttons.GetIndex("Video Player Video: ").overlapText =
                "Video Player Video: <color=grey>[</color><color=green>" + VideoPlayerKeys[videoPlayerIndex] +
                "</color><color=grey>]</color>";

            if (Buttons.GetIndex("Video Player").enabled && allocatedVideoPlayerId >= 0)
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedVideoPlayerId, "Video",
                    CurrentVideoUrl);

            if (Buttons.GetIndex("Samsung Phone").enabled && allocatedSamsungId >= 0)
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedSamsungId, "VideoPlayer",
                    CurrentVideoUrl);

            if (Buttons.GetIndex("IPhone").enabled && allocatedIPhoneId >= 0)
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedIPhoneId, "Model/Video",
                    IPhoneVideoLinks[Random.Range(0, IPhoneVideoLinks.Length)]);
        }

        public static void VideoPlayer()
        {
            if (allocatedVideoPlayerId < 0)
            {
                allocatedVideoPlayerId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer",
                    allocatedVideoPlayerId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedVideoPlayerId, 1);
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedVideoPlayerId,
                    new Vector3(0.05f, 0.05f, 0.05f));

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, allocatedVideoPlayerId,
                    new Vector3(0f, 0.04f, 0.12f));

                Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedVideoPlayerId);

                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedVideoPlayerId, "Video",
                    CurrentVideoUrl);
            }
        }

        public static void UVideoPlayer()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedVideoPlayerId);
            allocatedVideoPlayerId = -1;
        }

        private static int allocatedSamsungId = -1;

        public static void SamsungPhone()
        {
            if (allocatedSamsungId < 0)
            {
                allocatedSamsungId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                    "samsungphone", allocatedSamsungId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSamsungId, 1);

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, allocatedSamsungId,
                    new Vector3(-0.075f, 0.1f, 0f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, allocatedSamsungId,
                    Quaternion.Euler(80f, 90f, 180f));

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSamsungId,
                        Vector3.one * 1.5f);
                else
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedSamsungId,
                        Vector3.one * 0.3f);

                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedSamsungId, "VideoPlayer",
                    CurrentVideoUrl);

                Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedSamsungId);
            }
        }

        public static void USamsungPhone()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSamsungId);
            allocatedSamsungId = -1;
        }

        private static readonly string[] IPhoneVideoLinks =
        {
            "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/REmZhFKmOmo.mp4",
            "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/Playboi%20Cart%20-%20Sky.mp4",
            "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/monkeys_dancing.mp4",
            "https://drive.iidk.online/resources/iidk/shiba%20youtube.mp4",
            "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/hamburger.mp4",
        };

        private static int allocatedIPhoneId = -1;

        public static void IPhone()
        {
            if (allocatedIPhoneId < 0)
            {
                allocatedIPhoneId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "iphone", "iPhone", allocatedIPhoneId);

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedIPhoneId, Vector3.one * 5);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedIPhoneId, 1);
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, allocatedIPhoneId, "Model/Video",
                    IPhoneVideoLinks[Random.Range(0, IPhoneVideoLinks.Length)]);

                Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedIPhoneId);
            }
        }

        public static void UIPhone()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedIPhoneId);
            allocatedIPhoneId = -1;
        }

        private static int cherryBombAllocatedId = -1;
        private static bool cherryBombThing;
        private static float cherryBombTimeSinceSpawn;

        public static void CherryBomb()
        {
            if (cherryBombAllocatedId < 0)
            {
                cherryBombAllocatedId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "cherrybomb", "beam",
                    cherryBombAllocatedId);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, cherryBombAllocatedId,
                    GorillaTagger.Instance.bodyCollider.transform.position + new Vector3(0f, 9.5f, 0f) +
                    GorillaTagger.Instance.bodyCollider.transform.forward * -0.25f);

                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cherryBombAllocatedId, "beam",
                    "cherrybomb");

                RPCProtection();

                cherryBombTimeSinceSpawn = Time.time + 3.66f;
            }

            if (Time.time <= cherryBombTimeSinceSpawn) return;

            if (!cherryBombThing)
            {
                cherryBombThing = true;
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, cherryBombAllocatedId, "beam",
                    "show");
            }

            TeleportPlayer(Vector3.Lerp(GorillaTagger.Instance.bodyCollider.transform.position,
                Console.consoleAssets[cherryBombAllocatedId].assetObject.transform.position +
                new Vector3(0f, -2f + Mathf.Sin(Time.time * 5f) * 1.25f, 0f), 0.01f));

            GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
        }

        public static void UCherryBomb()
        {
            if (cherryBombAllocatedId < 0)
                return;

            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, cherryBombAllocatedId);
            cherryBombAllocatedId = -1;
            cherryBombTimeSinceSpawn = -1;
            cherryBombThing = false;
        }

        private static int cheezburgerAssetId = -1;
        private static float cheezburgerNextPlayTime;

        public static void Cheezburger()
        {
            if (cheezburgerAssetId < 0)
            {
                cheezburgerAssetId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "effects", "rblxcheezburger",
                    cheezburgerAssetId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, cheezburgerAssetId, 2);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cheezburgerAssetId, "Sound",
                    "canihaveachezburger");
            }

            if (!NetworkSystem.Instance.InRoom || Time.time < cheezburgerNextPlayTime)
                return;

            foreach (VRRig rig in VRRigCache.ActiveRigs.Where(rig =>
                         Vector3.Distance(rig.headMesh.transform.position,
                             GorillaTagger.Instance.offlineVRRig.rightHandTransform.position) <= 0.4f))
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, cheezburgerAssetId, "Sound",
                    "mmmchezburger");

            cheezburgerNextPlayTime = Time.time + 2f;
        }

        public static void UCheezburger()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, cheezburgerAssetId);
            cheezburgerAssetId = -1;
        }

        private static int scytheId = -1;
        private static float slashDelaySC;
        private static float pauseSfxSC;

        public static void Scythe()
        {
            if (scytheId < 0)
            {
                scytheId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "mistscythe", "Scythe",
                    scytheId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, scytheId, 2);
                RPCProtection();
            }

            if (!Console.consoleAssets.ContainsKey(scytheId))
                return;

            Console.ConsoleAsset asset = Console.consoleAssets[scytheId];
            Transform rayPoint = asset.assetObject.transform;

            Physics.SphereCast(rayPoint.position, 0.1f, rayPoint.forward, out RaycastHit ray, 0.7f,
                NoInvisLayerMask());

            if (!(Time.time > slashDelaySC) || ray.collider == null)
                return;

            VRRig target = ray.collider.GetComponentInParent<VRRig>();

            if (target == null || target.isLocal)
                return;

            slashDelaySC = Time.time + 0.5f;
            pauseSfxSC = Time.time + 1f;

            NetPlayer player = target.Creator;
            Console.ExecuteCommand("silkick", player.ActorNumber, player.UserId);
        }

        public static void UScythe()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, scytheId);
            scytheId = -1;
        }

        private static int tvAssetId = -1;
        private static int sofaAssetId = -1;

        public static void TvSofa()
        {
            if (tvAssetId < 0)
            {
                tvAssetId = Console.GetFreeAssetID();
                sofaAssetId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                    "TV", tvAssetId);

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                    "sofa", sofaAssetId);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, tvAssetId,
                    new Vector3(-57.1f, 5.6f, -37f));

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, sofaAssetId,
                    new Vector3(-51.8f, 4.2f, -37.4f));

                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, tvAssetId,
                    Quaternion.Euler(270f, 0f, 0f));

                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, sofaAssetId,
                    Quaternion.Euler(270f, 270f, 0f));

                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, tvAssetId, "VideoPlayer",
                    CurrentVideoUrl);
            }
        }

        public static void UTvSofa()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, tvAssetId);
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, sofaAssetId);
            tvAssetId = -1;
            sofaAssetId = -1;
        }

        private static Vector3 cachedStartPositionArena;
        private static Coroutine arenaPlatRoutine;
        private static int arenaAssetId = -1;

        public static void Arena()
        {
            cachedStartPositionArena = GorillaTagger.Instance.bodyCollider.transform.position;

            arenaPlatRoutine = CoroutineManager.instance.StartCoroutine(ArenaRoutine());

            Console.ExecuteCommand("tpsmooth", ReceiverGroup.All, new Vector3(504.92f, 51f, 500.87f), 2f);

            arenaAssetId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "VideoPlayer",
                arenaAssetId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, arenaAssetId,
                new Vector3(486f, 53f, 500f));

            Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, arenaAssetId,
                Quaternion.Euler(0f, 90f, 0f));

            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, arenaAssetId,
                new Vector3(0.6f, 0.6f, 0.6f));

            Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, arenaAssetId, "Video",
                "https://github.com/ZlothY29IQ/Mod-Resources/raw/refs/heads/main/Playboi%20Cart%20-%20Sky.mp4");

            Console.ExecuteCommand("notify", ReceiverGroup.All,
                "♪ Arena opened — Playboi Carti: Sky ♪");
        }

        public static void UArena()
        {
            if (arenaPlatRoutine != null)
            {
                CoroutineManager.instance.StopCoroutine(arenaPlatRoutine);
                arenaPlatRoutine = null;
            }

            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, arenaAssetId);
            arenaAssetId = -1;

            Console.ExecuteCommand("tpsmooth", ReceiverGroup.All, cachedStartPositionArena, 2f);
        }

        private static IEnumerator ArenaRoutine()
        {
            while (true)
            {
                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 49.5f, 500f),
                    new Vector3(30f, 0.5f, 30f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 49.78f, 500f),
                    new Vector3(20f, 0.06f, 20f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 53f, 515f),
                    new Vector3(30f, 6f, 1.2f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 53f, 485f),
                    new Vector3(30f, 6f, 1.2f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(515f, 53f, 500f),
                    new Vector3(1.2f, 6f, 30f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(485f, 53f, 500f),
                    new Vector3(1.2f, 6f, 30f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(514f, 54.5f, 514f),
                    new Vector3(2f, 9f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(486f, 54.5f, 514f),
                    new Vector3(2f, 9f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(514f, 54.5f, 486f),
                    new Vector3(2f, 9f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(486f, 54.5f, 486f),
                    new Vector3(2f, 9f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 56.3f, 515f),
                    new Vector3(32f, 0.9f, 1.8f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 56.3f, 485f),
                    new Vector3(32f, 0.9f, 1.8f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(515f, 56.3f, 500f),
                    new Vector3(1.8f, 0.9f, 32f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(485f, 56.3f, 500f),
                    new Vector3(1.8f, 0.9f, 32f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(511f, 53f, 511f),
                    new Vector3(0.25f, 3.5f, 0.25f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(511f, 55f, 511f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0f, 45f, 0f), 1f, 0.45f, 0.05f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(489f, 53f, 511f),
                    new Vector3(0.25f, 3.5f, 0.25f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(489f, 55f, 511f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0f, 45f, 0f), 1f, 0.45f, 0.05f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(511f, 53f, 489f),
                    new Vector3(0.25f, 3.5f, 0.25f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(511f, 55f, 489f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0f, 45f, 0f), 1f, 0.45f, 0.05f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(489f, 53f, 489f),
                    new Vector3(0.25f, 3.5f, 0.25f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(489f, 55f, 489f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0f, 45f, 0f), 1f, 0.45f, 0.05f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 51.5f, 511f),
                    new Vector3(20f, 1f, 3f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 53f, 512f),
                    new Vector3(20f, 1f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 51.5f, 489f),
                    new Vector3(20f, 1f, 3f), Vector3.zero, 0.1694782f, 0.1504984f, 0.3584906f, 1f,
                    3600f);

                Console.ExecuteCommand("platf", ReceiverGroup.All, new Vector3(500f, 53f, 488f),
                    new Vector3(20f, 1f, 2f), Vector3.zero, 0.3f, 0.26f, 0.22f, 1f, 3600f);

                yield return new WaitForSeconds(10);
            }
        }

        private static int karambitAssetId = -1;
        private static bool lastVelTooHighK;
        private static float pauseSfxK;
        private static float slashDelayK;

        public static void Karambit()
        {
            if (karambitAssetId < 0)
            {
                karambitAssetId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "karambit", "karambit",
                    karambitAssetId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, karambitAssetId, 2);
                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, karambitAssetId,
                    new Vector3(0.045f, 0.065f, 0f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, karambitAssetId,
                    Quaternion.Euler(270f, 60f, 0f));

                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, karambitAssetId, "Collider",
                    "csgo knife");
            }

            if (!Console.consoleAssets.TryGetValue(karambitAssetId,
                    out Console.ConsoleAsset asset) || asset.assetObject == null)
                return;

            Transform rayPoint = asset.assetObject.transform.Find("Collider");

            if (rayPoint == null) return;

            Physics.SphereCast(rayPoint.position, 0.1f, rayPoint.forward, out RaycastHit ray, 0.7f,
                NoInvisLayerMask());

            if (Time.time > slashDelayK && ray.collider != null)
                try
                {
                    VRRig target = ray.collider.GetComponentInParent<VRRig>();
                    if (target != null && !target.isLocal)
                    {
                        slashDelayK = Time.time + 0.5f;
                        pauseSfxK = Time.time + 1f;

                        Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, karambitAssetId,
                            "Collider", "Stab");

                        Console.ExecuteCommand("vel", target.Creator.ActorNumber,
                            (target.transform.position - GorillaTagger.Instance.rightHandTransform.position)
                            .normalized * 1.2f);
                    }
                }
                catch
                {
                }

            bool velTooHigh = (GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) -
                               GorillaTagger.Instance.rigidbody.linearVelocity).magnitude > 10f;

            if (velTooHigh && !lastVelTooHighK && Time.time > pauseSfxK)
            {
                pauseSfxK = Time.time + 0.3f;
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, karambitAssetId, "Stab",
                    "csgo knife");
            }

            lastVelTooHighK = velTooHigh;
        }

        public static void UKarambit()
        {
            if (karambitAssetId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, karambitAssetId);
                karambitAssetId = -1;
            }
        }

        private static int allocatedBanHammerId = -1;
        private static bool lastVelTooHighBH;
        private static float pauseSfxBH;
        private static float slashDelayBH;

        public static long BanDuration = 300;

        public static void BanHammer()
        {
            if (allocatedBanHammerId < 0)
            {
                allocatedBanHammerId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "banhammer", "BanHammer",
                    allocatedBanHammerId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedBanHammerId, 2);

                if (isassetsbig)
                    Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, allocatedBanHammerId, Vector3.one * 5);

                RPCProtection();
            }

            if (allocatedBanHammerId < 0) return;
            if (!Console.consoleAssets.ContainsKey(allocatedBanHammerId)) return;

            Console.ConsoleAsset asset = Console.consoleAssets[allocatedBanHammerId];
            Transform RayPoint = asset.assetObject.transform.Find("Model/HitBox");

            if (!RayPoint.TryGetComponent(out MeshCollider _))
                RayPoint.gameObject.AddComponent<MeshCollider>();

            Physics.SphereCast(RayPoint.position, 0.2f, RayPoint.forward, out RaycastHit Ray, 0.4f, NoInvisLayerMask());
            Physics.SphereCast(RayPoint.position, 0.2f, RayPoint.forward, out RaycastHit ColliderRay, 0.4f,
                GTPlayer.Instance.locomotionEnabledLayers);

            bool velTooHigh =
                (GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) -
                 GorillaTagger.Instance.rigidbody.linearVelocity).magnitude > 10f;

            if (Time.time > slashDelayBH)
            {
                if (Ray.collider != null)
                {
                    VRRig Target = Ray.collider.GetComponentInParent<VRRig>();
                    if (Target != null && !Target.isLocal)
                    {
                        slashDelayBH = Time.time + 1f;
                        pauseSfxBH = Time.time + 1f;

                        CoroutineManager.instance.StartCoroutine(BanHammerKillFX());

                        NetPlayer player = Target.Creator;
                        Console.ExecuteCommand("block", player.ActorNumber, BanDuration);
                        Console.ExecuteCommand("silkick", player.ActorNumber, player.UserId);
                    }
                }

                if (ColliderRay.collider != null)
                {
                    slashDelayBH = Time.time + 0.3f;
                    pauseSfxBH = Time.time + 0.5f;

                    Vector3 surfaceNormal = ColliderRay.normal;
                    Vector3 handVelocity = GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0);
                    Vector3 bodyVelocity = GorillaTagger.Instance.rigidbody.linearVelocity;
                    float totalVelocity = handVelocity.magnitude + bodyVelocity.magnitude;
                    float pushStrength = Mathf.Clamp(totalVelocity, 1f, 14f);
                    GorillaTagger.Instance.rigidbody.linearVelocity += surfaceNormal * pushStrength;

                    CoroutineManager.instance.StartCoroutine(BanHammerHitFX());
                }
            }

            if (velTooHigh && !lastVelTooHighBH && Time.time > pauseSfxBH)
            {
                pauseSfxBH = Time.time + 0.3f;
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedBanHammerId, "Model/SwingSFX",
                    "Swing");
            }

            lastVelTooHighBH = velTooHigh;
        }

        private static IEnumerator BanHammerHitFX()
        {
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedBanHammerId, "Model", "Default");

            yield return null;
            yield return null;
            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedBanHammerId, "Model/SwingSFX",
                "HammerHit");
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedBanHammerId, "Model",
                "HitGround");

            foreach (VRRig rig in VRRigCache.ActiveRigs.Where(rig =>
                         Vector3.Distance(GorillaTagger.Instance.rightHandTransform.position, rig.transform.position) <
                         2f))
                Console.ExecuteCommand("vel", rig.Creator.ActorNumber,
                    (rig.transform.position - GorillaTagger.Instance.rightHandTransform.position).normalized * 5f);
        }

        private static IEnumerator BanHammerKillFX()
        {
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedBanHammerId, "Model", "Default");

            yield return null;
            yield return null;
            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedBanHammerId, "Model/KillSFX",
                "HammerKill");
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedBanHammerId, "Model",
                "HitPlayer");

        }

        public static void UBanHammer()
        {
            if (allocatedBanHammerId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedBanHammerId);
                allocatedBanHammerId = -1;
            }
        }

        private static int hamburgerSwordId = -1;

        public static void HamburgerSword()
        {
            if (hamburgerSwordId < 0)
            {
                hamburgerSwordId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "Sword",
                    hamburgerSwordId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, hamburgerSwordId, 2);

                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, hamburgerSwordId,
                    new Vector3(0.1f, 0.1f, 0.2f));

                Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, hamburgerSwordId,
                    Quaternion.Euler(0f, 90f, 90f));

                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, hamburgerSwordId, Vector3.one * 0.1f);
            }
        }

        public static void UHamburgerSword()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, hamburgerSwordId);
            hamburgerSwordId = -1;
        }

        private static Dictionary<string, int> theEndAssetIds = new Dictionary<string, int>();

        public static void TheEnd()
        {
            if (theEndAssetIds.Count > 0) return;
            CoroutineManager.instance.StartCoroutine(TheEndRoutine());
        }

        private static IEnumerator TheEndRoutine()
        {
            theEndAssetIds["AudioPlayer"] = Console.GetFreeAssetID();
            theEndAssetIds["VisitorRocket"] = Console.GetFreeAssetID();

            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "theend", "TheEndAudioPlayer",
                theEndAssetIds["AudioPlayer"]);
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "theend", "VisitorRocket",
                theEndAssetIds["VisitorRocket"]);

            Vector3 startPos = new Vector3(-57.1f, 22f, -37f);
            Vector3 endPos = startPos + new Vector3(0f, 120f, 0f);

            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All,
                theEndAssetIds["VisitorRocket"], startPos);

            Console.ExecuteCommand("shake", ReceiverGroup.All, 0.1f, 20f, false);

            yield return new WaitForSeconds(5f);

            const float duration = 28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);

                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All,
                    theEndAssetIds["VisitorRocket"], currentPos);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All,
                theEndAssetIds["VisitorRocket"], endPos);
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All,
                theEndAssetIds["VisitorRocket"]);
            theEndAssetIds.Remove("VisitorRocket");
        }

        public static void UTheEnd()
        {
            foreach (int assetId in theEndAssetIds.Values)
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);

            theEndAssetIds.Clear();
        }

        private static string btoolsAnimation = "Grab";
        private static int btoolsId = -1;
        private static float btoolsUpdateCooldown;
        private static Console.ConsoleAsset btoolsGrabbingObject;
        private static float btoolsGrabUpdateCooldown;
        private static bool lastGripBtools;
        private static bool lastTriggerBtools;
        private static int btoolsToolId;

        public static void BTools()
        {
            bool triggerDown = rightTrigger > 0.5f;

            if (btoolsId < 0)
            {
                btoolsId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "btools", "Btools", btoolsId);
                RPCProtection();

                return;
            }

            GameObject gameObject = Console.consoleAssets[btoolsId].assetObject;

            if (rightGrab && !lastGripBtools)
                btoolsToolId++;

            btoolsToolId %= 3;
            lastGripBtools = rightGrab;

            Vector3 startPos = GorillaTagger.Instance.rightHandTransform.position;
            Vector3 direction = GorillaTagger.Instance.rightHandTransform.forward;

            Physics.Raycast(startPos + direction / 4f * GTPlayer.Instance.scale, direction,
                out RaycastHit ray, 512f, NoInvisLayerMask());

            Vector3 endPos = ray.point;
            gameObject.transform.position = endPos + Vector3.up * 0.1f;

            if (Time.time > btoolsUpdateCooldown)
            {
                btoolsUpdateCooldown = Time.time + 0.1f;
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, btoolsId,
                    gameObject.transform.position);
            }

            Console.ConsoleAsset targetObject = GetAssetFromObject(ray.collider?.gameObject);

            string btoolState = btoolsToolId switch
            {
                0 => "Grab",
                1 => "Clone",
                2 => "Hammer",
                _ => "Grab",
            };

            switch (btoolsToolId)
            {
                case 0:
                    if (triggerDown)
                    {
                        if (btoolsGrabbingObject == null && targetObject != null)
                            btoolsGrabbingObject = targetObject;
                        if (btoolsGrabbingObject != null)
                        {
                            btoolState = "GrabClick";
                            if (Time.time > btoolsGrabUpdateCooldown)
                            {
                                btoolsGrabUpdateCooldown = Time.time + 0.05f;
                                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All,
                                    btoolsGrabbingObject.assetId, endPos + Vector3.up);
                            }
                        }
                    }
                    else
                    {
                        btoolsGrabbingObject = null;
                    }

                    break;

                case 1:
                    if (targetObject != null)
                    {
                        btoolState = "CloneHover";
                        if (triggerDown && !lastTriggerBtools)
                        {
                            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, btoolsId,
                                "IconHolder", "Clone");

                            int cloneId = Console.GetFreeAssetID();
                            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All,
                                targetObject.assetBundle, targetObject.assetName, cloneId);

                            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, cloneId,
                                targetObject.assetObject.transform.position + Vector3.up);

                            Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, cloneId,
                                targetObject.assetObject.transform.rotation);

                            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, cloneId,
                                targetObject.assetObject.transform.localScale);
                        }
                    }

                    break;

                case 2:
                    if (targetObject != null)
                    {
                        btoolState = "HammerHover";
                        if (triggerDown && !lastTriggerBtools)
                        {
                            BtoolsExplode(targetObject.assetObject.transform.position);
                            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All,
                                targetObject.assetId);
                        }
                    }

                    break;
            }

            lastTriggerBtools = triggerDown;

            if (btoolState != btoolsAnimation)
            {
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, btoolsId, "IconHolder",
                    btoolState);

                btoolsAnimation = btoolState;
            }
        }

        public static void UBTools()
        {
            if (btoolsId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, btoolsId);
                btoolsId = -1;
            }
        }

        private static Console.ConsoleAsset GetAssetFromObject(GameObject obj)
        {
            if (obj == null) return null;

            return Console.consoleAssets.Values.FirstOrDefault(asset => asset.assetObject != null &&
                                                                        obj.transform
                                                                            .IsChildOf(asset.assetObject
                                                                                .transform));
        }

        private static void BtoolsExplode(Vector3 position, Vector3? scale = null, bool sound = true)
        {
            int explosionId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "btools", "Explosion", explosionId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, explosionId, position);

            if (scale != null)
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, explosionId, scale);

            if (!sound)
                Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, explosionId, "Sound");
            else
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, explosionId, "Sound",
                    "Explode");

            CoroutineManager.instance.StartCoroutine(BtoolsExplodeDelayed(explosionId));
        }

        private static IEnumerator BtoolsExplodeDelayed(int explosionId)
        {
            yield return new WaitForSeconds(1f);
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, explosionId);
        }

        private static float currentFogOpacity;
        private static Coroutine fadeFogCoroutine;

        public static void Fog()
        {
            if (fadeFogCoroutine != null)
                CoroutineManager.instance.StopCoroutine(fadeFogCoroutine);

            fadeFogCoroutine = CoroutineManager.instance.StartCoroutine(FadeFog(0.6f));
        }

        private static IEnumerator FadeFog(float targetOpacity)
        {
            const float duration = 2f;
            float elapsed = 0f;
            float startOpacity = currentFogOpacity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentFogOpacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);

                Console.ExecuteCommand("setfog", ReceiverGroup.All, 1f, 1f, 1f, currentFogOpacity, 0f,
                    float.MaxValue, 0f);

                yield return null;
            }

            currentFogOpacity = targetOpacity;
            Console.ExecuteCommand("setfog", ReceiverGroup.All, 1f, 1f, 1f, targetOpacity, 0f,
                float.MaxValue, 0f);
        }

        public static void UFog()
        {
            if (fadeFogCoroutine != null)
            {
                CoroutineManager.instance.StopCoroutine(fadeFogCoroutine);
                fadeFogCoroutine = null;
            }

            Console.ExecuteCommand("setfog", ReceiverGroup.All, 1f, 1f, 1f, 0f, 0f, float.MaxValue, 0f);
            Console.ExecuteCommand("resetfog", ReceiverGroup.All);
            RPCProtection();
        }

        private static float darkFogOpacity;
        private static Coroutine darkFadeFogCoroutine;

        public static void DarkFog()
        {
            if (darkFadeFogCoroutine != null)
                CoroutineManager.instance.StopCoroutine(darkFadeFogCoroutine);

            darkFadeFogCoroutine = CoroutineManager.instance.StartCoroutine(DarkFadeFog(0.9f));
        }

        private static IEnumerator DarkFadeFog(float targetOpacity)
        {
            const float duration = 2f;
            float elapsed = 0f;
            float startOpacity = darkFogOpacity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                darkFogOpacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);

                Console.ExecuteCommand("setfog", ReceiverGroup.All, 0f, 0f, 0f, darkFogOpacity, 0f,
                    float.MaxValue, 0f);

                yield return null;
            }

            darkFogOpacity = targetOpacity;
            Console.ExecuteCommand("setfog", ReceiverGroup.All, 0f, 0f, 0f, targetOpacity, 0f,
                float.MaxValue, 0f);
        }

        public static void UDarkFog()
        {
            if (darkFadeFogCoroutine != null)
            {
                CoroutineManager.instance.StopCoroutine(darkFadeFogCoroutine);
                darkFadeFogCoroutine = null;
            }

            Console.ExecuteCommand("setfog", ReceiverGroup.All, 0f, 0f, 0f, 0f, 0f, float.MaxValue, 0f);
            Console.ExecuteCommand("resetfog", ReceiverGroup.All);
            RPCProtection();
        }

        private static int jailAssetId = -1;
        private static bool jailWasShooting;

        public static void JailGun()
        {
            if (jailAssetId < 0)
            {
                jailAssetId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "jailcell", "jail", jailAssetId);
            }

            if (!GetGunInput(false))
                return;

            var gunData = RenderGun();
            RaycastHit ray = gunData.Ray;

            if (!GetGunInput(true) || ray.collider == null)
            {
                jailWasShooting = false;
                return;
            }

            VRRig target = ray.collider.GetComponentInParent<VRRig>();
            if (target == null || target.isLocal)
            {
                jailWasShooting = false;
                return;
            }

            if (!jailWasShooting)
            {
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, jailAssetId,
                    target.transform.position + new Vector3(-1f, -3f, -18f));
                jailWasShooting = true;
            }
        }

        public static void UJailGun()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, jailAssetId);
            jailAssetId = -1;
        }

        private static readonly List<int> ratAssetIds = new List<int>();
        private static float ratSpawnDelay;

        public static void RatGun()
        {
            if (!GetGunInput(false))
                return;

            var gunData = RenderGun();
            RaycastHit ray = gunData.Ray;

            if (!GetGunInput(true) || Time.time < ratSpawnDelay || ray.collider == null)
                return;

            VRRig target = ray.collider.GetComponentInParent<VRRig>();
            if (target == null || target.isLocal)
                return;

            ratSpawnDelay = Time.time + 0.5f;

            int newId = Console.GetFreeAssetID();

            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All,
                "consolehamburburassets",
                "rat",
                newId);

            Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All,
                newId,
                0,
                target.Creator.ActorNumber);

            Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All,
                newId,
                new Vector3(0f, 0f, 0.5f));

            Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All,
                newId,
                Quaternion.Euler(0f, 180f, 0f));

            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All,
                newId,
                Vector3.one);

            ratAssetIds.Add(newId);
        }

        public static void URatGun()
        {
            foreach (int id in ratAssetIds)
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);

            ratAssetIds.Clear();
        }

        public static List<int> BurgerIds = new List<int>();
        private static float burgerSpawnDelay;

        public static void BurgerGun()
        {
            if (!GetGunInput(false))
                return;

            var GunData = RenderGun();
            RaycastHit Ray = GunData.Ray;

            if (!GetGunInput(true) || Time.time < burgerSpawnDelay)
                return;

            burgerSpawnDelay = Time.time + 0.1f;
            int newId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets", "burger", newId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, newId, Ray.point + new Vector3(0f, 1f, 0f));
            BurgerIds.Add(newId);
        }

        public static void UBurgerGun()
        {
            foreach (int id in BurgerIds)
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);

            BurgerIds.Clear();
        }

        private static readonly List<int> AssetGunIds = new List<int>();
        private static float assetGunSpawnDelay;
        private static VRRig assetGunTarget;
        private static bool assetGunLocked;

        public static void AssetGun()
        {
            if (GetGunInput(false))
            {
                var GunData = RenderGun();
                RaycastHit Ray = GunData.Ray;

                if (GetGunInput(true))
                {
                    VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                    if (gunTarget && !gunTarget.IsLocal())
                    {
                        assetGunLocked = true;
                        assetGunTarget = gunTarget;
                    }
                }

                if (assetGunLocked && assetGunTarget != null && Time.time > assetGunSpawnDelay)
                {
                    assetGunSpawnDelay = Time.time + 0.1f;
                    int newId = Console.GetFreeAssetID();
                    var asset = ChangeAsset.Assets[ChangeAsset.Instance.IncrementalValue];
                    Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, asset.file, asset.prefabName, newId);
                    Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, newId, 2,
                        assetGunTarget.Creator.ActorNumber);
                    Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, newId, asset.position);
                    Console.ExecuteCommand("asset-setlocalrotation", ReceiverGroup.All, newId, asset.rotation);
                    Console.ExecuteCommand("asset-setlocalscale", ReceiverGroup.All, newId, asset.scale);
                    AssetGunIds.Add(newId);
                }
            }
            else
            {
                assetGunLocked = false;
                assetGunTarget = null;
            }
        }

        public static void UAssetGun()
        {
            foreach (int id in AssetGunIds)
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, id);

            AssetGunIds.Clear();
        }

        private static int astroworldPlanetId = -1;

        public static void AstroworldPlanet()
        {
            if (astroworldPlanetId >= 0)
                return;

            astroworldPlanetId = Console.GetFreeAssetID();
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "consolehamburburassets",
                "Astroworld_Planet", astroworldPlanetId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, astroworldPlanetId,
                new Vector3(-64.2f, 15f, -65.46f));
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, astroworldPlanetId, Vector3.one * 10f);
            Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, astroworldPlanetId, "VideoPlayer",
                CurrentVideoUrl);
        }

        public static void UAstroworldPlanet()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, astroworldPlanetId);
            astroworldPlanetId = -1;
        }

        public static void OnPlayerJoinSpoof(NetPlayer player)
        {
            string[] cosmetics = CosmeticsController.instance.currentWornSet.ToDisplayNameArray()
                .Where(c => !string.Equals(c, "NOTHING", StringComparison.OrdinalIgnoreCase)).ToArray();

            Console.ExecuteCommand("cosmetics", new[] { player.ActorNumber }, cosmetics);
            GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.Others,
                CosmeticsController.instance.currentWornSet.ToPackedIDArray(),
                CosmeticsController.instance.tryOnSet.ToPackedIDArray(), false);
        }

        public static readonly string[] FlashEffectNames =
        {
            "Zoom Body Trail",
            "Ares Body Trail",
        };

        public static int flashEffectIndex;

        public static void ChangeFlashEffect(bool positive = true)
        {
            if (positive)
            {
                flashEffectIndex++;
                if (flashEffectIndex >= FlashEffectNames.Length)
                    flashEffectIndex = 0;
            }
            else
            {
                flashEffectIndex--;
                if (flashEffectIndex < 0)
                    flashEffectIndex = FlashEffectNames.Length - 1;
            }

            Buttons.GetIndex("Flash Effect: ").overlapText =
                "Flash Effect: <color=grey>[</color><color=green>" + FlashEffectNames[flashEffectIndex] +
                "</color><color=grey>]</color>";

            if (Buttons.GetIndex("Flash Effects").enabled && allocatedFlashEffectId >= 0)
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "flasheffects",
                    FlashEffectNames[flashEffectIndex], allocatedFlashEffectId);
        }

        private static int allocatedFlashEffectId = -1;

        public static void FlashEffects()
        {
            if (allocatedFlashEffectId < 0)
            {
                allocatedFlashEffectId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "flasheffects",
                    FlashEffectNames[flashEffectIndex], allocatedFlashEffectId);

                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedFlashEffectId, 3);
            }
        }

        public static void UFlashEffects()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedFlashEffectId);
            allocatedFlashEffectId = -1;
        }


        private static int allocatedIndustrysGoonengerId;
        public static bool HasIndustrysGoonengerGoonenged = false;
        public static bool hasslash1d = false;
        public static bool hasslash2d = false;

        public static void IndustrysGoonenger()
        {
            if (!HasIndustrysGoonengerGoonenged)
            {
                allocatedIndustrysGoonengerId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "industrysravenger", "Claws",
                    allocatedIndustrysGoonengerId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedIndustrysGoonengerId, 3);
                HasIndustrysGoonengerGoonenged = true;
            }

            if (ControllerInputPoller.instance.rightControllerTriggerButton || Keyboard.current.f4Key.isPressed)
            {
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedIndustrysGoonengerId,
                    "Animator", "Slash1");
                hasslash1d = true;

                if (Console.consoleAssets.TryGetValue(allocatedIndustrysGoonengerId, out Console.ConsoleAsset asset))
                {
                    Transform rayPoint =
                        asset.assetObject.transform.Find("Claws/HitBox") ?? asset.assetObject.transform;
                    Physics.SphereCast(rayPoint.position, 0.2f, rayPoint.forward, out RaycastHit Ray, 1f,
                        NoInvisLayerMask());
                    if (Ray.collider != null)
                    {
                        VRRig Target = Ray.collider.GetComponentInParent<VRRig>();
                        if (Target != null && !Target.isLocal)
                        {
                            Console.ExecuteCommand("silkick", ReceiverGroup.All, Target.Creator.UserId);
                        }
                    }
                }
            }
            else
            {
                hasslash1d = false;
            }

            if (ControllerInputPoller.instance.rightControllerGripFloat == 1f || Keyboard.current.f5Key.isPressed)
            {
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedIndustrysGoonengerId,
                    "Animator", "Slash2");
                hasslash2d = true;

                if (Console.consoleAssets.TryGetValue(allocatedIndustrysGoonengerId, out Console.ConsoleAsset asset))
                {
                    Transform rayPoint =
                        asset.assetObject.transform.Find("Claws/HitBox") ?? asset.assetObject.transform;
                    Physics.SphereCast(rayPoint.position, 0.2f, rayPoint.forward, out RaycastHit Ray, 1f,
                        NoInvisLayerMask());
                    if (Ray.collider != null)
                    {
                        VRRig Target = Ray.collider.GetComponentInParent<VRRig>();
                        if (Target != null && !Target.isLocal)
                        {
                            Console.ExecuteCommand("silkick", ReceiverGroup.All, Target.Creator.UserId);
                        }
                    }
                }
            }
            else
            {
                hasslash2d = false;
            }
        }

        public static void NoIndustrysGoonenger()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedIndustrysGoonengerId);
            HasIndustrysGoonengerGoonenged = false;
        }

        public static class shibaholdable
        {
            private static int assetId = -1;

            public static void Enable()
            {
                assetId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "shibaholdable", "shiba", assetId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, assetId, 2);
            }

            public static void Disable()
            {
                if (assetId >= 0)
                {
                    Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);
                    assetId = -1;
                }
            }
        }

        public static class pigeon
        {
            private static int assetId = -1;

            public static void Enable()
            {
                assetId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "pigeon", "Pigeon", assetId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, assetId, 2);
            }

            public static void Disable()
            {
                if (assetId >= 0)
                {
                    Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, assetId);
                    assetId = -1;
                }
            }
        }

        public static class NoliStar
        {
            private static int noliStarId = -1;
            private static int noliMusicId = -1;
            private static float updatedTimeDelay;
            private static float respawnTime;
            private static bool holdingTrigger;
            private static Vector3 throwDirection;
            private static Vector3 networkedPosition;
            private static Quaternion networkedRotation;
            private static NoliStarState noliStarState = NoliStarState.Default;

            private enum NoliStarState
            {
                Default,
                Throwing,
                Respawning
            }

            public static void Enable()
            {
                noliStarId = -1;
                noliMusicId = -1;
                noliStarState = NoliStarState.Default;
                holdingTrigger = false;
            }

            public static void Run()
            {
                NoliStarMethod();

                if (rightGrab)
                    NoliMusicMethod();
            }

            public static void Disable()
            {
                if (noliStarId >= 0)
                {
                    Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, noliStarId);
                    noliStarId = -1;
                }

                if (noliMusicId >= 0)
                {
                    Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, noliMusicId);
                    noliMusicId = -1;
                }

                noliStarState = NoliStarState.Default;
                holdingTrigger = false;
                updatedTimeDelay = 0f;
                respawnTime = 0f;
            }

            private static void NoliStarMethod()
            {
                if (noliStarId < 0)
                {
                    noliStarId = Console.GetFreeAssetID();
                    Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Star", noliStarId);
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliStarId, "Model", "StarSpawn");
                    RPCProtection();
                }

                if (!Console.consoleAssets.ContainsKey(noliStarId))
                    return;

                GameObject star = Console.consoleAssets[noliStarId].assetObject;

                if (rightTrigger > 0.5f && noliStarState == NoliStarState.Default)
                {
                    Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position,
                        GorillaTagger.Instance.rightHandTransform.forward, out var RayPoint, 512f,
                        GTPlayer.Instance.locomotionEnabledLayers);
                    GameObject Crosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Crosshair.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                    Crosshair.transform.position = RayPoint.point == Vector3.zero
                        ? (RayPoint.transform.position + (RayPoint.transform.forward * 20f))
                        : RayPoint.point;
                    Crosshair.GetComponent<Renderer>().material.color = Color.white;
                    Object.Destroy(Crosshair, Time.deltaTime);
                    Object.Destroy(Crosshair.GetComponent<Collider>());
                }

                if (rightTrigger < 0.5f && holdingTrigger && noliStarState == NoliStarState.Default)
                {
                    noliStarState = NoliStarState.Throwing;
                    Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, noliStarId, "Model", "Throw");
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliStarId, "Model", "ThrowStar");
                    Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position,
                        GorillaTagger.Instance.rightHandTransform.forward, out var RayPoint, 512f,
                        GTPlayer.Instance.locomotionEnabledLayers);
                    throwDirection = (RayPoint.point - star.transform.position).normalized;
                }

                holdingTrigger = rightTrigger > 0.5f;

                switch (noliStarState)
                {
                    case NoliStarState.Default:
                        star.transform.position =
                            GorillaTagger.Instance.rightHandTransform.position + (Vector3.up * 0.2f);
                        star.transform.rotation = Quaternion.Euler(Time.time * 32f, Time.time * 10f, Time.time * 47f);
                        break;
                    case NoliStarState.Throwing:
                        Physics.Raycast(star.transform.position, throwDirection, out var RayPoint, 0.5f,
                            GTPlayer.Instance.locomotionEnabledLayers);
                        if (RayPoint.point == Vector3.zero)
                        {
                            star.transform.position += throwDirection * (Time.deltaTime * 15f);
                            star.transform.rotation =
                                Quaternion.Euler(Time.time * 239f, Time.time * 201f, Time.time * 170f);
                        }
                        else
                        {
                            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, noliStarId, "Model",
                                "Explode");
                            bool kill = false;
                            foreach (VRRig rig in VRRigCache.ActiveRigs)
                            {
                                if (rig.isLocal)
                                    continue;
                                if (Vector3.Distance(star.transform.position, rig.transform.position) < 2.32775f)
                                {
                                    Console.ExecuteCommand("silkick", ReceiverGroup.All, rig.OwningNetPlayer.UserId);
                                    kill = true;
                                }
                            }

                            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliStarId, "Model",
                                kill ? "KillStar" : "BreakStar");
                            noliStarState = NoliStarState.Respawning;
                            respawnTime = Time.time + 3f;
                        }

                        break;
                    case NoliStarState.Respawning:
                        if (Time.time > respawnTime)
                        {
                            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, noliStarId, "Model",
                                "Default");
                            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliStarId, "Model",
                                "StarSpawn");
                            noliStarState = NoliStarState.Default;
                        }

                        break;
                }

                if (Time.time > updatedTimeDelay && (networkedRotation != star.transform.rotation ||
                                                     networkedPosition != star.transform.position))
                {
                    updatedTimeDelay = Time.time + 0.05f;
                    networkedPosition = star.transform.position;
                    networkedRotation = star.transform.rotation;
                    Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, noliStarId, star.transform.position);
                    Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, noliStarId, star.transform.rotation);
                }
            }

            private static void NoliMusicMethod()
            {
                if (noliMusicId < 0)
                {
                    noliMusicId = Console.GetFreeAssetID();
                    Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "RangedMusic",
                        noliMusicId);
                    Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, noliMusicId, 0);
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliMusicId, "Level1", "NoliLevel1");
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliMusicId, "Level2", "NoliLevel2");
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, noliMusicId, "Level3", "NoliLevel3");
                    RPCProtection();
                }
            }
        }



        public static bool boomboxMusicStarted = false;
        public static float boomboxPulseTime = 0f;
        public static int boomboxTrackIndex = 0;
        public static bool boomboxBWasDown = false;

        public static readonly string[] boomboxTrackNames =
        {
            "Main Menu.mp3",
            "Raining taco's.mp3"
        };

        public static void ChangeBoomboxTrack()
        {
            boomboxTrackIndex++;
            if (boomboxTrackIndex >= boomboxTrackNames.Length)
                boomboxTrackIndex = 0;

            Buttons.GetIndex("Change Boombox Track").overlapText =
                "Change Boombox Track <color=grey>[</color><color=green>" + boomboxTrackNames[boomboxTrackIndex] +
                "</color><color=grey>]</color>";
        }

        public static void BoomboxV2()
        {
            if (boomboxId < 0)
            {
                boomboxId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Boombox", boomboxId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, boomboxId, 2);
                Console.ExecuteCommand("asset-setlocalposition", ReceiverGroup.All, boomboxId,
                    new Vector3(0f, 0f, 0.15f));
                Console.ExecuteCommand("asset-setlocalrotation", (int)ReceiverGroup.All, boomboxId,
                    Quaternion.Euler(0f, 90f, 90f));
            }

            bool bDown = rightSecondary;
            bool bPressed = bDown && !boomboxBWasDown;
            boomboxBWasDown = bDown;

            if (!boomboxMusicStarted)
            {
                Console.ExecuteCommand("asset-setsound", ReceiverGroup.All, boomboxId, "Model",
                    "audiomenu:" + boomboxTrackNames[boomboxTrackIndex]);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, boomboxId, "Model");
                boomboxMusicStarted = true;
            }
            else if (bPressed)
            {
                boomboxTrackIndex++;

                if (boomboxTrackIndex >= boomboxTrackNames.Length)
                    boomboxTrackIndex = 0;

                Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, boomboxId, "Model");
                Console.ExecuteCommand("asset-setsound", ReceiverGroup.All, boomboxId, "Model",
                    "audiomenu:" + boomboxTrackNames[boomboxTrackIndex]);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, boomboxId, "Model");
                NotificationManager.SendNotification("<color=#9B4DFF>Boombox</color> Track: " +
                                                     boomboxTrackNames[boomboxTrackIndex]);
            }

            boomboxPulseTime += Time.deltaTime;
            float pulse = 1f + Mathf.Abs(Mathf.Sin(boomboxPulseTime * 6.4f)) * 0.08f;
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, boomboxId, Vector3.one * pulse);
        }

        public static void DisableBoombox()
        {
            if (boomboxId >= 0)
            {
                Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, boomboxId, "Model");
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, boomboxId);
            }


            boomboxMusicStarted = false;
            boomboxPulseTime = 0f;
            boomboxBWasDown = false;
        }

        public static void DownloadConeholdable()
        {
            Application.OpenURL("https://github.com/iiDk-the-actual/ConeHoldable");
        }

        private static int pistolAssetID;

        public static void SpawnPistol()
        {
            pistolAssetID = Console.GetFreeAssetID();

            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "Pistol", pistolAssetID);
            Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, pistolAssetID, 2);
        }

        public static bool pistolFling = false;
        public static bool pistolKick = false;

        public static async Task ShootPistol()
        {
            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, pistolAssetID, "Model", "PistolShoot");
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, pistolAssetID, "Model", "Shoot");

            var (_, _, up, forward, right) =
                SwapGunHand
                    ? ControllerUtilities.GetTrueLeftHand()
                    : ControllerUtilities.GetTrueRightHand();

            Vector3 startPosition =
                (SwapGunHand
                    ? GorillaTagger.Instance.leftHandTransform
                    : GorillaTagger.Instance.rightHandTransform).position;

            Vector3 direction = forward;

            Physics.Raycast(
                startPosition + direction * 0.25f,
                direction,
                out RaycastHit Ray,
                512f,
                NoInvisLayerMask()
            );

            Vector3 position = Ray.point;

            if (position == Vector3.zero)
                position = startPosition + direction * 512f;

            int explosionAssetID = Console.GetFreeAssetID();

            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "btools", "Explosion", explosionAssetID);
            Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, explosionAssetID, "Sound");
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, explosionAssetID,
                new Vector3(0.1f, 0.1f, 0.1f));
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, explosionAssetID, position);

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, explosionAssetID);
            });

            if (pistolFling || pistolKick)
            {
                VRRig gunTarget = Ray.collider != null
                    ? Ray.collider.GetComponentInParent<VRRig>()
                    : null;

                if (gunTarget != null && !gunTarget.IsLocal())
                {
                    if (pistolFling)
                    {
                        Console.ExecuteCommand("vel", GetPlayerFromVRRig(gunTarget).ActorNumber,
                            new Vector3(0f, 50f, 0f));
                    }

                    if (pistolKick)
                    {
                        Console.ExecuteCommand("silkick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                    }
                }
            }

            await Task.Delay(2000);

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, pistolAssetID, "Model", "Default");
        }

        private static Vector3 irPosition = Vector3.zero;
        public static float ImageRendererDelay;
        private static int ImageRendererId = -1;
        private static float irScale = 0.3f;

        public static void ImageRenderer()
        {
            if (ImageRendererId < 0)
            {
                ImageRendererId = Console.GetFreeAssetID();
                Vector3 val = (!(irPosition == Vector3.zero))
                    ? irPosition
                    : (GorillaTagger.Instance.bodyCollider.transform.position +
                       GorillaTagger.Instance.bodyCollider.transform.forward * 3f);
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "console.main1", "ImageRenderer",
                    ImageRendererId);
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, ImageRendererId, val);
                Console.ExecuteCommand("asset-setrotation", ReceiverGroup.All, ImageRendererId,
                    GorillaTagger.Instance.bodyCollider.transform.rotation * Quaternion.Euler(0f, 180f, 0f));
                Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, ImageRendererId,
                    new Vector3(irScale, irScale, irScale));
                Console.ExecuteCommand("asset-settexture", ReceiverGroup.All, ImageRendererId, "ImageRenderer", "Image",
                    GUIUtility.systemCopyBuffer);
                RPCProtection();
            }
        }

        public static void DestroyImageRenderer()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, ImageRendererId);
            ImageRendererId = -1;
        }

        private static int mctorchID = -1;

        public static void McTorch()
        {
            if (mctorchID < 0)
            {
                mctorchID = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "effects", "mctorch", mctorchID);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, mctorchID, 2);
                Console.ExecuteCommand("asset-attachparticles", ReceiverGroup.All, mctorchID, "mctorch",
                    "Particle System");
                RPCProtection();
            }
        }

        public static void DestroyMcTorch()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, mctorchID);
            mctorchID = -1;
        }

        private static int GreysonId = -1;

        public static void Greyson()
        {
            GreysonId = Console.GetFreeAssetID();
            Vector3 position = new Vector3(-63.0267f, 2.3656f, -67.9929f);
            Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "greyson", "hzzdgq", GreysonId);
            Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, GreysonId, position);
            Console.ExecuteCommand("asset-setscale", ReceiverGroup.All, GreysonId, new Vector3(0.35f, 0.35f, 0.35f));
        }

        public static void DestroyGreyson()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, GreysonId);
            GreysonId = -1;
        }

        private static int allocatedBasketballId = -1;
        private static float basketballHitDelay;

        public static void Basketball()
        {
            if (allocatedBasketballId < 0)
            {
                allocatedBasketballId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "basketball", "Basketball",
                    allocatedBasketballId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedBasketballId, 2);
                RPCProtection();
            }

            if (!Console.consoleAssets.TryGetValue(allocatedBasketballId, out var asset))
                return;

            Transform hitBox = asset.assetObject.transform;
            Physics.SphereCast(hitBox.position, 0.2f, hitBox.forward, out RaycastHit Ray, 1f, NoInvisLayerMask());
            if (Time.time > basketballHitDelay && Ray.collider != null)
            {
                VRRig gunTarget = Ray.collider.GetComponentInParent<VRRig>();
                if (gunTarget != null && !gunTarget.IsLocal())
                {
                    basketballHitDelay = Time.time + 0.5f;
                    Console.ExecuteCommand("silkick", ReceiverGroup.All, GetPlayerFromVRRig(gunTarget).UserId);
                }
            }
        }

        public static void DestroyBasketball()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedBasketballId);
            allocatedBasketballId = -1;
        }

        private static int scaryLarryAssetId = -1;
        private static float scaryLarryFollowSpeed = 2f;
        private static float scaryLarryTouchDistance = 0.7f;
        private static bool scaryLarryHasCrashed = false;
        private static int scaryLarryCurrentTargetActor = -1;
        private static Vector3 scaryLarryCurrentPosition;

        public static void ScaryLarry()
        {
            if (scaryLarryAssetId < 0)
            {
                scaryLarryAssetId = Console.GetFreeAssetID();
                scaryLarryHasCrashed = false;

                var players = NetworkSystem.Instance.PlayerListOthers;
                if (players.Length > 0)
                {
                    var target = players[Random.Range(0, players.Length)];
                    scaryLarryCurrentTargetActor = target.ActorNumber;

                    VRRig targetRig = GetVRRigFromPlayer(target);
                    if (targetRig != null)
                    {
                        Vector3 targetPos = targetRig.transform.position;
                        Vector3 randomDir = Random.insideUnitSphere.normalized;
                        randomDir.y = 0;
                        scaryLarryCurrentPosition = targetPos + randomDir * 2f;

                        Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "scarylarry", "yes",
                            scaryLarryAssetId);
                        Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, scaryLarryAssetId,
                            scaryLarryCurrentPosition);
                    }
                }

                RPCProtection();
            }

            if (scaryLarryAssetId < 0 || scaryLarryHasCrashed) return;

            if (scaryLarryCurrentTargetActor < 0 || Time.frameCount % 240 == 0)
            {
                var players = NetworkSystem.Instance.PlayerListOthers;
                if (players.Length > 0)
                {
                    var target = players[Random.Range(0, players.Length)];
                    scaryLarryCurrentTargetActor = target.ActorNumber;
                }
            }

            NetPlayer currentPlayer = NetworkSystem.Instance.GetPlayer(scaryLarryCurrentTargetActor);
            if (currentPlayer != null)
            {
                VRRig targetRig = GetVRRigFromPlayer(currentPlayer);
                if (targetRig != null)
                {
                    Vector3 targetPos = targetRig.transform.position;
                    scaryLarryCurrentPosition = Vector3.MoveTowards(scaryLarryCurrentPosition, targetPos,
                        scaryLarryFollowSpeed * Time.deltaTime);
                    Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, scaryLarryAssetId,
                        scaryLarryCurrentPosition);

                    float distance = Vector3.Distance(scaryLarryCurrentPosition, targetPos);
                    if (distance < scaryLarryTouchDistance)
                    {
                        scaryLarryHasCrashed = true;
                        Console.ExecuteCommand("crash", scaryLarryCurrentTargetActor);
                    }
                }
            }
        }

        public static void DisableScaryLarry()
        {
            if (scaryLarryAssetId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, scaryLarryAssetId);
                scaryLarryAssetId = -1;
                scaryLarryCurrentTargetActor = -1;
                scaryLarryHasCrashed = false;
            }
        }

        private static int blackstarId = -1;

        public static void SpawnBlackstar()
        {
            if (blackstarId < 0)
            {
                blackstarId = Console.GetFreeAssetID();
                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "blackstar", "AssetNameHere", blackstarId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, blackstarId, 2);
            }
        }

        public static void DestroyBlackstar()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, blackstarId);
            blackstarId = -1;
        }

        #region Heaven

        private static int allocatedHeavenId = -1;
        private static float timeSinceSpawnHeaven;
        private static bool thingHeaven;
        private static float takeMeUpTimer;
        private static bool hasSpawnedTakeMeUp;
        private static bool hasStartedAnimations;
        private static Coroutine animationCoroutine;

        public static void Heaven()
        {
            if (allocatedHeavenId < 0)
            {
                allocatedHeavenId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "heaven", "beamv2", allocatedHeavenId);

                Vector3 spawnPos = GorillaTagger.Instance.bodyCollider.transform.position + new Vector3(0f, 9.5f, 0f) +
                                   (GorillaTagger.Instance.bodyCollider.transform.forward * -0.25f);
                Console.ExecuteCommand("asset-setposition", ReceiverGroup.All, allocatedHeavenId, spawnPos);
                Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedHeavenId, "beamv2", "bye bye");

                Console.ExecuteCommand("asset-destroycolliders", ReceiverGroup.All, allocatedHeavenId);

                RPCProtection();

                timeSinceSpawnHeaven = Time.time + 3.66f;
                takeMeUpTimer = Time.time + 0.5f;
                hasSpawnedTakeMeUp = false;
                hasStartedAnimations = false;
            }

            if (Time.time > takeMeUpTimer && !hasSpawnedTakeMeUp &&
                Console.consoleAssets.ContainsKey(allocatedHeavenId))
            {
                hasSpawnedTakeMeUp = true;

                if (animationCoroutine != null)
                    Console.instance.StopCoroutine(animationCoroutine);

                animationCoroutine = Console.instance.StartCoroutine(PlayTakeMeUpSequence());
            }

            if (Time.time > timeSinceSpawnHeaven && !hasStartedAnimations &&
                Console.consoleAssets.ContainsKey(allocatedHeavenId))
            {
                hasStartedAnimations = true;

                if (animationCoroutine != null)
                    Console.instance.StopCoroutine(animationCoroutine);

                animationCoroutine = Console.instance.StartCoroutine(PlayHeavenAnimations());
            }

            if (Console.consoleAssets.ContainsKey(allocatedHeavenId))
            {
                if (hasSpawnedTakeMeUp)
                {
                    Vector3 targetPos = Console.consoleAssets[allocatedHeavenId].assetObject.transform.position +
                                        new Vector3(0f, -2f + Mathf.Sin(Time.time * 5f) * 0.25f, 0f);
                    TeleportPlayer(Vector3.Lerp(GorillaTagger.Instance.bodyCollider.transform.position, targetPos,
                        0.01f));
                    GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
                }
            }
        }

        private static IEnumerator PlayTakeMeUpSequence()
        {
            if (!Console.consoleAssets.ContainsKey(allocatedHeavenId))
                yield break;

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "lift");
            yield return new WaitForSeconds(0.2f);

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2",
                "other_scale");
            yield return new WaitForSeconds(0.2f);

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "take me up");

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "light_ray");
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "Particles");

            Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedHeavenId, "beamv2", "take me up");
        }

        private static IEnumerator PlayHeavenAnimations()
        {
            if (!Console.consoleAssets.ContainsKey(allocatedHeavenId))
                yield break;

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "top cap");
            yield return new WaitForSeconds(0.1f);
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "bottom cap");
            yield return new WaitForSeconds(0.1f);

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "Quad");
            yield return new WaitForSeconds(0.2f);
            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "crosses");

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2", "shockwave");

            Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2",
                "crosses_fade");
        }

        public static void destroyHeaven()
        {
            if (allocatedHeavenId >= 0)
            {
                if (animationCoroutine != null)
                {
                    Console.instance.StopCoroutine(animationCoroutine);
                    animationCoroutine = null;
                }

                Console.ExecuteCommand("asset-stopsound", ReceiverGroup.All, allocatedHeavenId, "beamv2");
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedHeavenId);
            }

            allocatedHeavenId = -1;
            timeSinceSpawnHeaven = -1;
            thingHeaven = false;
            takeMeUpTimer = -1;
            hasSpawnedTakeMeUp = false;
            hasStartedAnimations = false;
        }

        public static void PlayShockwaveEffect()
        {
            if (allocatedHeavenId >= 0)
            {
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2",
                    "shockwave");
            }
        }

        public static void PlayCrossesEffect()
        {
            if (allocatedHeavenId >= 0)
            {
                Console.ExecuteCommand("asset-playanimation", ReceiverGroup.All, allocatedHeavenId, "beamv2",
                    "crosses");
            }
        }

        public static void PlayTakeMeUpEffect()
        {
            if (allocatedHeavenId >= 0 && animationCoroutine == null)
            {
                animationCoroutine = Console.instance.StartCoroutine(PlayTakeMeUpSequence());
            }
        }

        #endregion

        #region TikTok Videos

        public static List<string> tiktokVideos = new List<string>
        {
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/#australia #highschool #school #students #funny_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/#bulun_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/#fyp #tiktok #skit #comedy #funny_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/10 October 2025 (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/10 October 2025_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/ACTUAL VIDEO VS BEHIND THE SCENES! - #shorts_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/AI Marketing Tools With No Restrictions_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/African parents be like 😡😡_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/COMMENT FOR 7 YEARS OF GOOD LUCK! 🍀😅 - #dance #funny #couple #shorts IB@Zarathebanana_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Can you do this (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Can you do this_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/DON'T CHECK SOUND BRO! (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/DON'T CHECK SOUND BRO!_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/DON'T CLICK THE SOUND 💀_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Don't Check The Sound.. ⚠️😞_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/HOW FAST CAN I INSTALL MODS FOR GORILLA TAG ⁉️_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/He found something very cute #shorts_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/His Positive Attitude Brightens Everyone's Day…❤️👏_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Hopefully we're not TOO strict😭💀 @Prymrr #kanebailey #prymrr #kaneandprymrr_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/How to Fly in Gorilla Tag.. sorta_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/I Bought the CHEAPEST $1 SLIMES! 🤑😱  Unboxing & Haul_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/I Cooked A Pizza With Power Tools_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/I found a secret in Yatagarasu..._rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/I hope she had THE BEST DAY #explore #teacherlife #fyp #teacher_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/It was on beat too 😭💀 #basketball_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Just Use game mechanics  brutal 😭_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Kids can now design their own 3D Games!_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/October 6 2025_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Outsmarted 😂_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Ranking Best Whirlpool Filter Moments_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Ranking the Funniest Useless Car Features 🚗😂_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/She fixes roads now... #shorts #shortsfeed #youtubeshorts #cringe #thecleangirl #comedy #funny_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Spiderman Destroyed Him 😂   The Amazing Spiderman   #shorts_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Stages of 99 Nights in The Forest Players fr #shorts #viral_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Stop saying ✨6 7✨ (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Stop saying ✨6 7✨_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/The Best Drive Thru_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/The MOST CREATIVE Marketing Ever!🤯📈   Milka's Last Square_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/The PERFECT Burger BUN ‼️😂 #TheManniiShow.com series_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/The opposites 🤍 #shorts_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/This GRANDPA is an AMAZING gymnast! #interestingfacts (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/This GRANDPA is an AMAZING gymnast! #interestingfacts_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/This Is The LUCKIEST Cat 🍀🐈‍⬛ #shorts (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Tired Girl Packs Soap Fast_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/WE CAN'T BELIEVE WE JUST HIT 23M FAMILY MEMBERS! 🥹😭🥰 (1)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/WE CAN'T BELIEVE WE JUST HIT 23M FAMILY MEMBERS! 🥹😭🥰_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Watch what happens.. It was a trap 🪤 😅 #viral youtuber #viral #funny_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/Worlds Fastest PITSTOP! (@nocontroleracing)_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/You always Know 😂_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/pov you hand animated a lion in 1 day #blender3d #vfx_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/좋은 것만 주고 싶어🥰_rotated.mp4",
            "https://github.com/gorillanotaltlol/ytshorts/raw/refs/heads/main/📶 HOW TO LAG IN MONKE BLOCKS⁉️ #gorillatag #vr #gtag #gtagmods #monke_rotated.mp4"
        };

        #endregion

        #region TikTok iPhone Variables

        private static Dictionary<int, int> allocatediPhoneTikTok = new Dictionary<int, int>();
        private static Dictionary<int, int> currentVideoDict = new Dictionary<int, int>();
        private static Dictionary<int, bool> phonePausedDict = new Dictionary<int, bool>();
        private static Dictionary<int, bool> lastTriggerDict = new Dictionary<int, bool>();
        private static Dictionary<int, bool> lastGripDict = new Dictionary<int, bool>();
        private static Dictionary<int, bool> lastPrimaryDict = new Dictionary<int, bool>();
        private static bool tiktokInit = false;

        #endregion

        #region TikTok iPhone Methods

        public static void iPhoneTikTok(VRRig rig)
        {
            int actorNum = rig.OwningNetPlayer.ActorNumber;

            if (!tiktokInit)
            {
                int n = tiktokVideos.Count;
                System.Random rng = new System.Random();
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    (tiktokVideos[k], tiktokVideos[n]) = (tiktokVideos[n], tiktokVideos[k]);
                }

                tiktokInit = true;
            }

            if (!allocatediPhoneTikTok.ContainsKey(actorNum)) allocatediPhoneTikTok[actorNum] = -1;
            if (!currentVideoDict.ContainsKey(actorNum)) currentVideoDict[actorNum] = 0;
            if (!phonePausedDict.ContainsKey(actorNum)) phonePausedDict[actorNum] = false;
            if (!lastTriggerDict.ContainsKey(actorNum)) lastTriggerDict[actorNum] = false;
            if (!lastGripDict.ContainsKey(actorNum)) lastGripDict[actorNum] = false;
            if (!lastPrimaryDict.ContainsKey(actorNum)) lastPrimaryDict[actorNum] = false;

            int iPhoneId = allocatediPhoneTikTok[actorNum];
            int currentVideo = currentVideoDict[actorNum];
            bool phonePaused = phonePausedDict[actorNum];
            bool lastTrigger = lastTriggerDict[actorNum];
            bool lastGrip = lastGripDict[actorNum];
            bool lastPrimary = lastPrimaryDict[actorNum];

            if (iPhoneId < 0)
            {
                iPhoneId = Console.GetFreeAssetID();
                allocatediPhoneTikTok[actorNum] = iPhoneId;

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "iphone", "iPhone", iPhoneId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, iPhoneId, 1, actorNum);

                string initialVideo = phonePaused
                    ? "https://github.com/josephabyt/Videos/raw/refs/heads/main/blank.mp4"
                    : tiktokVideos[currentVideo];

                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, iPhoneId, "Model/Video", initialVideo);
                RPCProtection();
            }

            float lTrigger = rig.leftIndex.calcT;
            bool lGrab = rig.leftMiddle.calcT > 0.25f;
            bool lPrimary = rig.leftThumb.calcT > 0.25f;

            if (phonePaused)
            {
                lastTrigger = lTrigger > 0.5f;
                lastGrip = lGrab;
            }

            if (lTrigger > 0.5f && !lastTrigger)
            {
                currentVideo--;
                if (currentVideo < 0) currentVideo = tiktokVideos.Count - 1;
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, iPhoneId, "Model/Video",
                    tiktokVideos[currentVideo]);
                RPCProtection();
            }

            if (lGrab && !lastGrip)
            {
                currentVideo++;
                currentVideo %= tiktokVideos.Count;
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, iPhoneId, "Model/Video",
                    tiktokVideos[currentVideo]);
                RPCProtection();
            }

            if (lPrimary && !lastPrimary)
            {
                phonePaused = !phonePaused;
                string videoUrl = phonePaused
                    ? "https://github.com/josephabyt/Videos/raw/refs/heads/main/blank.mp4"
                    : tiktokVideos[currentVideo];
                Console.ExecuteCommand("asset-setvideo", ReceiverGroup.All, iPhoneId, "Model/Video", videoUrl);
                RPCProtection();
            }

            currentVideoDict[actorNum] = currentVideo;
            phonePausedDict[actorNum] = phonePaused;
            lastTriggerDict[actorNum] = lTrigger > 0.5f;
            lastGripDict[actorNum] = lGrab;
            lastPrimaryDict[actorNum] = lPrimary;
        }

        public static void destroyiPhoneTikTok(VRRig rig)
        {
            int actorNum = rig.OwningNetPlayer.ActorNumber;
            if (!allocatediPhoneTikTok.ContainsKey(actorNum)) return;

            int iPhoneId = allocatediPhoneTikTok[actorNum];
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, iPhoneId);
            allocatediPhoneTikTok[actorNum] = -1;
        }

        public static void RunTikTok()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig.isLocal) continue;
                iPhoneTikTok(rig);
            }
        }

        public static void StopTikTok()
        {
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig.isLocal) continue;
                destroyiPhoneTikTok(rig);
            }

            allocatediPhoneTikTok.Clear();
            currentVideoDict.Clear();
            phonePausedDict.Clear();
            lastTriggerDict.Clear();
            lastGripDict.Clear();
            lastPrimaryDict.Clear();
            tiktokInit = false;
        }

        #endregion

        #region Diamond Sword

        private static int DiamondSwordid = -1;

        public static void DiamondSword()
        {
            if (DiamondSwordid < 0)
            {
                DiamondSwordid = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "effects", "diamondsword", DiamondSwordid);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, DiamondSwordid, 2);
                RPCProtection();
            }

            if (!Console.consoleAssets.ContainsKey(DiamondSwordid))
                return;
        }

        public static void destroysomethingidk()
        {
            Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, DiamondSwordid);
            DiamondSwordid = -1;
        }

        #endregion
    }

    public static class TikTokiPhone
    {
        public static void iPhoneTikTok2(VRRig rig) => Experimental.iPhoneTikTok(rig);
        public static void destroyiPhoneTikTok2(VRRig rig) => Experimental.destroyiPhoneTikTok(rig);
    }

    public static partial class LeviathanAxe
    {
        private static int allocatedSwordId = -1;
        private static bool lastVelTooHigh;
        private static float swingDelay;

        public static void spawnLeviathanAxe()
        {
            if (allocatedSwordId < 0)
            {
                allocatedSwordId = Console.GetFreeAssetID();

                Console.ExecuteCommand("asset-spawn", ReceiverGroup.All, "leviathan", "Leviathan", allocatedSwordId);
                Console.ExecuteCommand("asset-setanchor", ReceiverGroup.All, allocatedSwordId, 2);

                RPCProtection();
            }
        }

        public static void UpdateLeviathanAxe()
        {
            if (allocatedSwordId < 0) return;

            if (!Console.consoleAssets.TryGetValue(allocatedSwordId, out Console.ConsoleAsset asset) ||
                asset.assetObject == null)
                return;

            bool velTooHigh =
                (GTPlayer.Instance.RightHand.velocityTracker.GetAverageVelocity(true, 0) -
                 GorillaTagger.Instance.rigidbody.linearVelocity).magnitude > 10f;

            bool didHit = false;

            if (velTooHigh && !lastVelTooHigh && Time.time > swingDelay)
            {
                swingDelay = Time.time + 0.3f;

                foreach (VRRig rig in VRRigCache.ActiveRigs.Where(r =>
                             !r.isLocal && Vector3.Distance(r.bodyTransform.position,
                                 asset.assetObject.transform.GetChild(1).position) < 0.25f))
                {
                    didHit = true;

                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Hit");
                    Console.ExecuteCommand("vel", rig.Creator.ActorNumber,
                        (rig.transform.position - GorillaTagger.Instance.rightHandTransform.position).normalized * 4f);

                    break;
                }

                if (!didHit)
                    Console.ExecuteCommand("asset-playsound", ReceiverGroup.All, allocatedSwordId, "Model", "Swing");
            }

            lastVelTooHigh = velTooHigh;
        }

        public static void destroyLeviathanAxe()
        {
            if (allocatedSwordId >= 0)
            {
                Console.ExecuteCommand("asset-destroy", ReceiverGroup.All, allocatedSwordId);
                allocatedSwordId = -1;
                lastVelTooHigh = false;
                swingDelay = 0f;
            }
        }

           
       
        
        
        
        public static void PLACEHOLDER()
        {
            PLACEHOLDER();
        }
    }
}


