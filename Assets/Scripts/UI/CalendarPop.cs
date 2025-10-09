using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CalendarPop : MonoBehaviour,IPointerExitHandler, IPointerEnterHandler{
    Vector3 _base; float _t; bool _down; bool _hover;
    void Awake(){ _base = transform.localScale; }
    void Update(){
        var target = _down ? _base * 0.96f : (_hover ? _base * 1.05f : _base);
        _t = Mathf.MoveTowards(_t, 1f, Time.unscaledDeltaTime * 12f);
        transform.localScale = Vector3.Lerp(transform.localScale, target, _t);
    }
    
    void OnDisable(){ _down = false; _hover = false; _t = 0f; transform.localScale = _base; StopAllCoroutines(); }
    
 
    public void OnPointerExit(PointerEventData e){ _down = false; _hover = false; _t = 0f; }
    public void OnPointerEnter(PointerEventData e){ _hover = true; _t = 0f; }
}