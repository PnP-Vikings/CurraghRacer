using UnityEngine;
using UnityEngine.Advertisements;

public class BannerAds : MonoBehaviour
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
        
        Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
    }
    
    
    public void LoadBannerAd()
    {
        BannerLoadOptions options = new BannerLoadOptions
        {
            loadCallback = BannerLoaded,
            errorCallback = BannerLoadedError
        };
        
        Advertisement.Banner.Load(adUnitId, options);
    }
    
    public void ShowBannerAd()
    {
      BannerOptions options = new BannerOptions
      {
          showCallback = BannerShown,
          clickCallback = BannerClicked,
          hideCallback = BannerHidden
      };
    }
    
    public void HideBannerAd()
    {
        Advertisement.Banner.Hide();
    }
    
    private void BannerShown()
    {
        Debug.Log("Banner ad shown");
    }
    
    private void BannerClicked()
    {
        Debug.Log("Banner ad clicked");
    }
    private void BannerHidden()
    {
        Debug.Log("Banner ad hidden");
    }
    
    private void BannerLoadedError(string message)
    {
        Debug.Log($"Banner ad failed to load: {message}");
    }
    
    private void BannerLoaded()
    {
        Debug.Log("Banner ad loaded");
        Advertisement.Banner.Show(adUnitId);
    }
}
