using ExitGames.Client.Photon;
using GorillaLocomotion;
using GorillaNetworking;
using Photon.Pun;
using Photon.Realtime;
using Seralyth.Classes.Menu;
using Seralyth.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Seralyth.Managers
{
    public class NetworkMenuManager : MonoBehaviour
    {
        public static NetworkMenuManager instance;
        public const byte NetworkMenuByte = 71;
        private const string CustomPropertyKey = "Seralyth NetworkMenu";

        private static float syncTimer;
        private static float heartbeatTimer;
        private static float themeTimer;
        private static float gunSyncTimer;

        private static readonly Dictionary<int, RemoteMenuState> remoteMenus = new Dictionary<int, RemoteMenuState>();
        private static readonly Dictionary<int, CachedTheme> cachedThemes = new Dictionary<int, CachedTheme>();

        public class CachedTheme
        {
            public ExtGradient menuBgGradient;
            public ExtGradient btnGradient0;
            public ExtGradient btnGradient1;
            public ExtGradient txtGradient0;
            public ExtGradient txtGradient1;
            public ExtGradient txtGradient2;
            public Color playerColor;
            public bool thinMenu;
            public bool swapButtonColors;
            public bool slowFadeColors;
        }

        public class RemoteMenuState
        {
            public Player player;
            public GameObject displayObject;
            public string category = "Main";
            public int page;
            public Vector3 position;
            public Quaternion rotation;
            public Dictionary<string, bool> buttonStates = new Dictionary<string, bool>();
            public float lastStateTime;
            public bool closing;
            public VRRig cachedRig;
            public Vector3 rigOffset;
            public Color menuBgColor;
            public Color btnColor0;
            public Color btnColor1;
            public Color textColor0;
            public Color textColor1;
            public Color textColor2;
            public Color playerColor;
            public bool thinMenu;
            public bool swapButtonColors;
            public ExtGradient menuBgGradient;
            public ExtGradient btnGradient0;
            public ExtGradient btnGradient1;
            public ExtGradient txtGradient0;
            public ExtGradient txtGradient1;
            public ExtGradient txtGradient2;
            public bool slowFadeColors;

            public void RefreshGradientColors()
            {
                float t = (Time.time / (slowFadeColors ? 10f : 2f)) % 1f;
                if (menuBgGradient != null) menuBgColor = menuBgGradient.GetColorTime(t);
                if (btnGradient0 != null) btnColor0 = btnGradient0.GetColorTime(t);
                if (btnGradient1 != null) btnColor1 = btnGradient1.GetColorTime(t);
                if (txtGradient0 != null) textColor0 = txtGradient0.GetColorTime(t);
                if (txtGradient1 != null) textColor1 = txtGradient1.GetColorTime(t);
                if (txtGradient2 != null) textColor2 = txtGradient2.GetColorTime(t);
            }
        }

        private void Awake()
        {
            instance = this;
            PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
        }

        private void OnDestroy()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
        }

        public static void EnableNetworkMenu()
        {
            var props = new ExitGames.Client.Photon.Hashtable { { CustomPropertyKey, true } };
            if (PhotonNetwork.LocalPlayer != null)
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            SendThemeState();
        }

        public static void DisableNetworkMenu()
        {
            SendMenuClose();
            var props = new ExitGames.Client.Photon.Hashtable { { CustomPropertyKey, false } };
            if (PhotonNetwork.LocalPlayer != null)
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }


        private static string GetCurrentCategoryName()
        {
            return Buttons.CurrentCategoryName;
        }

        private static int GetCurrentPage()
        {
            return Main.pageNumber;
        }

        private static Vector3 GetMenuPosition()
        {
            if (Main.menu != null)
                return Main.menu.transform.position;
            if (Main.rightHand)
                return GorillaTagger.Instance.rightHandTransform.position;
            return GorillaTagger.Instance.leftHandTransform.position;
        }

        private static Quaternion GetMenuRotation()
        {
            if (Main.menu != null)
                return Main.menu.transform.rotation;
            if (Main.rightHand)
                return GorillaTagger.Instance.rightHandTransform.rotation;
            return GorillaTagger.Instance.leftHandTransform.rotation;
        }

        private static int ColorToPacked(Color c)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            return (r << 16) | (g << 8) | b;
        }

        private static Color PackedToColor(int packed)
        {
            float r = ((packed >> 16) & 0xFF) / 255f;
            float g = ((packed >> 8) & 0xFF) / 255f;
            float b = (packed & 0xFF) / 255f;
            return new Color(r, g, b);
        }

        private static void SerializeGradient(object[] args, ref int idx, ExtGradient grad)
        {
            int flags = 0;
            if (grad.rainbow) flags |= 1;
            if (grad.pastelRainbow) flags |= 2;
            if (grad.epileptic) flags |= 4;
            if (grad.copyRigColor) flags |= 8;
            if (grad.transparent) flags |= 16;

            GradientColorKey[] keys = grad.colors ?? ExtGradient.GetSolidGradient(Color.magenta);
            int keyCount = Mathf.Clamp(keys.Length, 2, 3);
            flags |= keyCount << 5;

            args[idx++] = flags;
            for (int i = 0; i < keyCount; i++)
            {
                args[idx++] = ColorToPacked(keys[i].color);
                args[idx++] = keys[i].time;
            }
            if (keyCount < 3)
            {
                Color lastColor = keys[keyCount - 1].color;
                float lastTime = keys[keyCount - 1].time;
                for (int i = keyCount; i < 3; i++)
                {
                    args[idx++] = ColorToPacked(lastColor);
                    args[idx++] = lastTime;
                }
            }
        }

        private static ExtGradient DeserializeGradient(object[] args, ref int idx)
        {
            int flags = Convert.ToInt32(args[idx++]);
            bool rainbow = (flags & 1) != 0;
            bool pastelRainbow = (flags & 2) != 0;
            bool epileptic = (flags & 4) != 0;
            bool copyRigColor = (flags & 8) != 0;
            bool transparent = (flags & 16) != 0;
            int keyCount = (flags >> 5) & 3;
            if (keyCount < 2) keyCount = 2;

            GradientColorKey[] keys = new GradientColorKey[keyCount];
            float[] times = keyCount == 2 ? new float[] { 0f, 1f } : new float[] { 0f, 0.5f, 1f };

            for (int i = 0; i < keyCount; i++)
            {
                Color c = PackedToColor(Convert.ToInt32(args[idx++]));
                float t = Convert.ToSingle(args[idx++]);
                keys[i] = new GradientColorKey(c, times[i]);
            }
            idx += (3 - keyCount) * 2;

            return new ExtGradient
            {
                colors = keys,
                rainbow = rainbow,
                pastelRainbow = pastelRainbow,
                epileptic = epileptic,
                copyRigColor = copyRigColor,
                transparent = transparent
            };
        }

        public static void SendMenuState()
        {
            if (!Main.networkMenuEnabled || !PhotonNetwork.InRoom)
                return;

            string categoryName = GetCurrentCategoryName();
            int page = GetCurrentPage();
            Vector3 menuPosition;
            if (VRRig.LocalRig != null)
                menuPosition = GetMenuPosition() - VRRig.LocalRig.transform.position;
            else
                menuPosition = GetMenuPosition();
            Quaternion menuRotation = GetMenuRotation();

            List<string> enabledNames = new List<string>();
            foreach (ButtonInfo[] category in Buttons.buttons)
            {
                if (category == null) continue;
                foreach (ButtonInfo button in category)
                {
                    if (button.isTogglable && button.enabled)
                        enabledNames.Add(button.buttonText);
                }
            }

            var stateArgs = new object[]
            {
                "seralyth_netmenu_state",
                categoryName,
                page,
                menuPosition.x, menuPosition.y, menuPosition.z,
                menuRotation.x, menuRotation.y, menuRotation.z, menuRotation.w,
                enabledNames.ToArray()
            };

            PhotonNetwork.RaiseEvent(NetworkMenuByte, stateArgs, new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others
            }, SendOptions.SendReliable);
        }

        private static void SendThemeState()
        {
            if (!PhotonNetwork.InRoom || !Main.networkMenuEnabled) return;

            Color playerColor = Color.white;
            if (VRRig.LocalRig != null)
                playerColor = VRRig.LocalRig.playerColor;

            int themeFlags = (Main.thinMenu ? 1 : 0) | (Main.swapButtonColors ? 2 : 0) | (Main.slowFadeColors ? 4 : 0);

            var themeArgs = new object[51];
            themeArgs[0] = "seralyth_netmenu_theme_v2";
            themeArgs[1] = themeFlags;
            themeArgs[2] = ColorToPacked(playerColor);

            int idx = 3;
            SerializeGradient(themeArgs, ref idx, Main.menuBackgroundColor);
            SerializeGradient(themeArgs, ref idx, Main.buttonColors[0]);
            SerializeGradient(themeArgs, ref idx, Main.buttonColors[1]);
            SerializeGradient(themeArgs, ref idx, Main.textColors[0]);
            SerializeGradient(themeArgs, ref idx, Main.textColors[1]);
            SerializeGradient(themeArgs, ref idx, Main.textColors[2]);

            PhotonNetwork.RaiseEvent(NetworkMenuByte, themeArgs, new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others
            }, SendOptions.SendReliable);
        }

        private static void SendMenuClose()
        {
            if (Main.networkMenuEnabled && PhotonNetwork.InRoom)
            {
                var closeArgs = new object[] { "seralyth_netmenu_close" };
                PhotonNetwork.RaiseEvent(NetworkMenuByte, closeArgs, new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others
                }, SendOptions.SendReliable);
            }
        }

        private static void SendMenuHeartbeat()
        {
            if (Main.networkMenuEnabled && PhotonNetwork.InRoom)
            {
                var hbArgs = new object[] { "seralyth_netmenu_heartbeat" };
                PhotonNetwork.RaiseEvent(NetworkMenuByte, hbArgs, new RaiseEventOptions
                {
                    Receivers = ReceiverGroup.Others
                }, SendOptions.SendUnreliable);
            }
        }

        private void OnEventReceived(EventData data)
        {
            if (data.Code != NetworkMenuByte) return;

            try
            {
                Player sender = PhotonNetwork.CurrentRoom?.GetPlayer(data.Sender);
                if (sender == null || sender == PhotonNetwork.LocalPlayer) return;

                object[] args = data.CustomData as object[];
                if (args == null || args.Length < 1) return;

                string command = args[0] as string;
                if (string.IsNullOrEmpty(command)) return;

                switch (command)
                {
                    case "seralyth_netmenu_state":
                        HandleRemoteMenuState(sender, args);
                        break;
                    case "seralyth_netmenu_close":
                        HandleRemoteMenuClose(sender);
                        break;
                    case "seralyth_netmenu_heartbeat":
                        HandleRemoteHeartbeat(sender);
                        break;
                    case "seralyth_netmenu_theme_v2":
                        HandleRemoteThemeV2(sender, args);
                        break;
                    case "seralyth_netmenu_theme":
                        HandleRemoteThemeLegacy(sender, args);
                        break;
                    case "seralyth_netmenu_gundata":
                        GunLib.HandleRemoteGunData(sender, args);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private static void HandleRemoteMenuState(Player sender, object[] args)
        {
            if (args.Length < 11) return;

            string category = args[1] as string ?? "Main";
            int page = Convert.ToInt32(args[2]);
            if (page < 0 || page > 100) return;
            if (!(args[3] is float posX)) posX = 0f;
            if (!(args[4] is float posY)) posY = 0f;
            if (!(args[5] is float posZ)) posZ = 0f;
            if (float.IsNaN(posX) || float.IsInfinity(posX)) return;
            if (float.IsNaN(posY) || float.IsInfinity(posY)) return;
            if (float.IsNaN(posZ) || float.IsInfinity(posZ)) return;
            Vector3 pos = new Vector3(posX, posY, posZ);

            Quaternion rot = Quaternion.identity;
            float rx = 0f, ry = 0f, rz = 0f, rw = 1f;
            if (args[6] is float frx) rx = frx;
            if (args[7] is float fry) ry = fry;
            if (args[8] is float frz) rz = frz;
            if (args[9] is float frw) rw = frw;
            if (float.IsNaN(rx) || float.IsInfinity(rx)) return;
            rot = new Quaternion(rx, ry, rz, rw);

            HashSet<string> enabledSet = new HashSet<string>();
            string[] enabledArr = args[10] as string[];
            if (enabledArr != null)
            {
                foreach (string name in enabledArr)
                {
                    if (!string.IsNullOrEmpty(name))
                        enabledSet.Add(name);
                }
            }

            Color defBg = new Color(0.086f, 0.086f, 0.086f, 0.5f);
            Color defBtn0 = new Color(0.463f, 0.024f, 0.988f, 1f);
            Color defBtn1 = new Color(0.345f, 0.024f, 0.729f, 1f);
            Color defText = Color.white;

            Color menuBgColor = defBg;
            Color btnColor0 = defBtn0;
            Color btnColor1 = defBtn1;
            Color textColor0 = defText;
            Color textColor1 = defText;
            Color textColor2 = defText;
            bool thinMenu = true;
            bool swapButtonColors = false;

            if (cachedThemes.TryGetValue(sender.ActorNumber, out CachedTheme cached))
            {
                menuBgColor = cached.menuBgGradient != null
                    ? cached.menuBgGradient.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defBg;
                btnColor0 = cached.btnGradient0 != null
                    ? cached.btnGradient0.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defBtn0;
                btnColor1 = cached.btnGradient1 != null
                    ? cached.btnGradient1.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defBtn1;
                textColor0 = cached.txtGradient0 != null
                    ? cached.txtGradient0.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defText;
                textColor1 = cached.txtGradient1 != null
                    ? cached.txtGradient1.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defText;
                textColor2 = cached.txtGradient2 != null
                    ? cached.txtGradient2.GetColorTime((Time.time / (cached.slowFadeColors ? 10f : 2f)) % 1f)
                    : defText;
                thinMenu = cached.thinMenu;
                swapButtonColors = cached.swapButtonColors;
            }

            var states = new Dictionary<string, bool>();
            foreach (ButtonInfo[] categoryButtons in Buttons.buttons)
            {
                if (categoryButtons == null) continue;
                foreach (ButtonInfo btn in categoryButtons)
                {
                    if (btn.isTogglable)
                        states[btn.buttonText] = enabledSet.Contains(btn.buttonText);
                }
            }

            int actorNumber = sender.ActorNumber;
            bool isNew = !remoteMenus.TryGetValue(actorNumber, out var state);
            if (isNew)
            {
                state = new RemoteMenuState { player = sender };
                remoteMenus[actorNumber] = state;
            }

            bool categoryChanged = !isNew && (state.page != page || state.category != category);

            state.category = category;
            state.page = page;
            VRRig rig = GorillaGameManager.StaticFindRigForPlayer(sender);
            state.cachedRig = rig;
            if (rig != null)
            {
                state.rigOffset = pos;
                state.position = rig.transform.position + pos;
            }
            else
            {
                state.position = pos;
            }
            state.rotation = rot;
            state.buttonStates = states;
            state.lastStateTime = Time.time;
            state.menuBgColor = menuBgColor;
            state.btnColor0 = btnColor0;
            state.btnColor1 = btnColor1;
            state.textColor0 = textColor0;
            state.textColor1 = textColor1;
            state.textColor2 = textColor2;
            state.thinMenu = thinMenu;
            state.swapButtonColors = swapButtonColors;
            if (cachedThemes.TryGetValue(sender.ActorNumber, out CachedTheme cachedForPlayer))
            {
                state.playerColor = cachedForPlayer.playerColor;
                state.menuBgGradient = cachedForPlayer.menuBgGradient;
                state.btnGradient0 = cachedForPlayer.btnGradient0;
                state.btnGradient1 = cachedForPlayer.btnGradient1;
                state.txtGradient0 = cachedForPlayer.txtGradient0;
                state.txtGradient1 = cachedForPlayer.txtGradient1;
                state.txtGradient2 = cachedForPlayer.txtGradient2;
                state.slowFadeColors = cachedForPlayer.slowFadeColors;
            }

            if (state.displayObject == null)
            {
                NetworkMenuDisplay.Create(state);
            }
            else if (categoryChanged)
            {
                NetworkMenuDisplay.UpdateState(state);
            }
            else
            {
                NetworkMenuDisplay.UpdateColors(state);
            }
            NetworkMenuDisplay.UpdatePosition(state);
        }

        private static void HandleRemoteMenuClose(Player sender)
        {
            int actorNumber = sender.ActorNumber;
            if (remoteMenus.TryGetValue(actorNumber, out var state) && state.displayObject != null && !state.closing)
            {
                NetworkMenuDisplay.CloseAndDestroy(state);
            }
        }

        private static void HandleRemoteHeartbeat(Player sender)
        {
            if (remoteMenus.TryGetValue(sender.ActorNumber, out var state))
            {
                state.lastStateTime = Time.time;
            }
        }

        private static void HandleRemoteThemeV2(Player sender, object[] args)
        {
            if (args.Length < 51) return;

            int themeFlags = Convert.ToInt32(args[1]);
            int packedPlayer = Convert.ToInt32(args[2]);

            int idx = 3;
            ExtGradient bgGrad = DeserializeGradient(args, ref idx);
            ExtGradient btn0Grad = DeserializeGradient(args, ref idx);
            ExtGradient btn1Grad = DeserializeGradient(args, ref idx);
            ExtGradient txt0Grad = DeserializeGradient(args, ref idx);
            ExtGradient txt1Grad = DeserializeGradient(args, ref idx);
            ExtGradient txt2Grad = DeserializeGradient(args, ref idx);

            cachedThemes[sender.ActorNumber] = new CachedTheme
            {
                menuBgGradient = bgGrad,
                btnGradient0 = btn0Grad,
                btnGradient1 = btn1Grad,
                txtGradient0 = txt0Grad,
                txtGradient1 = txt1Grad,
                txtGradient2 = txt2Grad,
                playerColor = PackedToColor(packedPlayer),
                thinMenu = (themeFlags & 1) != 0,
                swapButtonColors = (themeFlags & 2) != 0,
                slowFadeColors = (themeFlags & 4) != 0
            };

            if (remoteMenus.TryGetValue(sender.ActorNumber, out var state))
            {
                CachedTheme t = cachedThemes[sender.ActorNumber];
                state.playerColor = t.playerColor;
                state.thinMenu = t.thinMenu;
                state.swapButtonColors = t.swapButtonColors;
                state.slowFadeColors = t.slowFadeColors;
                state.menuBgGradient = t.menuBgGradient;
                state.btnGradient0 = t.btnGradient0;
                state.btnGradient1 = t.btnGradient1;
                state.txtGradient0 = t.txtGradient0;
                state.txtGradient1 = t.txtGradient1;
                state.txtGradient2 = t.txtGradient2;
                state.RefreshGradientColors();
                NetworkMenuDisplay.UpdateColors(state);
            }
        }

        private static void HandleRemoteThemeLegacy(Player sender, object[] args)
        {
            if (args.Length < 9) return;

            int packedBg = Convert.ToInt32(args[1]);
            int packedBtn0 = Convert.ToInt32(args[2]);
            int packedBtn1 = Convert.ToInt32(args[3]);
            int packedTxt0 = Convert.ToInt32(args[4]);
            int packedTxt1 = Convert.ToInt32(args[5]);
            int packedTxt2 = Convert.ToInt32(args[6]);
            int packedPlayer = Convert.ToInt32(args[7]);
            int flags = Convert.ToInt32(args[8]);

            Color bg = PackedToColor(packedBg);
            Color btn0 = PackedToColor(packedBtn0);
            Color btn1 = PackedToColor(packedBtn1);
            Color txt0 = PackedToColor(packedTxt0);
            Color txt1 = PackedToColor(packedTxt1);
            Color txt2 = PackedToColor(packedTxt2);

            cachedThemes[sender.ActorNumber] = new CachedTheme
            {
                menuBgGradient = new ExtGradient { colors = ExtGradient.GetSolidGradient(bg) },
                btnGradient0 = new ExtGradient { colors = ExtGradient.GetSolidGradient(btn0) },
                btnGradient1 = new ExtGradient { colors = ExtGradient.GetSolidGradient(btn1) },
                txtGradient0 = new ExtGradient { colors = ExtGradient.GetSolidGradient(txt0) },
                txtGradient1 = new ExtGradient { colors = ExtGradient.GetSolidGradient(txt1) },
                txtGradient2 = new ExtGradient { colors = ExtGradient.GetSolidGradient(txt2) },
                playerColor = PackedToColor(packedPlayer),
                thinMenu = (flags & 1) != 0,
                swapButtonColors = (flags & 2) != 0
            };

            if (remoteMenus.TryGetValue(sender.ActorNumber, out var state))
            {
                CachedTheme t = cachedThemes[sender.ActorNumber];
                state.playerColor = t.playerColor;
                state.thinMenu = t.thinMenu;
                state.swapButtonColors = t.swapButtonColors;
                state.menuBgGradient = t.menuBgGradient;
                state.btnGradient0 = t.btnGradient0;
                state.btnGradient1 = t.btnGradient1;
                state.txtGradient0 = t.txtGradient0;
                state.txtGradient1 = t.txtGradient1;
                state.txtGradient2 = t.txtGradient2;
                state.RefreshGradientColors();
                NetworkMenuDisplay.UpdateColors(state);
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            List<int> toRemove = null;
            foreach (var kvp in remoteMenus)
            {
                if (!kvp.Value.closing && (Time.time - kvp.Value.lastStateTime > 3.5f || PhotonNetwork.CurrentRoom?.GetPlayer(kvp.Key) == null))
                {
                    if (toRemove == null) toRemove = new List<int>();
                    toRemove.Add(kvp.Key);
                }
            }
            if (toRemove != null)
            {
                foreach (int key in toRemove)
                {
                    if (remoteMenus.TryGetValue(key, out var state))
                    {
                        if (state.displayObject != null && !state.closing)
                            NetworkMenuDisplay.CloseAndDestroy(state);
                        remoteMenus.Remove(key);
                        cachedThemes.Remove(key);
                    }
                }
            }

            if (Time.time - heartbeatTimer >= 3f)
            {
                heartbeatTimer = Time.time;
                SendMenuHeartbeat();
            }

            if (Time.time - themeTimer >= 2f)
            {
                themeTimer = Time.time;
                SendThemeState();
            }

            foreach (var kvp in remoteMenus)
            {
                RemoteMenuState state = kvp.Value;
                if (state.displayObject == null || state.closing) continue;

                state.RefreshGradientColors();
                NetworkMenuDisplay.UpdateColors(state);

                if (state.cachedRig == null)
                {
                    state.cachedRig = GorillaGameManager.StaticFindRigForPlayer(state.player);
                    if (state.cachedRig != null)
                        state.rigOffset = state.position - state.cachedRig.transform.position;
                }
                if (state.cachedRig != null)
                {
                    Vector3 newPos = state.cachedRig.transform.position + state.rigOffset;
                    if (state.position != newPos)
                    {
                        state.position = newPos;
                        NetworkMenuDisplay.UpdatePosition(state);
                    }
                }
            }

            if (Main.menu != null)
            {
                if (Time.time - syncTimer >= 0.033f)
                {
                    syncTimer = Time.time;
                    SendMenuState();
                }
            }

            if (Time.time - gunSyncTimer >= 0.033f)
            {
                gunSyncTimer = Time.time;
                GunLib.SendGunData();
            }

            GunLib.UpdateRemoteGunPointers();

            Main.GunActiveThisFrame = false;
        }

        public static void SyncOnJoin()
        {
            if (Main.networkMenuEnabled && PhotonNetwork.InRoom && Main.menu != null)
            {
                SendThemeState();
                instance.StartCoroutine(DelayedSync());
            }
        }

        private static IEnumerator DelayedSync()
        {
            yield return new WaitForSeconds(1f);
            if (Main.networkMenuEnabled && PhotonNetwork.InRoom && Main.menu != null)
            {
                SendMenuState();
            }
        }

        public static void RemoveRemoteMenu(Player player)
        {
            RemoveRemoteMenu(player.ActorNumber);
        }

        public static void RemoveRemoteMenu(int actorNumber)
        {
            if (remoteMenus.TryGetValue(actorNumber, out var state))
            {
                if (state.displayObject != null && !state.closing)
                    NetworkMenuDisplay.CloseAndDestroy(state);
                remoteMenus.Remove(actorNumber);
                cachedThemes.Remove(actorNumber);
            }
            if (GunLib.remoteGunPointers.TryGetValue(actorNumber, out var ptr))
            {
                if (ptr.pointer != null) UnityEngine.Object.Destroy(ptr.pointer);
                if (ptr.line != null) UnityEngine.Object.Destroy(ptr.line.gameObject);
                GunLib.remoteGunPointers.Remove(actorNumber);
            }
        }

        public static void ClearAllRemoteMenus()
        {
            foreach (var kvp in remoteMenus)
            {
                if (kvp.Value.displayObject != null && !kvp.Value.closing)
                    NetworkMenuDisplay.CloseAndDestroy(kvp.Value);
            }
            remoteMenus.Clear();
            cachedThemes.Clear();
            GunLib.ClearRemoteGunPointers();
        }
    }
}
