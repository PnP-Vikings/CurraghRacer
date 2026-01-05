using DG.Tweening;
using UnityEngine;

public class RockVisual : MonoBehaviour
{
    GameObject rockVisualObject;
    public bool onHovered = false;
    public bool onSelected = false;
    public Rock rockData;
    Sequence hoverSequence;
    public void Initialize(Rock.RockType rockType, Rock Data)
    {
        rockData = Data;
        rockVisualObject = this.gameObject;
        /*switch (rockType)
        {
            case Rock.RockType.Small:
                rockVisualObject.transform.localScale = Vector3.one * 1f;
                rockVisualObject.GetComponent<Renderer>().material.color = Color.gray;
                break;
            case Rock.RockType.Medium:
                rockVisualObject.transform.localScale = Vector3.one * 2f;
                rockVisualObject.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case Rock.RockType.Large:
                rockVisualObject.transform.localScale = Vector3.one * 3f;
                rockVisualObject.GetComponent<Renderer>().material.color = Color.black;
                break;
        }*/
        
        
    }
    
    public void RockIsHovered()
    {
        onHovered = true;
        hoverSequence = DOTween.Sequence();
        
        hoverSequence.Append(rockVisualObject.transform.DOScale(rockVisualObject.transform.localScale * 1.2f, 0.3f).SetEase(Ease.OutBack))
            .Append(rockVisualObject.transform.DORotate(new Vector3(0, 360, 0), 1f, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear));
    }
    
    public void RockIsSelected()
    {
        onSelected = true;
    }
}
