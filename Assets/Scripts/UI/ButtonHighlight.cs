using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
public class ButtonHighlight : MonoBehaviour , IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{

   [SerializeField] GameObject highlight;
    
    void Start()
    {
        highlight = transform.Find("Highlight").gameObject;
        SetHighlightActive(false);
    }
    IEnumerator ClickRoutine(){
        
        yield return new WaitForSeconds(0.1f);
        SetHighlightActive(false);
    }
    
    public void SetHighlightActive(bool active)
    {
        if(highlight != null)
            highlight.SetActive(active);
    }

    public void OnPointerDown(PointerEventData e)
    {
        SetHighlightActive(true);
        StartCoroutine(ClickRoutine());
    }
    public void OnPointerUp(PointerEventData e){ SetHighlightActive(false); }
    public void OnPointerExit(PointerEventData e){ SetHighlightActive(false); }
    public void OnPointerEnter(PointerEventData e){ SetHighlightActive(true); }
    
    void OnDisable(){ StopAllCoroutines(); }
}
