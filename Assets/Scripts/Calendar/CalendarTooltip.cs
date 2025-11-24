using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Calendar
{
    /// <summary>
    /// Tooltip that auto-sizes to content, flips around the cursor near edges,
    /// clamps inside the canvas, and enables vertical scrolling if content is too tall.
    /// </summary>
    public class CalendarTooltip : MonoBehaviour
    {
        [Header("Tooltip UI")]
        public GameObject tooltipPanel;          // Root object with an Image (background)
        public TextMeshProUGUI tooltipText;      // TMP text child
        public Image tooltipBackground;          // Background image
        public Canvas parentCanvas;              // Canvas that contains this tooltip

        [Header("Tooltip Styling")]
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        public Color borderColor = new Color(0.4f, 0.6f, 0.8f, 1f);  // (not used here but kept for future)
        public Color textColor = new Color(0.9f, 0.95f, 1f, 1f);

        [Header("Settings")]
        public float showDelay = 0.3f;
        public Vector2 offset = new Vector2(15, 10);
        public float maxTooltipWidth = 400f;
        public float minTooltipWidth = 250f;
        public float paddingHorizontal = 12f;
        public float paddingVertical = 8f;

        // Internal state
        private bool isHovering = false;
        private string currentTooltipContent = "";

        // Optional scrolling helpers (added at runtime if needed)
        [SerializeField] private RectMask2D mask;
        [SerializeField] private ScrollRect scrollRect;

        // Static access
        public static CalendarTooltip Instance { get; private set; }

        private void Awake()
        {
            Instance = this;

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        /// <summary>Request to show a tooltip for given content (HTML-style TMP rich text allowed).</summary>
        public void ShowTooltip(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            currentTooltipContent = content;
            isHovering = true;

            CancelInvoke(nameof(DisplayTooltip));
            Invoke(nameof(DisplayTooltip), showDelay);
        }

        /// <summary>Hide the tooltip immediately.</summary>
        public void HideTooltip()
        {
            isHovering = false;
            CancelInvoke(nameof(DisplayTooltip));

            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void DisplayTooltip()
        {
            if (!isHovering || string.IsNullOrEmpty(currentTooltipContent)) return;
            if (tooltipPanel == null || tooltipText == null) return;

            // Setup references
            if (parentCanvas == null) parentCanvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (canvasRect == null || tooltipRect == null) return;

            // Put content and base styles
            tooltipText.text = currentTooltipContent;
            tooltipText.color = textColor;
            tooltipText.enableWordWrapping = true;
            tooltipText.overflowMode = TextOverflowModes.Overflow;

            // Convert mouse to canvas local space
            Vector2 mouse;
            if (!InputHelpers.TryGetPrimaryPointerPosition(out mouse))
                return;
            Vector2 mouseLocal;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, mouse, parentCanvas.worldCamera, out mouseLocal))
                return;

            // ---------- Auto-size ----------
            Vector2 canvasSize = canvasRect.sizeDelta;

            float availableWidth = Mathf.Min(maxTooltipWidth, canvasSize.x * 0.9f);
            float optimalWidth = Mathf.Max(
                minTooltipWidth,
                Mathf.Min(availableWidth, tooltipText.preferredWidth + paddingHorizontal * 2f)
            );

            // Constrain text width to compute preferred height
            tooltipText.rectTransform.sizeDelta = new Vector2(optimalWidth - paddingHorizontal * 2f, 0f);

            // Force TMP & layout to update before reading preferredHeight
            tooltipText.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();

            float contentHeight = tooltipText.preferredHeight;
            float maxHeight = canvasSize.y * 0.9f;                       // allow up to 90% of canvas height
            float totalHeight = Mathf.Clamp(contentHeight + paddingVertical * 2f, 50f, maxHeight);

            // Apply final tooltip size
            Vector2 tooltipSize = new Vector2(optimalWidth, totalHeight);
            tooltipRect.sizeDelta = tooltipSize;

            // Layout text inside (top anchored so it grows downward)
            var textRect = tooltipText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot     = new Vector2(0.5f, 1f);
            // Offsets: top padding is -paddingVertical (because top-anchored),
            // bottom offset is negative full content height minus padding.
            textRect.offsetMax = new Vector2(-paddingHorizontal, -paddingVertical);
            textRect.offsetMin = new Vector2( paddingHorizontal, -(contentHeight + paddingVertical));

            // Background look & make sure it doesn't eat raycasts
            if (tooltipBackground != null)
            {
                tooltipBackground.color = backgroundColor;
                tooltipBackground.raycastTarget = false;
            }

            // Enable vertical scroll only if needed
            EnsureScrollable(tooltipRect, textRect, contentHeight, totalHeight);

            // ---------- Position ----------
            Vector2 pos = CalculateSmartPosition(mouseLocal, tooltipSize, canvasSize, tooltipRect);
            tooltipRect.localPosition = pos;

            // Ensure the panel never blocks input
            var cg = tooltipPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// Picks a pivot & position based on free space around the cursor and clamps inside the canvas.
        /// </summary>
        private Vector2 CalculateSmartPosition(
            Vector2 mouseLocal,
            Vector2 tooltipSize,
            Vector2 canvasSize,
            RectTransform tooltipRect)
        {
            Vector2 half = canvasSize * 0.5f;
            const float pad = 8f;

            float rightRoom  =  half.x - mouseLocal.x;
            float leftRoom   =  half.x + mouseLocal.x;
            float topRoom    =  half.y - mouseLocal.y;
            float bottomRoom =  half.y + mouseLocal.y;

            bool placeRight = rightRoom >= tooltipSize.x + offset.x + pad;
            bool placeAbove = topRoom   >= tooltipSize.y + offset.y + pad;

            // pivot matches the corner "touching" the cursor
            tooltipRect.pivot = new Vector2(placeRight ? 0f : 1f, placeAbove ? 1f : 0f);

            Vector2 pos = mouseLocal;
            pos.x += placeRight ?  offset.x : -offset.x;
            pos.y += placeAbove ?  offset.y : -offset.y;

            if (!placeRight) pos.x -= tooltipSize.x;
            if (!placeAbove) pos.y -= tooltipSize.y;

            // clamp fully inside canvas
            float minX = -half.x + pad;
            float maxX =  half.x - tooltipSize.x - pad;
            float minY = -half.y + pad;
            float maxY =  half.y - tooltipSize.y - pad;

            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);

            return pos;
        }

        /// <summary>
        /// Adds/updates RectMask2D + ScrollRect to enable vertical scrolling when content is taller than the visible area.
        /// </summary>
        private void EnsureScrollable(RectTransform tooltipRect, RectTransform textRect, float contentHeight, float totalHeight)
        {
            bool needsScroll = contentHeight + paddingVertical * 2f > totalHeight;

            if (needsScroll)
            {
                if (mask == null)
                    mask = tooltipRect.GetComponent<RectMask2D>() ?? tooltipRect.gameObject.AddComponent<RectMask2D>();

                if (scrollRect == null)
                    scrollRect = tooltipRect.GetComponent<ScrollRect>() ?? tooltipRect.gameObject.AddComponent<ScrollRect>();

                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.viewport = tooltipRect;
                scrollRect.content  = textRect;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 25f;

                // Make sure the content rect reports full height so ScrollRect can compute overflow
                textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, contentHeight);
            }
            else
            {
                if (scrollRect != null) scrollRect.vertical = false;
                var m = tooltipRect.GetComponent<RectMask2D>();
                if (m != null) Destroy(m);
            }
        }

        private void Update()
        {
            // Follow the mouse while visible
            if (tooltipPanel != null && tooltipPanel.activeInHierarchy && isHovering)
            {
                if (parentCanvas == null) parentCanvas = tooltipPanel.GetComponentInParent<Canvas>();
                if (parentCanvas == null) return;

                RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
                if (canvasRect == null || tooltipRect == null) return;

                Vector2 mouse;
                Vector2 mouseLocal;
                if (!InputHelpers.TryGetPrimaryPointerPosition(out mouse))
                    return;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, mouse, parentCanvas.worldCamera, out mouseLocal))
                {
                    Vector2 canvasSize = canvasRect.sizeDelta;
                    Vector2 size = tooltipRect.sizeDelta;
                    Vector2 target = CalculateSmartPosition(mouseLocal, size, canvasSize, tooltipRect);

                    tooltipRect.localPosition = Vector2.Lerp(tooltipRect.localPosition, target, Time.deltaTime * 10f);
                }
            }
        }
    }
}
