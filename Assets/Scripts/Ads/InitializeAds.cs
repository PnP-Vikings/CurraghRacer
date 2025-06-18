using UnityEngine;
using UnityEngine.Advertisements;

public class InitializeAds : MonoBehaviour, IUnityAdsInitializationListener
{
   [SerializeField] private string androidGameId = "1234567";
   [SerializeField] private string iosGameId = "1234567";
   [SerializeField] private bool isTesting = true;
   private string gameId;
   
   private void Awake()
   {
       if (Application.platform == RuntimePlatform.IPhonePlayer)
       {
           gameId = iosGameId;
       }
       else
       {
           gameId = androidGameId;
       }
       
       if(!Advertisement.isInitialized && Advertisement.isSupported)
       {
           Advertisement.Initialize(gameId, isTesting,this);
           
       }
       else
       {
           Debug.Log("Ads already initialized or not supported");
       }
   }
   public void OnInitializationComplete()
   {
       Debug.Log("Ads Initialized");
   }
   public void OnInitializationFailed(UnityAdsInitializationError error, string message)
   {
    
   }
}
