using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using Calendar;

namespace Calendar
{
    /// <summary>
    /// Simple tooltip that appears when hovering over calendar events
    /// </summary>
    public class CalendarTooltip : MonoBehaviour
    {
        [Header("Tooltip UI")]
        public GameObject tooltipPanel;
        public TextMeshProUGUI tooltipText;
        public Image tooltipBackground;
        
        [Header("Tooltip Styling")]
        [Tooltip("Background color for the tooltip")]
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f); // Darker blue-gray with high opacity
        [Tooltip("Border color for the tooltip")]
        public Color borderColor = new Color(0.4f, 0.6f, 0.8f, 1f); // Light blue border
        [Tooltip("Text color for the tooltip")]
        public Color textColor = new Color(0.9f, 0.95f, 1f, 1f); // Light blue-white text
        
        [Header("Settings")]
        public float showDelay = 0.3f;
        public Vector2 offset = new Vector2(15, 10);
        public float maxTooltipWidth = 400f;
        public float minTooltipWidth = 250f;
        public float paddingHorizontal = 12f;
        public float paddingVertical = 8f;
        
        private bool isHovering = false;
        private string currentTooltipContent = "";
        
        // Static instance for easy access
        public static CalendarTooltip Instance { get; private set; }
        
        private void Awake()
        {
            Debug.Log("🔧 CalendarTooltip Awake() called - Setting up instance");
            Instance = this;
            
            Debug.Log($"🔧 tooltipPanel assigned: {tooltipPanel != null}");
            Debug.Log($"🔧 tooltipText assigned: {tooltipText != null}");
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
                Debug.Log("✅ Tooltip panel hidden on startup");
            }
            else
            {
                Debug.LogError("❌ tooltipPanel is NULL in CalendarTooltip!");
            }
        }
        
        /// <summary>
        /// Show tooltip with content at mouse position
        /// </summary>
        public void ShowTooltip(string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            
            currentTooltipContent = content;
            isHovering = true;
            
            // Cancel any existing delayed show
            CancelInvoke(nameof(DisplayTooltip));
            
            // Show tooltip after delay
            Invoke(nameof(DisplayTooltip), showDelay);
        }
        
        /// <summary>
        /// Hide the tooltip
        /// </summary>
        public void HideTooltip()
        {
            isHovering = false;
            CancelInvoke(nameof(DisplayTooltip));
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        private void DisplayTooltip()
        {
            if (!isHovering || string.IsNullOrEmpty(currentTooltipContent)) return;
            
            if (tooltipPanel == null || tooltipText == null) return;
            
            // Set the text and apply initial styling
            tooltipText.text = currentTooltipContent;
            tooltipText.color = textColor;
            
            // Configure text wrapping and sizing
            tooltipText.enableWordWrapping = true;
            tooltipText.overflowMode = TextOverflowModes.Overflow; // Allow overflow to get proper height calculation
            
            // Get canvas and rect transforms
            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            
            if (canvasRect == null || tooltipRect == null) return;
            
            // Convert mouse position to canvas space
            Vector2 mousePos = Input.mousePosition;
            Vector2 canvasMousePos;
            
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mousePos, canvas.worldCamera, out canvasMousePos)) return;
            
            // Calculate tooltip size based on content
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            // Determine optimal width based on content and available space
            float availableWidth = Mathf.Min(maxTooltipWidth, canvasSize.x * 0.4f); // Max 40% of screen width
            float optimalWidth = Mathf.Max(minTooltipWidth, Mathf.Min(availableWidth, tooltipText.preferredWidth + paddingHorizontal * 2));
            
            // First, set the text width to calculate proper height
            tooltipText.rectTransform.sizeDelta = new Vector2(optimalWidth - paddingHorizontal * 2, 0);
            
            // Force text to update its layout
            Canvas.ForceUpdateCanvases();
            
            // Now get the actual preferred height based on the constrained width
            float contentHeight = tooltipText.preferredHeight;
            float totalHeight = contentHeight + paddingVertical * 2;
            
            // Ensure minimum height and cap maximum height to prevent huge tooltips
            totalHeight = Mathf.Max(totalHeight, 50f); // Minimum height
            totalHeight = Mathf.Min(totalHeight, canvasSize.y * 0.8f); // Max 80% of screen height
            
            // Set final tooltip size
            Vector2 tooltipSize = new Vector2(optimalWidth, totalHeight);
            tooltipRect.sizeDelta = tooltipSize;
            
            // Set text rect to fit within padding with correct height
            tooltipText.rectTransform.anchorMin = Vector2.zero;
            tooltipText.rectTransform.anchorMax = Vector2.one;
            tooltipText.rectTransform.offsetMin = new Vector2(paddingHorizontal, paddingVertical);
            tooltipText.rectTransform.offsetMax = new Vector2(-paddingHorizontal, -paddingVertical);
            
            // Apply background styling
            if (tooltipBackground != null)
            {
                tooltipBackground.color = backgroundColor;
                tooltipBackground.raycastTarget = false;
            }
            
            // Smart positioning based on screen position
            Vector2 tooltipPos = CalculateSmartPosition(canvasMousePos, tooltipSize, canvasSize);
            
            // Set position
            tooltipRect.localPosition = tooltipPos;
            
            // Ensure tooltip doesn't block raycasts
            CanvasGroup tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            tooltipCanvasGroup.blocksRaycasts = false;
            
            // Show the tooltip
            tooltipPanel.SetActive(true);
        }
        
        private Vector2 CalculateSmartPosition(Vector2 mousePos, Vector2 tooltipSize, Vector2 canvasSize)
        {
            Vector2 position;
            
            // Simple positioning: tooltip appears to the right of mouse by default
            position.x = mousePos.x + offset.x;
            position.y = mousePos.y + offset.y;
            
            // If tooltip would go off the right edge, position it to the left of mouse
            if (position.x + tooltipSize.x > canvasSize.x * 0.5f)
            {
                position.x = mousePos.x - tooltipSize.x - offset.x;
            }
            
            // If tooltip would go off the top edge, position it below mouse
            if (position.y + tooltipSize.y > canvasSize.y * 0.5f)
            {
                position.y = mousePos.y - tooltipSize.y - offset.y;
            }
            
            // If tooltip would go off the bottom edge, position it above mouse
            if (position.y < -canvasSize.y * 0.5f)
            {
                position.y = mousePos.y + offset.y;
            }
            
            // If tooltip would go off the left edge, position it to the right
            if (position.x < -canvasSize.x * 0.5f)
            {
                position.x = mousePos.x + offset.x;
            }
            
            // Final bounds clamping to ensure tooltip stays on screen
            position.x = Mathf.Clamp(position.x, -canvasSize.x * 0.5f + 5f, canvasSize.x * 0.5f - tooltipSize.x - 5f);
            position.y = Mathf.Clamp(position.y, -canvasSize.y * 0.5f + 5f, canvasSize.y * 0.5f - tooltipSize.y - 5f);
            
            return position;
        }
        
        private void Update()
        {
            // Update position if tooltip is visible and mouse moves
            if (tooltipPanel != null && tooltipPanel.activeInHierarchy && isHovering)
            {
                // Get the canvas to convert mouse position properly
                Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
                if (canvas == null) return;
                
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
                
                if (canvasRect == null || tooltipRect == null) return;
                
                // Convert mouse position to canvas space
                Vector2 mousePos = Input.mousePosition;
                Vector2 canvasMousePos;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, mousePos, canvas.worldCamera, out canvasMousePos))
                {
                    // Recalculate smart position for smooth following
                    Vector2 tooltipSize = tooltipRect.sizeDelta;
                    Vector2 canvasSize = canvasRect.sizeDelta;
                    Vector2 newPosition = CalculateSmartPosition(canvasMousePos, tooltipSize, canvasSize);
                    
                    // Smoothly move to new position
                    tooltipRect.localPosition = Vector2.Lerp(tooltipRect.localPosition, newPosition, Time.deltaTime * 8f);
                }
            }
        }
    }
}
