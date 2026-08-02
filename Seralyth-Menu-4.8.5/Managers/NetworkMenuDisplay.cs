using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Seralyth.Classes.Menu;
using Seralyth.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Seralyth.Managers
{
    internal static class NetworkMenuDisplay
    {
        private static Shader _cachedShader;
        private static Shader CachedShader
        {
            get
            {
                if (_cachedShader == null)
                    _cachedShader = Shader.Find("Universal Render Pipeline/Unlit");
                return _cachedShader;
            }
        }

        private static readonly Color EnabledGreen = new Color(0.2f, 0.85f, 0.3f);
        private static readonly Color DisabledRed = new Color(0.85f, 0.25f, 0.25f);

        private static void SetColor(Renderer r, Color c)
        {
            r.material.SetColor("_BaseColor", c);
        }

        private static Material MakeMaterial(Color c)
        {
            Material m = new Material(CachedShader);
            m.SetColor("_BaseColor", c);
            return m;
        }

        public static void Create(NetworkMenuManager.RemoteMenuState state)
        {
            if (state.displayObject != null) return;

            bool thin = state.thinMenu;

            GameObject root = new GameObject("SeralythNetMenu_" + state.player.ActorNumber);
            root.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);

            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(bg.GetComponent<BoxCollider>());
            bg.transform.parent = root.transform;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = thin ? new Vector3(0.1f, 1f, 1f) : new Vector3(0.1f, 1.5f, 1f);
            bg.transform.localPosition = new Vector3(0.50f, 0f, 0f);
            bg.name = "seralyth_bg";

            Renderer bgRenderer = bg.GetComponent<Renderer>();
            bgRenderer.material = MakeMaterial(state.menuBgColor);

            GameObject canvasObj = new GameObject("Canvas");
            canvasObj.transform.parent = root.transform;
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            scaler.dynamicPixelsPerUnit = 2500f;
            scaler.referencePixelsPerUnit = 100f;

            string menuName = Main.doCustomName ? Main.customMenuName : "Seralyth Remake";
            string playerName = string.IsNullOrEmpty(state.player.NickName) ? "Unknown" : state.player.NickName;

            CreateTMPText(canvasObj.transform, "MenuTitle", menuName, state.textColor0,
                TextAlignmentOptions.Center, new Vector2(0.28f, 0.05f),
                new Vector3(0.06f, 0f, 0.165f), Quaternion.Euler(180f, 90f, 90f));

            string playerDisplay = "<b>" + playerName + "</b>";
            playerDisplay += $" <color=grey>[</color><color=white>{state.page + 1}</color><color=grey>]</color>";

            CreateTMPText(canvasObj.transform, "PlayerName", playerDisplay, state.textColor0,
                TextAlignmentOptions.Center, new Vector2(0.28f, 0.02f),
                new Vector3(0.06f, 0f, 0.135f), Quaternion.Euler(180f, 90f, 90f));

            string categoryName = state.category;
            if (categoryName != "Main")
            {
                CreateTMPText(canvasObj.transform, "CategoryLabel", $"[{categoryName}]", state.textColor1,
                    TextAlignmentOptions.Center, new Vector2(0.28f, 0.02f),
                    new Vector3(0.06f, 0f, 0.105f), Quaternion.Euler(180f, 90f, 90f));
            }

            int enabledCount = 0;
            foreach (var kv in state.buttonStates)
            {
                if (kv.Value) enabledCount++;
            }
            CreateTMPText(canvasObj.transform, "EnabledCount", enabledCount + " enabled", EnabledGreen,
                TextAlignmentOptions.Center, new Vector2(0.28f, 0.02f),
                new Vector3(0.06f, 0f, 0.075f), Quaternion.Euler(180f, 90f, 90f));

            float disconnectOffset = -0.3f;
            float disconnectZ = 0.28f - disconnectOffset;
            Color dcBtnColor = state.swapButtonColors ? state.btnColor1 : state.btnColor0;

            GameObject dcBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(dcBtn.GetComponent<Rigidbody>());
            dcBtn.GetComponent<BoxCollider>().isTrigger = true;
            dcBtn.transform.parent = root.transform;
            dcBtn.transform.localRotation = Quaternion.identity;
            dcBtn.transform.localScale = thin ? new Vector3(0.09f, 0.9f, Main.ButtonDistance * 0.8f) : new Vector3(0.09f, 1.3f, Main.ButtonDistance * 0.8f);
            dcBtn.transform.localPosition = new Vector3(0.56f, 0f, disconnectZ);
            dcBtn.name = "Disconnect";
            dcBtn.GetComponent<Renderer>().material = MakeMaterial(dcBtnColor);
            dcBtn.AddComponent<NetworkMenuBtnCollider>().relatedText = "Disconnect";
            CreateTMPText(canvasObj.transform, "DisconnectText", "Disconnect", state.textColor1,
                TextAlignmentOptions.Center, new Vector2(0.2f, 0.03f * (Main.ButtonDistance / 0.1f)),
                new Vector3(0.064f, 0f, 0.111f - disconnectOffset / 2.6f),
                Quaternion.Euler(180f, 90f, 90f));

            Color navBtnColor = state.swapButtonColors ? state.btnColor1 : state.btnColor0;
            CreateNavButton(root, canvasObj.transform, "PreviousPage", "<", state.textColor1,
                new Vector3(0.09f, 0.2f, 0.9f),
                new Vector3(0.56f, thin ? 0.65f : 0.9f, 0f),
                new Vector3(0.064f, thin ? 0.195f : 0.267f, 0f),
                navBtnColor);
            CreateNavButton(root, canvasObj.transform, "NextPage", ">", state.textColor1,
                new Vector3(0.09f, 0.2f, 0.9f),
                new Vector3(0.56f, thin ? -0.65f : -0.9f, 0f),
                new Vector3(0.064f, thin ? -0.195f : -0.267f, 0f),
                navBtnColor);

            BuildButtonPage(state, root, canvasObj, thin);

            state.displayObject = root;
            UpdatePosition(state);

            if (Main.dynamicAnimations)
                ((MonoBehaviour)NetworkMenuManager.instance).StartCoroutine(OpenAnimation(state));
            else
                root.transform.localScale = new Vector3(0.1f, 0.3f, 0.3825f);
        }

        private static void CreateTMPText(Transform parent, string name, string text, Color color,
            TextAlignmentOptions alignment, Vector2 sizeDelta, Vector3 localPos, Quaternion localRot)
        {
            GameObject obj = new GameObject(name);
            obj.transform.parent = parent;
            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.font = Main.activeFont;
            tmp.text = text;
            tmp.fontSize = 1;
            tmp.color = color;
            tmp.fontStyle = Main.activeFontStyle;
            tmp.alignment = alignment;
            tmp.richText = true;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.localPosition = Vector3.zero;
            rt.sizeDelta = sizeDelta;
            rt.localPosition = localPos;
            rt.localRotation = localRot;
        }

        private static void CreateNavButton(GameObject root, Transform canvasParent, string name, string label,
            Color textColor, Vector3 btnScale, Vector3 btnPos, Vector3 textPos, Color btnColor)
        {
            GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(btn.GetComponent<Rigidbody>());
            btn.GetComponent<BoxCollider>().isTrigger = true;
            btn.transform.parent = root.transform;
            btn.transform.localRotation = Quaternion.identity;
            btn.transform.localScale = btnScale;
            btn.transform.localPosition = btnPos;
            btn.name = name;

            Renderer r = btn.GetComponent<Renderer>();
                r.material = MakeMaterial(btnColor);

            btn.AddComponent<NetworkMenuBtnCollider>().relatedText = name;

            CreateTMPText(canvasParent, name + "Text", label, textColor, TextAlignmentOptions.Center,
                new Vector2(0.2f, 0.03f), textPos,
                Quaternion.Euler(180f, 90f, 90f));
        }

        private static void BuildButtonPage(NetworkMenuManager.RemoteMenuState state, GameObject root, GameObject canvasObj, bool thin)
        {
            List<ButtonInfo> categoryButtons = GetCategoryButtons(state.category, state.buttonStates);
            int pageSize = Main.PageSize;
            int startIdx = state.page * pageSize;
            int count = Mathf.Min(pageSize, categoryButtons.Count - startIdx);
            float buttonDistance = Main.ButtonDistance;
            float incOffset = thin ? 0.399f : 0.599f;
            float incTextOffset = thin ? 0.12f : 0.18f;

            for (int i = 0; i < count; i++)
            {
                float offset = (i) * buttonDistance;
                ButtonInfo info = categoryButtons[startIdx + i];
                string btnName = info.buttonText;
                string displayName = !string.IsNullOrEmpty(info.overlapText) ? info.overlapText : btnName;
                bool isLabel = info.label;
                bool isTogglable = info.isTogglable;
                bool isIncremental = info.incremental;
                bool isDetected = info.detected;
                bool isEnabled = isTogglable && state.buttonStates.TryGetValue(btnName, out bool val) && val;

                if (isDetected)
                    displayName = "<color=red>" + displayName + "</color>";

                if (isLabel)
                {
                    CreateTMPText(canvasObj.transform, "btn_" + i, displayName, state.textColor0,
                        TextAlignmentOptions.Center, new Vector2(0.2f, 0.03f * (buttonDistance / 0.1f)),
                        new Vector3(0.064f, 0f, 0.111f - offset / 2.6f),
                        Quaternion.Euler(180f, 90f, 90f));
                    continue;
                }

                Vector3 mainBtnScale;
                if (isIncremental)
                    mainBtnScale = thin ? new Vector3(0.09f, 0.646f, buttonDistance * 0.8f) : new Vector3(0.09f, 1.046f, buttonDistance * 0.8f);
                else
                    mainBtnScale = thin ? new Vector3(0.09f, 0.9f, buttonDistance * 0.8f) : new Vector3(0.09f, 1.3f, buttonDistance * 0.8f);

                GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(btn.GetComponent<Rigidbody>());
                btn.GetComponent<BoxCollider>().isTrigger = true;
                btn.transform.parent = root.transform;
                btn.transform.localRotation = Quaternion.identity;
                btn.transform.localScale = mainBtnScale;
                btn.transform.localPosition = new Vector3(0.56f, 0f, 0.28f - offset);

                Renderer r = btn.GetComponent<Renderer>();
                Color btnColor;
                if (isTogglable)
                    btnColor = isEnabled ? state.btnColor1 : state.btnColor0;
                else
                    btnColor = state.btnColor0;
            r.material = MakeMaterial(btnColor);
                btn.AddComponent<NetworkMenuBtnCollider>().relatedText = btnName;

                if (isIncremental)
                {
                    Color incBtnColor = state.swapButtonColors ? state.btnColor1 : state.btnColor0;

                    GameObject minusBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(minusBtn.GetComponent<Rigidbody>());
                    minusBtn.GetComponent<BoxCollider>().isTrigger = true;
                    minusBtn.transform.parent = root.transform;
                    minusBtn.transform.localRotation = Quaternion.identity;
                    minusBtn.transform.localScale = new Vector3(0.09f, 0.102f, buttonDistance * 0.8f);
                    minusBtn.transform.localPosition = new Vector3(0.56f, incOffset, 0.28f - offset);
                    minusBtn.GetComponent<Renderer>().material = MakeMaterial(incBtnColor);
                    minusBtn.AddComponent<NetworkMenuBtnCollider>().relatedText = "Minus_" + btnName;

                    CreateTMPText(canvasObj.transform, "inc_minus_" + i, "-", state.textColor1,
                        TextAlignmentOptions.Center, new Vector2(0.2f, 0.03f * (buttonDistance / 0.1f)),
                        new Vector3(0.064f, incTextOffset, 0.111f - offset / 2.6f),
                        Quaternion.Euler(180f, 90f, 90f));

                    GameObject plusBtn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(plusBtn.GetComponent<Rigidbody>());
                    plusBtn.GetComponent<BoxCollider>().isTrigger = true;
                    plusBtn.transform.parent = root.transform;
                    plusBtn.transform.localRotation = Quaternion.identity;
                    plusBtn.transform.localScale = new Vector3(0.09f, 0.102f, buttonDistance * 0.8f);
                    plusBtn.transform.localPosition = new Vector3(0.56f, -incOffset, 0.28f - offset);
                    plusBtn.GetComponent<Renderer>().material = MakeMaterial(incBtnColor);
                    plusBtn.AddComponent<NetworkMenuBtnCollider>().relatedText = "Plus_" + btnName;

                    CreateTMPText(canvasObj.transform, "inc_plus_" + i, "+", state.textColor1,
                        TextAlignmentOptions.Center, new Vector2(0.2f, 0.03f * (buttonDistance / 0.1f)),
                        new Vector3(0.064f, -incTextOffset, 0.111f - offset / 2.6f),
                        Quaternion.Euler(180f, 90f, 90f));
                }

                Color textColor;
                if (isTogglable)
                    textColor = isEnabled ? state.textColor2 : state.textColor1;
                else
                    textColor = state.textColor1;
                float textWidth = isIncremental ? 0.18f : 0.2f;
                CreateTMPText(canvasObj.transform, "btn_" + i, displayName, textColor,
                    TextAlignmentOptions.Center, new Vector2(textWidth, 0.03f * (buttonDistance / 0.1f)),
                    new Vector3(0.064f, 0f, 0.111f - offset / 2.6f),
                    Quaternion.Euler(180f, 90f, 90f));
            }
        }

        private static List<ButtonInfo> GetCategoryButtons(string categoryName, Dictionary<string, bool> buttonStates = null)
        {
            if (categoryName == "Enabled Mods" && buttonStates != null)
            {
                List<ButtonInfo> enabledMods = new List<ButtonInfo>();
                foreach (ButtonInfo[] category in Buttons.buttons)
                {
                    if (category == null) continue;
                    foreach (ButtonInfo btn in category)
                    {
                        if (btn.isTogglable && buttonStates.TryGetValue(btn.buttonText, out bool val) && val)
                            enabledMods.Add(btn);
                    }
                }
                enabledMods = enabledMods.OrderBy(v => v.buttonText).ToList();
                ButtonInfo exitBtn = null;
                int exitCatIdx = Buttons.GetCategory("Enabled Mods");
                if (exitCatIdx >= 0 && exitCatIdx < Buttons.buttons.Length)
                {
                    foreach (ButtonInfo btn in Buttons.buttons[exitCatIdx])
                    {
                        if (btn.buttonText == "Exit Enabled Mods")
                        {
                            exitBtn = btn;
                            break;
                        }
                    }
                }
                if (exitBtn != null)
                    enabledMods.Insert(0, exitBtn);
                return enabledMods;
            }

            int catIdx = Buttons.GetCategory(categoryName);
            if (catIdx < 0 || catIdx >= Buttons.buttons.Length)
                return new List<ButtonInfo>();

            return new List<ButtonInfo>(Buttons.buttons[catIdx]);
        }

        private static IEnumerator OpenAnimation(NetworkMenuManager.RemoteMenuState state)
        {
            GameObject root = state.displayObject;
            if (root == null) yield break;

            float elapsed = 0f;
            Vector3 startScale = root.transform.localScale;
            Vector3 targetScale = new Vector3(0.1f, 0.3f, 0.3825f);

            while (elapsed < 0.3f)
            {
                if (root == null || state.closing) yield break;
                float t = elapsed / 0.3f;
                float s = 1.70158f;
                t -= 1f;
                float bounce = t * t * ((s + 1f) * t + s) + 1f;
                root.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, bounce);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (root != null)
                root.transform.localScale = targetScale;
        }

        public static void CloseAndDestroy(NetworkMenuManager.RemoteMenuState state)
        {
            if (state.displayObject == null || state.closing) return;
            state.closing = true;
            if (Main.dynamicAnimations)
                ((MonoBehaviour)NetworkMenuManager.instance).StartCoroutine(CloseAnimation(state));
            else
            {
                Object.Destroy(state.displayObject);
                state.displayObject = null;
                NetworkMenuManager.RemoveRemoteMenu(state.player);
            }
        }

        private static IEnumerator CloseAnimation(NetworkMenuManager.RemoteMenuState state)
        {
            GameObject root = state.displayObject;
            if (root == null) yield break;

            float elapsed = 0f;
            Vector3 startScale = root.transform.localScale;

            while (elapsed < 0.3f)
            {
                if (root == null) yield break;
                float t = elapsed / 0.3f;
                float s = 1.70158f;
                t -= 1f;
                float bounce = t * t * ((s + 1f) * t - s);
                root.transform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, bounce);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (root != null)
                Object.Destroy(root);
            state.displayObject = null;
        }

        public static void UpdateState(NetworkMenuManager.RemoteMenuState state)
        {
            if (state.displayObject == null || state.closing) return;

            GameObject root = state.displayObject;
            bool thin = state.thinMenu;
            GameObject canvasObj = null;
            foreach (Transform child in root.transform)
            {
                if (child.name == "Canvas")
                {
                    canvasObj = child.gameObject;
                    break;
                }
            }

            foreach (Transform child in root.transform)
            {
                NetworkMenuBtnCollider btn = child.GetComponent<NetworkMenuBtnCollider>();
                if (btn != null && btn.relatedText != "PreviousPage" && btn.relatedText != "NextPage" && btn.relatedText != "Disconnect")
                {
                    Object.Destroy(child.gameObject);
                }
            }

            if (canvasObj != null)
            {
                foreach (Transform child in canvasObj.transform)
                {
                    if (child.name != "MenuTitle" && child.name != "PlayerName" && child.name != "DisconnectText")
                    {
                        Object.Destroy(child.gameObject);
                    }
                }
            }
            else
            {
                canvasObj = new GameObject("Canvas");
                canvasObj.transform.parent = root.transform;
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                scaler.dynamicPixelsPerUnit = 2500f;
                scaler.referencePixelsPerUnit = 100f;
            }

            string menuName = Main.doCustomName ? Main.customMenuName : "Seralyth Remake";
            foreach (Transform child in canvasObj.transform)
            {
                if (child.name == "MenuTitle")
                {
                    TextMeshPro tmp = child.GetComponent<TextMeshPro>();
                    if (tmp != null) tmp.text = menuName;
                }

                if (child.name == "PlayerName")
                {
                    TextMeshPro tmp = child.GetComponent<TextMeshPro>();
                    if (tmp != null)
                    {
                        string playerDisplay = "<b>" + state.player.NickName + "</b>";
                        playerDisplay += $" <color=grey>[</color><color=white>{state.page + 1}</color><color=grey>]</color>";
                        tmp.text = playerDisplay;
                    }
                }
            }

            string categoryName = state.category;
            if (categoryName != "Main")
            {
                CreateTMPText(canvasObj.transform, "CategoryLabel", $"[{categoryName}]", state.textColor1,
                    TextAlignmentOptions.Center, new Vector2(0.28f, 0.02f),
                    new Vector3(0.06f, 0f, 0.105f), Quaternion.Euler(180f, 90f, 90f));
            }

            int enabledCount = 0;
            foreach (var kv in state.buttonStates)
            {
                if (kv.Value) enabledCount++;
            }
            CreateTMPText(canvasObj.transform, "EnabledCount", enabledCount + " enabled", EnabledGreen,
                TextAlignmentOptions.Center, new Vector2(0.28f, 0.02f),
                new Vector3(0.06f, 0f, 0.075f), Quaternion.Euler(180f, 90f, 90f));

            BuildButtonPage(state, root, canvasObj, thin);
            UpdateColors(state);
            UpdatePosition(state);
        }

        public static void UpdateColors(NetworkMenuManager.RemoteMenuState state)
        {
            if (state.displayObject == null || state.closing) return;

            foreach (Transform child in state.displayObject.transform)
            {
                if (child.name == "seralyth_bg")
                {
                    Renderer bgR = child.GetComponent<Renderer>();
                    if (bgR != null) SetColor(bgR, state.menuBgColor);
                    bool thin = state.thinMenu;
                    child.localScale = thin ? new Vector3(0.1f, 1f, 1f) : new Vector3(0.1f, 1.5f, 1f);
                    continue;
                }

                NetworkMenuBtnCollider btn = child.GetComponent<NetworkMenuBtnCollider>();
                if (btn == null || string.IsNullOrEmpty(btn.relatedText)) continue;

                Renderer r = child.GetComponent<Renderer>();
                if (r == null) continue;

                if (btn.relatedText == "PreviousPage" || btn.relatedText == "NextPage")
                {
                    SetColor(r, state.swapButtonColors ? state.btnColor1 : state.btnColor0);
                }
                else if (btn.relatedText == "Disconnect")
                {
                    SetColor(r, state.swapButtonColors ? state.btnColor1 : state.btnColor0);
                }
                else if (btn.relatedText.StartsWith("Minus_") || btn.relatedText.StartsWith("Plus_"))
                {
                    SetColor(r, state.swapButtonColors ? state.btnColor1 : state.btnColor0);
                }
                else
                {
                    List<ButtonInfo> btns = GetCategoryButtons(state.category, state.buttonStates);
                    ButtonInfo found = null;
                    foreach (var b in btns)
                    {
                        if (b.buttonText == btn.relatedText) { found = b; break; }
                    }
                    if (found != null && found.label)
                    {
                        continue;
                    }
                    else if (found != null && !found.isTogglable)
                    {
                        SetColor(r, state.btnColor0);
                    }
                    else
                    {
                        bool isEnabled = state.buttonStates.TryGetValue(btn.relatedText, out bool val) && val;
                        SetColor(r, isEnabled ? state.btnColor1 : state.btnColor0);
                    }
                }
            }

            foreach (Transform child in state.displayObject.transform)
            {
                if (child.name != "Canvas") continue;
                foreach (Transform textChild in child)
                {
                    TextMeshPro tmp = textChild.GetComponent<TextMeshPro>();
                    if (tmp == null) continue;
                    if (textChild.name == "MenuTitle")
                    {
                        tmp.color = state.textColor0;
                    }
                    else if (textChild.name == "PlayerName")
                    {
                        tmp.color = state.textColor0;
                    }
                    else if (textChild.name == "CategoryLabel")
                    {
                        tmp.color = state.textColor1;
                    }
                    else if (textChild.name == "EnabledCount")
                    {
                        int remoteEnabled = 0;
                        foreach (var kv in state.buttonStates)
                        {
                            if (kv.Value) remoteEnabled++;
                        }
                        tmp.text = remoteEnabled + " enabled";
                        tmp.color = EnabledGreen;
                    }
                    else if (textChild.name == "DisconnectText")
                    {
                        tmp.color = state.textColor1;
                    }
                    else if (textChild.name.StartsWith("inc_minus_") || textChild.name.StartsWith("inc_plus_"))
                    {
                        tmp.color = state.textColor1;
                    }
                    else if (textChild.name.StartsWith("btn_"))
                    {
                        int btnIdx = 0;
                        if (int.TryParse(textChild.name.Substring(4), out btnIdx))
                        {
                            List<ButtonInfo> buttons = GetCategoryButtons(state.category, state.buttonStates);
                            int pageSize = Main.PageSize;
                            int startIdx = state.page * pageSize;
                            if (btnIdx + startIdx < buttons.Count)
                            {
                                ButtonInfo info = buttons[startIdx + btnIdx];
                                string btnName = info.buttonText;
                                string displayName = !string.IsNullOrEmpty(info.overlapText) ? info.overlapText : btnName;
                                if (info.detected)
                                    displayName = "<color=red>" + displayName + "</color>";
                                bool isLabel = info.label;
                                bool isTogglable = info.isTogglable;
                                bool isEnabled = isTogglable && !isLabel && state.buttonStates.TryGetValue(btnName, out bool val) && val;
                                Color textColor = isLabel ? state.textColor0 : (isTogglable ? (isEnabled ? state.textColor2 : state.textColor1) : state.textColor1);
                                tmp.color = textColor;
                                tmp.text = displayName;
                            }
                        }
                    }
                }
            }

            UpdatePosition(state);
        }

        public static void UpdatePosition(NetworkMenuManager.RemoteMenuState state)
        {
            if (state.displayObject == null || state.closing) return;
            state.displayObject.transform.position = state.position;
            state.displayObject.transform.rotation = state.rotation;
        }
    }

    public class NetworkMenuBtnCollider : MonoBehaviour
    {
        public string relatedText;
    }
}
