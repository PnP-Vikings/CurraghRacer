using UnityEngine;

public class StackAnimEvents : MonoBehaviour
{
  // deact animator/animations to save resources
  public void FinishedStackPlacement()
  {
    //Debug.Log("Placed");
    this.gameObject.GetComponent<Animator>().enabled = false;
  }
}
