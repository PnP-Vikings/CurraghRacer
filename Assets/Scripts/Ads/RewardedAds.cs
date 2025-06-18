using UnityEngine;
using UnityEngine.Advertisements;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidAdUnitId;
    [SerializeField] private string iosAdUnitId;
    
    private string adUnitId;
    
    private void Awake()
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            adUnitId = iosAdUnitId;
        }
        else
        {
            adUnitId = androidAdUnitId;
        }
    }
    
    public void LoadRewardedAd()
    {
        Advertisement.Load(adUnitId,this);
    }
    
    public void ShowRewardedAd()
    {
        Advertisement.Show(adUnitId,this);
        LoadRewardedAd();
    }
    public void OnUnityAdsAdLoaded(string placementId)
    {
        Debug.Log($"Reward ad loaded: {placementId}");
    }
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Reward ad failed to load: {placementId} - Error: {error} - Message: {message}");
    }
    
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
      
    }
    public void OnUnityAdsShowStart(string placementId)
    {
      
    }
    public void OnUnityAdsShowClick(string placementId)
    {
     
    }
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
       if(placementId == adUnitId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
       {
           Debug.Log($"Reward ad shown: {placementId}");
           // Grant reward to the user
           GameManager.Instance.isRewarded = true;

          GameManager.Instance.Sleep(30);
         
           
           
       }
    }
}
