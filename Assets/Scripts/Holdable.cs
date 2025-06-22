using UnityEngine;

public interface Holdable
{
   public void SetPositionOnHold(Vector3 position);
   
   public Vector3 GetPositionOnHold(out Vector3 position);

}
