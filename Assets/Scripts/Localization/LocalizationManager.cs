using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public bool isActive = false;
    public UnityEvent OnLanguageChanged;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    
    public void ChangeLocale(int _localeID)
    {
        if (isActive)
            return;
        StartCoroutine(SetLocale(_localeID));
    }
    
    
    IEnumerator SetLocale(int _localeID)
    {
       isActive= true;
       yield return LocalizationSettings.InitializationOperation;
       var selectedLocale = LocalizationSettings.AvailableLocales.Locales[_localeID];
       if (selectedLocale != null)
       {
           LocalizationSettings.SelectedLocale = selectedLocale;
           Debug.Log($"Current Locale: {selectedLocale.Identifier.Code}");
       }
       else
       {
           Debug.LogWarning("No locale is currently selected.");
       }
       isActive = false;
       OnLanguageChanged?.Invoke();
    }
}
