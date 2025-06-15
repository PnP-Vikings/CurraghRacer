using UnityEngine;
using UnityEngine.UIElements;

public class GameEvents : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // game state
    private int _count = 0;
    private int _multiplier = 1;
    private int _gameCount = 0;

    // UI references
    private Label _countLabel;
    private Label _multiplierLabel;
    private Button _increaseBtn;
    private Button _multiplierBtn;
    private Button _restartBtn;
    
    

    void OnEnable()
    {
        // grab root
        var root = uiDocument.rootVisualElement;

        // find our Labels (make sure your UXML has these names!)
        _countLabel      = root.Q<Label>("ButtonCountLabel");
        _multiplierLabel = root.Q<Label>("MultiplierLabel");

        // find our Buttons (again, match the name=… in your UXML)
        _increaseBtn   = root.Q<Button>("buttonIncrease");
        _multiplierBtn = root.Q<Button>("buttonMultiplier");
        _restartBtn    = root.Q<Button>("buttonRestart");

        // wire up callbacks
        _increaseBtn.clicked   += OnIncrease;
        _multiplierBtn.clicked += OnIncreaseMultiplier;
        _restartBtn.clicked    += OnRestart;

        // initial UI
        RefreshUI();
    }

    void OnDisable()
    {
        // unregister to avoid leaks
        _increaseBtn.clicked   -= OnIncrease;
        _multiplierBtn.clicked -= OnIncreaseMultiplier;
        _restartBtn.clicked    -= OnRestart;
    }

    private void OnIncrease()
    {
        _count += _multiplier;
        RefreshUI();
    }

    private void OnIncreaseMultiplier()
    {
        if (AdsManager.Instance?.rewardedAds == null)
        {
            Debug.LogError("RewardedAds reference is missing!");
            return;
        }

        AdsManager.Instance.rewardedAds.ShowRewardedAd();

        if (GameManager.Instance != null && GameManager.Instance.isRewarded)
        {
            _multiplier++;
            GameManager.Instance.isRewarded = false;
        }

        RefreshUI();
    }

    private void OnRestart()
    {
        _count = 0;
        _multiplier = 1;
        _gameCount +=1;
        if(_gameCount % 3 == 0)
        {
            AdsManager.Instance.interstitialAds.ShowInterstitialAd();
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        _countLabel.text      = $"Count: {_count}";
        _multiplierLabel.text = $"Multiplier: {_multiplier}";
    }
    
    
}