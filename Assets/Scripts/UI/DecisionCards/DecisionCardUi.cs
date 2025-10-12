using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DecisionCardUi : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Swipe Settings")]
    private float swipeThreshold = 30f;
    [SerializeField] private float rotationStrength = 0.3f;
    [SerializeField] private float swipeSpeed = 5f;
    public DecisionCardUiMaster decisionCardUiMaster;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private bool isSwiping = false;
    
    public TMPro.TMP_Text cardTitleText, cardDescriptionText;
    public TMPro.TMP_Text acceptText, rejectText;
    public Image  cardImage;  
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        decisionCardUiMaster = GetComponentInParent<DecisionCardUiMaster>();
        /*// Force center anchor and pivot
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);*/

        if (decisionCardUiMaster != null)
        {
            swipeThreshold = decisionCardUiMaster.swipeThreshold;
        }
         if(acceptText != null)
         {
             acceptText.text = "Spent €60 \n On Buying \n The lads a round\n may improve team morale";
             acceptText.gameObject.SetActive(false);
         }
        if(rejectText != null)
        {
            rejectText.text = "Save €60 \n By Not Buying \n The lads a round \n but lose team morale";
            rejectText.gameObject.SetActive(false);
        }
         
        // Now store the position
        originalPosition = rectTransform.anchoredPosition;
    }
    
    public void SetCardData(string title, string description, Sprite image)
    {
        if (cardTitleText != null) cardTitleText.text = title;
        if (cardDescriptionText != null) cardDescriptionText.text = description;
        if (cardImage != null) cardImage.sprite = image;
    }
    
    public void HideDecisionText()
    {
        if (acceptText != null) acceptText.gameObject.SetActive(false);
        if (rejectText != null) rejectText.gameObject.SetActive(false);
    }
    
    public void ShowAcceptText()
    {
        if (acceptText != null) acceptText.gameObject.SetActive(true);
        if (rejectText != null) rejectText.gameObject.SetActive(false);
    }
    
    public void ShowRejectText()
    {
        if (acceptText != null) acceptText.gameObject.SetActive(false);
        if (rejectText != null) rejectText.gameObject.SetActive(true);
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        isSwiping = true;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isSwiping) return;
        
        if (decisionCardUiMaster != null)
        {
            decisionCardUiMaster.DragStarted(this);
        }
        // Move card with drag
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        // Rotate card based on horizontal movement
        float rotation = rectTransform.anchoredPosition.x * rotationStrength *-1;
        rectTransform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isSwiping = false;
        float swipeDistance = rectTransform.anchoredPosition.x;
        
        if (Mathf.Abs(swipeDistance) >= swipeThreshold)
        {
            // Swiped far enough - accept or reject
            if (swipeDistance > 0)
                OnSwipeRight(); // Accept/Hire
            else
                OnSwipeLeft(); // Reject
        }
        else
        {
            // Return to original position
            ResetPosition();
        }
    }
    
    private void OnSwipeRight()
    {
        // Hire the racer
        Debug.Log("Swiped Right - Hire!");
        AnimateOffScreen(Vector2.right);
    }
    
    private void OnSwipeLeft()
    {
        // Reject the racer
        Debug.Log("Swiped Left - Reject!");
        AnimateOffScreen(Vector2.left);
    }
    
    private void AnimateOffScreen(Vector2 direction)
    {
        StartCoroutine(AnimateOffScreenCoroutine(direction));
    }

    private System.Collections.IEnumerator AnimateOffScreenCoroutine(Vector2 direction)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 targetPos = new Vector2(direction.x * 2000f, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); // EaseInBack approximation
        
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(gameObject);
        // Load next card
    }

    private void ResetPosition()
    {
        StartCoroutine(ResetPositionCoroutine());
    }

    private System.Collections.IEnumerator ResetPositionCoroutine()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        Quaternion startRot = rectTransform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI * 0.5f); // EaseOutBack approximation
        
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            rectTransform.rotation = Quaternion.Lerp(startRot, Quaternion.identity, t);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.rotation = Quaternion.identity;
    }
}