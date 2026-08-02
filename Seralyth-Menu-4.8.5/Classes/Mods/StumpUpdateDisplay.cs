/*
 * Seralyth Menu  Classes/Mods/StumpUpdateDisplay.cs
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
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Mods;
using Seralyth.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using static Seralyth.Menu.Main;

namespace Seralyth.Classes.Mods
{
    public class StumpUpdateDisplay : MonoBehaviour
    {
        public static StumpUpdateDisplay Instance { get; private set; }

        private GameObject canvasObject;
        private Canvas canvas;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI contentText;
        private TextMeshProUGUI pageLabelText;

        private int currentPage;
        private int itemsPerPage = 5;
        private List<ChangelogEntry> allEntries;

        private Coroutine autoScrollCoroutine;
        public static bool AutoScrollEnabled = true;
        public static long LastSeenDllTimestamp;
        private static bool HasPoppedThisSession;
        private static long CurrentDllTimestamp => new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).LastWriteTime.Ticks;

        private static readonly Vector3 stumpPosition = new Vector3(-66f, 12f, -79f);

        private LineRenderer pointerLine;
        private GameObject pointerDot;
        private GraphicRaycaster graphicRaycaster;
        private EventSystem eventSystem;
        private PointerEventData pointerData;
        private readonly List<RaycastResult> uiResults = new List<RaycastResult>();
        private GameObject currentHover;
        private GameObject pressedUI;
        private bool lastTriggerClick;
        private Vector2 lastPointerPos;

        private readonly Dictionary<GameObject, System.Action> buttonActions = new Dictionary<GameObject, System.Action>();

        private void Awake()
        {
            Instance = this;
            allEntries = new List<ChangelogEntry>();
        }

        private void Start()
        {
            allEntries = new List<ChangelogEntry>(Changelog.Entries);
            allEntries.Reverse();

            if (CurrentDllTimestamp != LastSeenDllTimestamp && !HasPoppedThisSession)
                StartCoroutine(DelayedShow());
        }

        private IEnumerator DelayedShow()
        {
            while (GorillaTagger.Instance == null || GorillaTagger.Instance.mainCamera == null)
                yield return null;
            Show();
        }

        private void OnDestroy()
        {
            if (autoScrollCoroutine != null)
                StopCoroutine(autoScrollCoroutine);
            if (pointerLine != null)
                Destroy(pointerLine.gameObject);
            if (pointerDot != null)
                Destroy(pointerDot);
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (canvasObject == null) return;

            Billboard();
            DoPointerInteraction();
        }

        private void Billboard()
        {
            Transform cam = GorillaTagger.Instance.mainCamera.transform;
            Vector3 dir = (canvasObject.transform.position - cam.position).normalized;
            canvasObject.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private void DoPointerInteraction()
        {
            if (graphicRaycaster == null) return;

            Camera cam = GorillaTagger.Instance.mainCamera?.GetComponent<Camera>();
            if (cam == null) return;

            if (canvas.worldCamera == null)
                canvas.worldCamera = cam;

            if (pointerLine == null) return;

            Vector3 origin;
            Vector3 direction;
            bool isVR = XRSettings.isDeviceActive;

            if (isVR)
            {
                var (pos, rot, _, fwd, _) = ControllerUtilities.GetTrueRightHand();
                origin = pos;
                direction = fwd;
            }
            else
            {
                Ray mouseRay = cam.ScreenPointToRay(Input.mousePosition);
                origin = mouseRay.origin + mouseRay.direction * 0.5f;
                direction = mouseRay.direction;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Plane canvasPlane = new Plane(canvasRect.forward, canvasRect.position);
            Ray ray = new Ray(origin, direction);
            Vector3 pointerWorldPos = origin + direction * 5f;

            if (canvasPlane.Raycast(ray, out float enter) && enter > 0f)
                pointerWorldPos = ray.GetPoint(enter);

            pointerLine.SetPosition(0, origin);
            pointerLine.SetPosition(1, pointerWorldPos);

            if (pointerDot != null)
                pointerDot.transform.position = pointerWorldPos;

            if (eventSystem == null)
                eventSystem = EventSystem.current;

            if (pointerData == null)
                pointerData = new PointerEventData(eventSystem);

            Vector3 screenPoint = cam.WorldToScreenPoint(pointerWorldPos);

            if (screenPoint.z < 0f)
            {
                ClearHover();
                return;
            }

            pointerData.position = screenPoint;
            uiResults.Clear();
            graphicRaycaster.Raycast(pointerData, uiResults);

            GameObject hitUI = uiResults.Count > 0 ? uiResults[0].gameObject : null;

            if (hitUI != currentHover)
            {
                if (currentHover != null)
                {
                    ExecuteEvents.Execute(currentHover, pointerData, ExecuteEvents.pointerExitHandler);
                    SetButtonHighlight(currentHover, false);
                }
                if (hitUI != null)
                {
                    ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerEnterHandler);
                    SetButtonHighlight(hitUI, true);
                }
                currentHover = hitUI;
            }

            bool trigger = isVR
                ? rightTrigger > 0.5f || rightGrab
                : Input.GetMouseButton(0);

            pointerData.delta = pointerData.position - lastPointerPos;
            lastPointerPos = pointerData.position;

            if (trigger && !lastTriggerClick && hitUI != null)
            {
                pressedUI = hitUI;
                pointerData.pressPosition = pointerData.position;
                pointerData.pointerPressRaycast = uiResults[0];
                ExecuteEvents.Execute(hitUI, pointerData, ExecuteEvents.pointerDownHandler);
                pointerData.pointerPress = hitUI;

                System.Action action = GetButtonAction(hitUI);
                if (action != null)
                {
                    action();
                    string soundName = hitUI.name switch
                    {
                        "PreviousPage" => "Previous",
                        "NextPage" => "Next",
                        _ => "Button"
                    };
                    SoundManager.Play(soundName);
                }
            }

            if (!trigger && lastTriggerClick && pressedUI != null)
            {
                ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerUpHandler);
                if (pressedUI == hitUI)
                    ExecuteEvents.Execute(pressedUI, pointerData, ExecuteEvents.pointerClickHandler);
                pressedUI = null;
                pointerData.pointerPress = null;
            }

            lastTriggerClick = trigger;
        }

        private void ClearHover()
        {
            if (currentHover != null)
            {
                if (pointerData != null)
                    ExecuteEvents.Execute(currentHover, pointerData, ExecuteEvents.pointerExitHandler);
                SetButtonHighlight(currentHover, false);
                currentHover = null;
            }
        }

        private void SetButtonHighlight(GameObject btn, bool highlighted)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = highlighted ? buttonColors[1].GetCurrentColor() : GetOriginalColor(btn);
        }

        private Color GetOriginalColor(GameObject btn)
        {
            string name = btn.name;
            if (name == "DoneButton") return buttonColors[0].GetCurrentColor();
            if (name == "PreviousPage" || name == "NextPage") return buttonColors[1].GetCurrentColor();
            return buttonColors[0].GetCurrentColor();
        }

        private System.Action GetButtonAction(GameObject obj)
        {
            buttonActions.TryGetValue(obj, out var action);
            return action;
        }

        private void CreateCanvas()
        {
            canvasObject = new GameObject("Seralyth_StumpUpdateCanvas");
            Transform head = GorillaTagger.Instance.mainCamera.transform;
            canvasObject.transform.position = head.position + head.forward * 0.6f;
            canvasObject.transform.localScale = Vector3.one * 0.001f;

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            graphicRaycaster = canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 550f);

            if (EventSystem.current == null)
            {
                GameObject es = new GameObject("Seralyth_EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            eventSystem = EventSystem.current;

            CreatePointerLine();
            CreatePointerDot();
            CreateBackground();
            CreateTitle();
            CreateContentArea();
            CreatePageLabel();
            CreateDoneButton();
            CreatePageButtons();

            Billboard();
        }

        private void CreatePointerLine()
        {
            GameObject lineObj = new GameObject("Seralyth_StumpPointerLine");

            pointerLine = lineObj.AddComponent<LineRenderer>();
            pointerLine.material = new Material(Shader.Find("GUI/Text Shader"));
            pointerLine.startWidth = 0.025f;
            pointerLine.endWidth = 0.025f;
            pointerLine.useWorldSpace = true;
            pointerLine.positionCount = 2;
            pointerLine.startColor = buttonColors[0].GetCurrentColor();
            Color endCol = buttonColors[0].GetCurrentColor();
            endCol.a = 0.5f;
            pointerLine.endColor = endCol;
            pointerLine.numCapVertices = 10;
            pointerLine.numCornerVertices = 5;
        }

        private void CreatePointerDot()
        {
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointerDot.name = "Seralyth_StumpPointerDot";
            pointerDot.transform.localScale = Vector3.one * 0.05f;
            pointerDot.GetComponent<Renderer>().material.color = buttonColors[0].GetCurrentColor();
            pointerDot.GetComponent<Collider>().enabled = false;
        }

        private void CreateBackground()
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasObject.transform, false);

            Image bgImage = bg.AddComponent<Image>();
            Color bgCol = menuBackgroundColor.GetCurrentColor();
            bgCol.a = 230f / 255f;
            bgImage.color = bgCol;

            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject border = new GameObject("Border");
            border.transform.SetParent(canvasObject.transform, false);

            Image borderImage = border.AddComponent<Image>();
            borderImage.color = buttonColors[0].GetCurrentColor();

            RectTransform borderRect = border.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(8f, 8f);
            borderRect.offsetMin = new Vector2(-4f, -4f);
            borderRect.offsetMax = new Vector2(4f, 4f);
            borderRect.SetAsFirstSibling();
        }

        private void CreateTitle()
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObject.transform, false);

            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Seralyth Remake Updates";
            titleText.color = textColors[0].GetCurrentColor();
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Top;
            titleText.fontStyle = FontStyles.Bold;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(0f, -6f);
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-12f, -4f);
        }

        private void CreateContentArea()
        {
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(canvasObject.transform, false);

            contentText = contentObj.AddComponent<TextMeshProUGUI>();
            contentText.fontSize = 20;
            contentText.alignment = TextAlignmentOptions.TopLeft;
            contentText.lineSpacing = 24f;
            contentText.color = textColors[1].GetCurrentColor();

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0.15f);
            contentRect.anchorMax = new Vector2(1f, 0.80f);
            contentRect.sizeDelta = new Vector2(-24f, -6f);
            contentRect.offsetMin = new Vector2(12f, 0f);
            contentRect.offsetMax = new Vector2(-12f, -4f);

            UpdateContent();
        }

        private void CreatePageLabel()
        {
            GameObject labelObj = new GameObject("PageLabel");
            labelObj.transform.SetParent(canvasObject.transform, false);

            pageLabelText = labelObj.AddComponent<TextMeshProUGUI>();
            pageLabelText.fontSize = 16;
            pageLabelText.alignment = TextAlignmentOptions.BottomRight;
            pageLabelText.color = textColors[1].GetCurrentColor();
            pageLabelText.alpha = 0.6f;

            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(1f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.sizeDelta = new Vector2(120f, 20f);
            labelRect.anchoredPosition = new Vector2(-32f, 50f);

            UpdatePageLabel();
        }

        private void UpdateContent()
        {
            if (contentText == null) return;

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allEntries.Count / itemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            int startIndex = currentPage * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, allEntries.Count);

            string text = "";
            for (int i = startIndex; i < endIndex; i++)
            {
                var entry = allEntries[i];
                string color = entry.type == "ADDED" ? "green" : entry.type == "REMOVED" ? "red" : entry.type == "UPDATED" ? "yellow" : "purple";
                string displayName = Changelog.GetTypeDisplayName(entry.type);
                text += $"<color={color}>[{displayName}]</color> {entry.description}\n\n";
            }

            contentText.text = text.TrimEnd('\n');
            UpdatePageLabel();
        }

        private void UpdatePageLabel()
        {
            if (pageLabelText == null) return;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allEntries.Count / itemsPerPage));
            pageLabelText.text = $"Page {currentPage + 1} / {totalPages}";
        }

        private void CreateButtonBase(string name, Vector2 anchoredPos, Vector2 size, Color color, string text, System.Action action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(canvasObject.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = color;

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColors[1].GetCurrentColor();

            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            buttonActions[btnObj] = action;
        }

        private void CreateDoneButton()
        {
            GameObject btnObj = new GameObject("DoneButton");
            btnObj.transform.SetParent(canvasObject.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = buttonColors[0].GetCurrentColor();

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(90f, 34f);
            rect.anchoredPosition = new Vector2(0f, 10f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Done";
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColors[1].GetCurrentColor();

            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            buttonActions[btnObj] = () => Hide();
        }

        private void CreatePageButtons()
        {
            CreateButtonBase("PreviousPage", new Vector2(-110f, 8f), new Vector2(80f, 34f), buttonColors[1].GetCurrentColor(), "< Prev", PreviousPage);
            CreateButtonBase("NextPage", new Vector2(-32f, 8f), new Vector2(80f, 34f), buttonColors[1].GetCurrentColor(), "Next >", NextPage);
        }

        public void PreviousPage()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allEntries.Count / itemsPerPage));
            currentPage--;
            if (currentPage < 0)
                currentPage = totalPages - 1;
            UpdateContent();
        }

        public void NextPage()
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allEntries.Count / itemsPerPage));
            currentPage++;
            if (currentPage >= totalPages)
                currentPage = 0;
            UpdateContent();
        }

        public void Show()
        {
            allEntries = new List<ChangelogEntry>(Changelog.Entries);
            allEntries.Reverse();

            if (canvasObject == null)
            {
                currentPage = 0;
                CreateCanvas();
            }
            else
            {
                Transform head = GorillaTagger.Instance.mainCamera.transform;
                canvasObject.transform.position = head.position + head.forward * 0.6f;
                canvasObject.SetActive(true);
                if (pointerLine != null)
                    pointerLine.gameObject.SetActive(true);
                if (pointerDot != null)
                    pointerDot.SetActive(true);
                UpdateContent();
            }

            if (AutoScrollEnabled && autoScrollCoroutine == null)
                autoScrollCoroutine = StartCoroutine(AutoScroll());
        }

        public void Hide()
        {
            HasPoppedThisSession = true;
            LastSeenDllTimestamp = CurrentDllTimestamp;
            Settings.SavePreferences();
            ClearNewBadge();
            if (autoScrollCoroutine != null)
            {
                StopCoroutine(autoScrollCoroutine);
                autoScrollCoroutine = null;
            }
            if (canvasObject != null)
                canvasObject.SetActive(false);
            if (pointerLine != null)
                pointerLine.gameObject.SetActive(false);
            if (pointerDot != null)
                pointerDot.SetActive(false);
            ClearHover();
        }

        public void Toggle()
        {
            if (canvasObject != null && canvasObject.activeSelf)
                Hide();
            else
                Show();
        }

        private static void ClearNewBadge()
        {
            ButtonInfo target = Buttons.GetIndex("Stump Updates");
            if (target == null || target.overlapText == null) return;
            string indicator = " <color=grey>[</color><color=green>New</color><color=grey>]</color>";
            if (target.overlapText.Contains(indicator))
            {
                target.overlapText = target.overlapText.Replace(indicator, "");
                if (target.overlapText == target.buttonText)
                    target.overlapText = target.buttonText;
            }
        }

        private IEnumerator AutoScroll()
        {
            while (true)
            {
                yield return new WaitForSeconds(6f);
                if (canvasObject != null && canvasObject.activeSelf)
                    NextPage();
            }
        }
    }
}
