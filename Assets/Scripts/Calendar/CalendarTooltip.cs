using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using Calendar;

namespace Calendar
{
    /// <summary>
    /// UI component for displaying detailed tooltips when hovering over calendar dates with completed races
    /// </summary>
    public class CalendarTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public GameObject tooltipPanel;
        public Image tooltipBackgroundImage;
        public TextMeshProUGUI tooltipText;
        public RectTransform tooltipRectTransform;
        public Canvas parentCanvas;
        
        [Header("Visual Settings")]
        public float showDelay = 0.5f;
        public float fadeInDuration = 0.2f;
        public Vector2 offset = new Vector2(10, 10);
        public int maxTooltipWidth = 400;
        
        [Header("Sizing")]
        public float minTooltipWidth = 320f;
        public float screenEdgeMargin = 12f;
        
        [Header("Tooltip Styling")]
        [Tooltip("Background color for the tooltip")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark semi-transparent
        [Tooltip("Border color for the tooltip")]
        public Color borderColor = new Color(1f, 1f, 1f, 0.8f); // White border
        [Tooltip("Border width in pixels")]
        public float borderWidth = 2f;
        [Tooltip("Corner radius for rounded corners")]
        public float cornerRadius = 8f;
        [Tooltip("Padding inside the tooltip")]
        public Vector4 padding = new Vector4(12f, 8f, 12f, 8f); // left, top, right, bottom
        
        [Header("Calendar Integration")]
        public CalendarEvents calendarEvents;
        
        [Header("Z-Order")]
        public bool forceTopMost = true;
        public int topMostSortingOrder = 5000;
        
        private DateTime dateForThisButton;
        private bool isHovering = false;
        private CanvasGroup tooltipCanvasGroup;
        private bool tooltipVisible = false;
        private Graphic targetGraphic; // This is needed for raycasting
        private DayEventHandler dayEventHandler; // same object provider
        private Canvas tooltipOwnCanvas;
        // Track original hierarchy to allow temporary reparenting for top-most rendering
        private Transform originalParent;
        private int originalSiblingIndex;
        private bool reparentedForTopMost = false;

        private void Awake()
        {
            Debug.Log($"🔧 CalendarTooltip Awake() called on {gameObject.name}");
            dayEventHandler = GetComponent<DayEventHandler>();
            if (dayEventHandler != null)
            {
                Debug.Log($"✅ Found DayEventHandler on {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No DayEventHandler found on {gameObject.name}");
            }
            
            targetGraphic = GetComponent<Graphic>();
            if (targetGraphic == null)
            {
                // Prefer background on tooltipPanel if possible
                if (tooltipPanel != null)
                {
                    var panelImg = tooltipPanel.GetComponent<Image>();
                    if (panelImg == null) panelImg = tooltipPanel.AddComponent<Image>();
                    tooltipBackgroundImage = panelImg;
                    targetGraphic = panelImg;
                }
                else
                {
                    Image img = gameObject.AddComponent<Image>();
                    tooltipBackgroundImage = img;
                    targetGraphic = img;
                }
                if (tooltipBackgroundImage != null)
                {
                    tooltipBackgroundImage.raycastTarget = true;
                }
            }
            else
            {
                Debug.Log($"🔧 Found existing Graphic component on {gameObject.name}: {targetGraphic.GetType().Name}");
            }
            
            // Ensure raycast target is enabled
            targetGraphic.raycastTarget = true;
            Debug.Log($"🔧 Raycast target enabled on {gameObject.name}: {targetGraphic.raycastTarget}");
            
            // Get or add CanvasGroup for fading
            tooltipCanvasGroup = tooltipPanel?.GetComponent<CanvasGroup>();
            if (tooltipPanel != null && tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(true);
                SetupTooltipStyling();
                if (tooltipCanvasGroup != null)
                {
                    tooltipCanvasGroup.alpha = 0f;
                    tooltipCanvasGroup.interactable = false;
                    tooltipCanvasGroup.blocksRaycasts = false;
                }
            }
            
            // Ensure own Canvas on tooltipPanel for top-most rendering
            if (forceTopMost && tooltipPanel != null)
            {
                tooltipOwnCanvas = tooltipPanel.GetComponent<Canvas>();
                if (tooltipOwnCanvas == null)
                {
                    tooltipOwnCanvas = tooltipPanel.AddComponent<Canvas>();
                }
                tooltipOwnCanvas.overrideSorting = true;
                tooltipOwnCanvas.sortingOrder = topMostSortingOrder;
                
                // Mirror parent canvas layer/camera if available
                if (parentCanvas == null)
                {
                    parentCanvas = GetComponentInParent<Canvas>();
                }
                if (parentCanvas != null)
                {
                    tooltipOwnCanvas.sortingLayerID = parentCanvas.sortingLayerID;
                    tooltipOwnCanvas.renderMode = parentCanvas.renderMode;
                    tooltipOwnCanvas.worldCamera = parentCanvas.worldCamera;
                    tooltipOwnCanvas.planeDistance = parentCanvas.planeDistance;
                }
                
                // Ensure a GraphicRaycaster exists on tooltipPanel
                if (tooltipPanel.GetComponent<GraphicRaycaster>() == null)
                {
                    tooltipPanel.AddComponent<GraphicRaycaster>();
                }
            }
            
            tooltipVisible = false;
        }
        
        private void Start()
        {
            // Ensure we have a GraphicRaycaster on the canvas
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }
            
            if (parentCanvas != null)
            {
                GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("Added GraphicRaycaster to canvas for tooltip functionality");
                }
            }
            
            // Check for EventSystem
            if (EventSystem.current == null)
            {
                Debug.LogWarning("No EventSystem found in scene! Pointer events will not work. Add an EventSystem to the scene.");
            }
            
            // Additional debugging for raycast blocking issues
            StartCoroutine(DebugPointerEvents());
        }
        
        private System.Collections.IEnumerator DebugPointerEvents()
        {
            yield return new WaitForSeconds(1f); // Wait for everything to initialize
            
            // Check if there are any components that might be blocking raycasts
            var allGraphics = GetComponentsInChildren<Graphic>();
            Debug.Log($"🔍 Total Graphic components in {gameObject.name}: {allGraphics.Length}");
            
            foreach (var graphic in allGraphics)
            {
                Debug.Log($"  - {graphic.gameObject.name}: {graphic.GetType().Name}, RaycastTarget: {graphic.raycastTarget}");
            }
            
            // Check for potential blockers
            var canvasGroups = GetComponentsInChildren<CanvasGroup>();
            foreach (var cg in canvasGroups)
            {
                Debug.Log($"🔍 CanvasGroup on {cg.gameObject.name}: BlocksRaycasts = {cg.blocksRaycasts}, Interactable = {cg.interactable}");
            }
            
            // Check button components that might intercept events
            var buttons = GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                Debug.Log($"🔍 Button on {btn.gameObject.name}: Interactable = {btn.interactable}");
            }
        }
        
        /// <summary>
        /// Get the current date from the DayEventHandler component
        /// </summary>
        private DateTime GetDateFromDayEventHandler()
        {
            if (dayEventHandler != null)
            {
                return dayEventHandler.currentDate;
            }
            return default(DateTime);
        }
        
        /// <summary>
        /// Set the date that this calendar button represents
        /// Call this when setting up calendar buttons
        /// </summary>
        public void SetDate(DateTime date)
        {
            dateForThisButton = date;
            Debug.Log($"CalendarTooltip: Set date to {dateForThisButton.ToString("MMM dd, yyyy")}");
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            Debug.Log($"🔵 POINTER ENTER DETECTED on GameObject: {gameObject.name}");
            
            // Get date from DayEventHandler instead of requiring SetDate() call
            dateForThisButton = GetDateFromDayEventHandler();
            
            // Check if date is valid
            if (dateForThisButton == default(DateTime))
            {
                Debug.LogError($"❌ Date not available from DayEventHandler on {gameObject.name}!");
                Debug.LogError($"❌ Current date value: {dateForThisButton.ToString("MMM dd, yyyy")} (this should not be Jan 01, 0001)");
                return;
            }
            
            Debug.Log($"✅ Pointer entered date: {dateForThisButton.ToString("MMM dd, yyyy")} (from DayEventHandler)");
            
            if (calendarEvents == null) 
            {
                Debug.LogError("❌ CalendarEvents not assigned to CalendarTooltip!");
                return;
            }
            
            // First, check what events exist for this date (for debugging)
            var events = calendarEvents.GetEventsOnDate(dateForThisButton);
            Debug.Log($"📅 Found {events.Count} events for {dateForThisButton.ToString("MMM dd, yyyy")}: ");
            foreach (var evt in events)
            {
                Debug.Log($"  - Event: {evt.eventName}, Type: {evt.OccasionType}, PlayerPart: {evt.playerHasTakenPart}, Active: {evt.eventActive}");
            }
            
            // Check if this date has any events worth showing a tooltip for
            string tooltipContent = calendarEvents.GetDetailedTooltipForDate(dateForThisButton);
            Debug.Log($"🎯 Tooltip content for {dateForThisButton.ToString("MMM dd, yyyy")}: '{tooltipContent}'");
            
            if (!string.IsNullOrEmpty(tooltipContent))
            {
                isHovering = true;
                if (tooltipText != null)
                {
                    tooltipText.text = tooltipContent;
                    ResizeTooltipToContent();
                }
                else
                {
                    Debug.LogError("❌ tooltipText is null! Assign the TextMeshPro component.");
                }
                
                // Check tooltip panel status
                if (tooltipPanel != null)
                {
                    Debug.Log($"📋 Tooltip panel status: Active={tooltipPanel.activeSelf}, CanvasGroup Alpha={tooltipCanvasGroup?.alpha}");
                }
                else
                {
                    Debug.LogError("❌ tooltipPanel is null! Assign the tooltip panel GameObject.");
                }
                
                // Start showing tooltip after delay
                CancelInvoke(nameof(ShowTooltipAfterDelay));
                Invoke(nameof(ShowTooltipAfterDelay), showDelay);
                
                Debug.Log($"⏳ Tooltip will show after {showDelay} seconds...");
            }
            else
            {
                Debug.Log($"ℹ️ No meaningful tooltip content for date: {dateForThisButton.ToString("MMM dd, yyyy")} - tooltip will not be shown");
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"Pointer exited date: {dateForThisButton.ToString("MMM dd, yyyy")}");
            isHovering = false;
            CancelInvoke(nameof(ShowTooltipAfterDelay));
            HideTooltip();
        }
        
        private void ShowTooltipAfterDelay()
        {
            if (!isHovering) return;
            
            ShowTooltip();
        }
        
        private void ShowTooltip()
        {
            if (tooltipVisible || tooltipCanvasGroup == null) return;
            ResizeTooltipToContent();

            // Ensure references
            if (tooltipPanel != null && tooltipRectTransform == null)
            {
                tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
            }
            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            // Elevate tooltip above all UI and escape potential masks
            if (forceTopMost && tooltipPanel != null)
            {
                BringTooltipToFront();
            }
            
            // Bring to front inside its parent/root
            if (tooltipRectTransform != null)
            {
                tooltipRectTransform.SetAsLastSibling();
            }
            
            // Position tooltip near mouse cursor
            Vector2 mousePosition = Input.mousePosition;
            Vector2 tooltipPosition = mousePosition + offset;
            
            // Ensure tooltip stays within screen bounds
            if (parentCanvas != null)
            {
                RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    Vector2 canvasSize = canvasRect.sizeDelta;
                    
                    // Adjust horizontal position
                    if (tooltipRectTransform != null && tooltipPosition.x + tooltipRectTransform.sizeDelta.x > canvasSize.x - screenEdgeMargin)
                    {
                        tooltipPosition.x = mousePosition.x - tooltipRectTransform.sizeDelta.x - offset.x;
                        tooltipPosition.x = Mathf.Max(screenEdgeMargin, tooltipPosition.x);
                    }
                    
                    // Adjust vertical position
                    if (tooltipRectTransform != null && tooltipPosition.y + tooltipRectTransform.sizeDelta.y > canvasSize.y - screenEdgeMargin)
                    {
                        tooltipPosition.y = mousePosition.y - tooltipRectTransform.sizeDelta.y - offset.y;
                        tooltipPosition.y = Mathf.Max(screenEdgeMargin, tooltipPosition.y);
                    }
                }
                
                if (tooltipRectTransform != null)
                {
                    tooltipRectTransform.position = tooltipPosition;
                }
            }
            
            // Enable tooltip interaction and make it visible
            tooltipCanvasGroup.interactable = true;
            tooltipCanvasGroup.blocksRaycasts = true;
            
            // Fade in
            StopAllCoroutines();
            StartCoroutine(FadeTooltip(1f));
            
            tooltipVisible = true;
            
            Debug.Log("Tooltip shown");
        }
        
        private void HideTooltip()
        {
            if (!tooltipVisible || tooltipCanvasGroup == null) return;
            
            // Disable tooltip interaction
            tooltipCanvasGroup.interactable = false;
            tooltipCanvasGroup.blocksRaycasts = false;
            
            StopAllCoroutines();
            StartCoroutine(FadeTooltip(0f, () => { tooltipVisible = false; if (tooltipText != null) tooltipText.text = ""; RestoreTooltipParentIfNeeded(); }));
            
            Debug.Log("Tooltip hidden");
        }
        
        private System.Collections.IEnumerator FadeTooltip(float targetAlpha, System.Action onComplete = null)
        {
            float startAlpha = tooltipCanvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                yield return null;
            }
            
            tooltipCanvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private void OnDisable()
        {
            // Ensure we always restore hierarchy if object gets disabled while showing
            RestoreTooltipParentIfNeeded();
        }

        private void BringTooltipToFront()
        {
            // Ensure a dedicated Canvas exists
            if (tooltipOwnCanvas == null && tooltipPanel != null)
            {
                tooltipOwnCanvas = tooltipPanel.GetComponent<Canvas>();
                if (tooltipOwnCanvas == null)
                {
                    tooltipOwnCanvas = tooltipPanel.AddComponent<Canvas>();
                }
            }

            if (tooltipOwnCanvas != null)
            {
                tooltipOwnCanvas.overrideSorting = true;

                // Prefer the root canvas for consistent layering
                Canvas root = parentCanvas != null ? parentCanvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
                if (root != null)
                {
                    tooltipOwnCanvas.renderMode = root.renderMode;
                    tooltipOwnCanvas.worldCamera = root.worldCamera;
                    tooltipOwnCanvas.planeDistance = root.planeDistance;
                    tooltipOwnCanvas.sortingLayerID = root.sortingLayerID;
                }

                // Choose a sorting order above all other canvases
                int maxOrder = topMostSortingOrder;
                var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (canvases[i] == null) continue;
                    if (!canvases[i].overrideSorting) continue; // only compare explicit sorted canvases
                    if (canvases[i].sortingOrder > maxOrder)
                        maxOrder = canvases[i].sortingOrder;
                }
                // Bump a bit to be clearly on top
                tooltipOwnCanvas.sortingOrder = Mathf.Max(topMostSortingOrder, maxOrder + 50);
            }

            // Reparent to the root canvas transform to escape masks and be last
            if (!reparentedForTopMost && tooltipPanel != null)
            {
                Canvas rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
                if (rootCanvas != null)
                {
                    originalParent = tooltipPanel.transform.parent;
                    originalSiblingIndex = tooltipPanel.transform.GetSiblingIndex();
                    tooltipPanel.transform.SetParent(rootCanvas.transform, false);
                    reparentedForTopMost = true;
                }
            }
        }

        private void RestoreTooltipParentIfNeeded()
        {
            if (reparentedForTopMost && tooltipPanel != null && originalParent != null)
            {
                tooltipPanel.transform.SetParent(originalParent, false);
                if (originalSiblingIndex >= 0)
                {
                    tooltipPanel.transform.SetSiblingIndex(originalSiblingIndex);
                }
            }
            reparentedForTopMost = false;
        }
        
        private void Update()
        {
            // Update tooltip position to follow mouse if visible
            if (tooltipVisible && isHovering && parentCanvas != null && tooltipRectTransform != null)
            {
                Vector2 mousePosition = Input.mousePosition;
                Vector2 tooltipPosition = mousePosition + offset;
                
                // Keep within bounds
                RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    Vector2 canvasSize = canvasRect.sizeDelta;
                    
                    if (tooltipPosition.x + tooltipRectTransform.sizeDelta.x > canvasSize.x)
                    {
                        tooltipPosition.x = mousePosition.x - tooltipRectTransform.sizeDelta.x - offset.x;
                    }
                    
                    if (tooltipPosition.y + tooltipRectTransform.sizeDelta.y > canvasSize.y)
                    {
                        tooltipPosition.y = mousePosition.y - tooltipRectTransform.sizeDelta.y - offset.y;
                    }
                }
                
                tooltipRectTransform.position = Vector2.Lerp(tooltipRectTransform.position, tooltipPosition, Time.deltaTime * 10f);
                
            }
        }
        
        /// <summary>
        /// Manually show tooltip for a specific date (useful for debugging)
        /// </summary>
        [ContextMenu("Test Tooltip")]
        public void TestTooltip()
        {
            if (calendarEvents == null)
            {
                Debug.LogWarning("Calendar Events not assigned!");
                return;
            }
            
            string tooltipContent = calendarEvents.GetDetailedTooltipForDate(dateForThisButton);
            
            if (string.IsNullOrEmpty(tooltipContent))
            {
                Debug.Log($"No tooltip content for date: {dateForThisButton.ToString("MMM dd, yyyy")}");
            }
            else
            {
                Debug.Log($"Tooltip content for {dateForThisButton.ToString("MMM dd, yyyy")}:\n{tooltipContent}");
                tooltipText.text = tooltipContent;
                ShowTooltip();
            }
        }
        
        /// <summary>
        /// Resize background and panel to fully cover current tooltip text with padding
        /// </summary>
        private void ResizeTooltipToContent()
        {
            if (tooltipText == null || tooltipRectTransform == null) return;
            
            // Determine target width from canvas (clamped by min/max)
            float canvasWidth = maxTooltipWidth;
            if (parentCanvas != null)
            {
                var canvasRT = parentCanvas.GetComponent<RectTransform>();
                if (canvasRT != null)
                {
                    canvasWidth = canvasRT.rect.width - 2f * screenEdgeMargin;
                }
            }
            float panelWidth = Mathf.Clamp(canvasWidth, minTooltipWidth, maxTooltipWidth);
            float textWidth = Mathf.Max(0.0f, panelWidth - (padding.x + padding.z));
            
            // Configure text rect to fill with padding
            RectTransform textRT = tooltipText.rectTransform;
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(padding.x, padding.y);
            textRT.offsetMax = new Vector2(-padding.z, -padding.w);
            // For preferred height calculation, set a fixed width for TMP
            textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
            
            // Preferred height from TMP given width
            Vector2 pref = tooltipText.GetPreferredValues(tooltipText.text, textWidth, Mathf.Infinity);
            float textHeight = Mathf.Max(0.0f, pref.y);
            float panelHeight = textHeight + padding.y + padding.w;
            
            // Apply sizes
            if (tooltipBackgroundImage != null)
            {
                RectTransform bgRT = tooltipBackgroundImage.rectTransform;
                tooltipBackgroundImage.color = backgroundColor;
                bgRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
                bgRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            }
            tooltipRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
            tooltipRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
        }

        /// <summary>
        /// Setup the visual styling of the tooltip based on the configured settings
        /// </summary>
        private void SetupTooltipStyling()
        {
            if (tooltipText != null)
            {
                // Use new API for wrapping
                tooltipText.textWrappingMode = TextWrappingModes.Normal;
                tooltipText.alignment = TextAlignmentOptions.TopLeft;
                tooltipText.enableAutoSizing = false;
            }
            if (tooltipBackgroundImage != null)
            {
                // Leave color control to user, but ensure component exists
                tooltipBackgroundImage.raycastTarget = true;
            }
        }
    }
}
