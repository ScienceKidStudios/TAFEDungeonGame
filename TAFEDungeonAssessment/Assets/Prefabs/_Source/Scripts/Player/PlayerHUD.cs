using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleMechanics
{
    /// <summary>
    /// A simple on-screen display: crosshair, "what can I do?" prompt and a controls list.
    /// The canvas is built in code when the game starts, so the prefab needs no UI setup.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Tooltip("The player's PlayerInteractor. Found automatically if left empty.")]
        public PlayerInteractor interactor;

        [Tooltip("The player's PlayerCarrier. Found automatically if left empty.")]
        public PlayerCarrier carrier;

        [Tooltip("The font for the on-screen text. If empty, the TextMesh Pro default font is used.")]
        public TMP_FontAsset font;

        [Tooltip("Show the list of controls in the bottom-left corner.")]
        public bool showControls = true;

        private TextMeshProUGUI promptText;
        private TextMeshProUGUI messageText;

        private void Awake()
        {
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (carrier == null) carrier = GetComponent<PlayerCarrier>();
        }

        private void Start()
        {
            BuildCanvas();
        }

        private void Update()
        {
            promptText.text = BuildPrompt();
            messageText.text = carrier != null && carrier.Message != null ? carrier.Message : "";
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("HUD Canvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Scales the HUD with the screen so text is the same relative size at any resolution.
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateText(canvas.transform, "Crosshair", "+", 40, Vector2.zero, new Vector2(60f, 60f), TextAlignmentOptions.Center);
            promptText = CreateText(canvas.transform, "Prompt", "", 30, new Vector2(0f, -70f), new Vector2(1200f, 50f), TextAlignmentOptions.Center);
            messageText = CreateText(canvas.transform, "Message", "", 26, new Vector2(0f, -115f), new Vector2(1200f, 44f), TextAlignmentOptions.Center);
            messageText.color = new Color(1f, 0.82f, 0.45f);

            if (showControls)
                CreateControlsPanel(canvas.transform);
        }

        private void CreateControlsPanel(Transform canvas)
        {
            GameObject panel = new GameObject("Controls Panel", typeof(RectTransform));
            panel.transform.SetParent(canvas, false);
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(560f, 150f);
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            TextMeshProUGUI controls = CreateText(panel.transform, "Controls",
                "<b>WASD</b> move   <b>Space</b> jump   <b>Shift</b> sprint\n" +
                "<b>E</b> use levers, switches and chests\n" +
                "<b>Hold Left Mouse</b> carry / drag   <b>Right Mouse</b> throw\n" +
                "<b>Esc</b> free the mouse",
                22, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            RectTransform textRect = controls.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Vector2 position,
            Vector2 boxSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            // A soft dark outline keeps white text readable on bright walls.
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);

            tmp.rectTransform.anchoredPosition = position;
            tmp.rectTransform.sizeDelta = boxSize;
            return tmp;
        }

        private string BuildPrompt()
        {
            if (carrier != null && carrier.HeldBody != null)
                return carrier.IsLifting ? "Let go of Left Mouse to drop. Right Mouse to throw" : "Dragging... let go of Left Mouse to stop";

            if (interactor != null && interactor.CurrentTarget != null)
                return $"[{interactor.interactKey}] {interactor.CurrentTarget.GetPrompt()}";

            if (carrier != null)
            {
                Carryable look = carrier.LookTarget();
                if (look != null)
                {
                    if (look.Mass > carrier.maxDragMass) return $"{look.displayName} - too heavy to move";
                    if (look.Mass > carrier.maxLiftMass) return $"[Left Mouse] Drag {look.displayName}";
                    return $"[Left Mouse] Pick up {look.displayName}";
                }
            }
            return "";
        }
    }
}
