using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class ButtonPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler{
    Vector3 _base; float _t; bool _down; bool _hover;
    void Awake(){ _base = transform.localScale; }
    void Update(){
        var target = _down ? _base * 0.96f : (_hover ? _base * 1.05f : _base);
        _t = Mathf.MoveTowards(_t, 1f, Time.unscaledDeltaTime * 12f);
        transform.localScale = Vector3.Lerp(transform.localScale, target, _t);
    }
    
    void OnDisable(){ _down = false; _hover = false; _t = 0f; transform.localScale = _base; StopAllCoroutines(); }
    
    IEnumerator ClickRoutine(){
        _down = true; _t = 0f;
        yield return new WaitForSeconds(0.1f);
        _down = false; _t = 0f;
    }
    public void OnPointerDown(PointerEventData e){ _down = true; _t = 0f; StartCoroutine(ClickRoutine()); }
    public void OnPointerUp(PointerEventData e){ _down = false; _t = 0f; }
    public void OnPointerExit(PointerEventData e){ _down = false; _hover = false; _t = 0f; }
    public void OnPointerEnter(PointerEventData e){ _hover = true; _t = 0f; }
}