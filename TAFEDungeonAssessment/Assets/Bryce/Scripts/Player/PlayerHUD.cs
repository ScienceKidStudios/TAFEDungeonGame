using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A very simple on-screen display: crosshair, "what can I do?" prompt and a controls list.
    /// Uses OnGUI so it needs no Canvas setup.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        public PlayerInteractor interactor;
        public PlayerCarrier carrier;

        [Tooltip("Show the controls list in the bottom-left corner.")]
        public bool showControls = true;

        private GUIStyle centerStyle;
        private GUIStyle cornerStyle;

        private void Awake()
        {
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (carrier == null) carrier = GetComponent<PlayerCarrier>();
        }

        private void OnGUI()
        {
            if (centerStyle == null)
            {
                centerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    fontStyle = FontStyle.Bold
                };
                centerStyle.normal.textColor = Color.white;
                cornerStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                cornerStyle.normal.textColor = Color.white;
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            GUI.Label(new Rect(cx - 10, cy - 12, 20, 24), "+", centerStyle);
            GUI.Label(new Rect(cx - 300, cy + 30, 600, 30), BuildPrompt(), centerStyle);

            if (carrier != null && carrier.Message != null)
                GUI.Label(new Rect(cx - 300, cy + 60, 600, 30), carrier.Message, centerStyle);

            if (showControls)
            {
                GUI.Label(new Rect(16, Screen.height - 110, 420, 100),
                    "WASD move   Space jump   Shift sprint\n" +
                    "E interact (buttons, levers)\n" +
                    "Hold Left Mouse: carry / drag   Right Mouse: throw\n" +
                    "Esc: free mouse", cornerStyle);
            }
        }

        private string BuildPrompt()
        {
            if (carrier != null && carrier.HeldBody != null)
                return carrier.IsLifting ? "Release LMB to drop, RMB to throw" : "Dragging... release LMB to let go";

            if (interactor != null && interactor.CurrentTarget != null)
                return $"[{interactor.interactKey}] {interactor.CurrentTarget.GetPrompt()}";

            if (carrier != null)
            {
                Carryable look = carrier.LookTarget();
                if (look != null)
                {
                    if (look.Mass > carrier.maxDragMass) return $"{look.displayName} ({look.Mass:0} kg) - too heavy to move";
                    if (look.Mass > carrier.maxLiftMass) return $"[LMB] Drag {look.displayName} ({look.Mass:0} kg)";
                    return $"[LMB] Pick up {look.displayName} ({look.Mass:0} kg)";
                }
            }
            return "";
        }
    }
}
