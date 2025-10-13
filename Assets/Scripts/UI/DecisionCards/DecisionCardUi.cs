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
    
    // Track current card data
    private DecisionCard currentCard;
    private TeamMember currentTargetMember;
    
    public TMPro.TMP_Text cardTitleText, cardDescriptionText, affectedMemberText;
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
    
    /// <summary>
    /// Setup card with DecisionCard data for the Master
    /// </summary>
    public void SetupCard(DecisionCard card, TeamMember targetMember = null)
    {
        currentCard = card;
        currentTargetMember = targetMember;
        
        // Set card visuals
        if (card.cardImage != null && cardImage != null)
            cardImage.sprite = card.cardImage;
        
        if (cardTitleText != null)
            cardTitleText.text = card.cardTitle;
        
        // Format description with target member name if applicable
        string description = card.cardDescription;
        if (targetMember != null && description.Contains("{member}"))
        {
            description = description.Replace("{member}", targetMember.memberName);
        }
        
        if (cardDescriptionText != null)
            cardDescriptionText.text = description;
        
        // Set option text
        if (acceptText != null)
            acceptText.text = card.optionA.optionText;
        
        if (rejectText != null)
            rejectText.text = card.optionB.optionText;
        
        // Show affected member name if applicable
        if (affectedMemberText != null)
        {
            if (targetMember != null)
            {
                affectedMemberText.text = $"Affected: {targetMember.memberName}";
                affectedMemberText.gameObject.SetActive(true);
            }
            else
            {
                affectedMemberText.gameObject.SetActive(false);
            }
        }
        
        // Hide decision text initially
        HideDecisionText();
        
        // Reset position
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.rotation = Quaternion.identity;
        
    }
    
    /// <summary>
    /// Get the current card for the Master
    /// </summary>
    public DecisionCard GetCurrentCard()
    {
        return currentCard;
    }
    
    /// <summary>
    /// Clear card data and hide
    /// </summary>
    public void ClearCard()
    {
        currentCard = null;
        currentTargetMember = null;
        HideDecisionText();
        gameObject.SetActive(false);
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
        // Accept - Option A
        Debug.Log("Swiped Right - Accept!");
        
        if (currentCard != null && decisionCardUiMaster != null)
        {
            decisionCardUiMaster.OnCardDecision(this, currentCard.optionA, currentCard, currentTargetMember);
        }
        else
        {
            AnimateOffScreen(Vector2.right);
        }
    }
    
    private void OnSwipeLeft()
    {
        // Reject - Option B
        Debug.Log("Swiped Left - Reject!");
        
        if (currentCard != null && decisionCardUiMaster != null)
        {
            decisionCardUiMaster.OnCardDecision(this, currentCard.optionB, currentCard, currentTargetMember);
        }
        else
        {
            AnimateOffScreen(Vector2.left);
        }
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