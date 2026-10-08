using UnityEngine;

public class SpawnedItemsParent : MonoBehaviour
{
   [SerializeField] private StoreManager storeManager;
   
   public StoreManager GetStoreManager()
   {
      return storeManager;
   }

   public void SetStoreManager(StoreManager storeManager)
   {
      this.storeManager = storeManager;
   }
}
