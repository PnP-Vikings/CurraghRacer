using UnityEngine;

public class RockVisual : MonoBehaviour
{
    GameObject rockVisualObject;
    
    public void Initialize(Rock.RockType rockType)
    {
        rockVisualObject = this.gameObject;
        switch (rockType)
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
        }
    }
}
