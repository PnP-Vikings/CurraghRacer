using UnityEngine;

/// <summary>
/// Simple save system for game settings using PlayerPrefs.
/// Settings are saved automatically and persist across all game sessions.
/// </summary>
public static class SettingsSaveSystem
{
    // Keys for PlayerPrefs
    private const string MASTER_VOLUME_KEY = "Settings_MasterVolume";
    private const string SETTINGS_INITIALIZED_KEY = "Settings_Initialized";
    private const string LANGUAGE_KEY = "Settings_LanguageIndex";

    /// <summary>
    /// Saves the master volume setting
    /// </summary>
    public static void SaveMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save(); // Force write to disk immediately
    }

    /// <summary>
    /// Loads the master volume setting. Returns 1.0 (full volume) if not set.
    /// </summary>
    public static float LoadMasterVolume()
    {
        return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
    }

    /// <summary>
    /// Saves the selected language index
    /// </summary>
    public static void SaveLanguage(int localeIndex)
    {
        PlayerPrefs.SetInt(LANGUAGE_KEY, localeIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads the selected language index. Returns -1 if not set.
    /// </summary>
    public static int LoadLanguage()
    {
        return PlayerPrefs.GetInt(LANGUAGE_KEY, -1);
    }

    /// <summary>
    /// Checks if settings have been initialized before
    /// </summary>
    public static bool HasSavedSettings()
    {
        return PlayerPrefs.HasKey(SETTINGS_INITIALIZED_KEY);
    }

    /// <summary>
    /// Marks settings as initialized
    /// </summary>
    public static void MarkSettingsInitialized()
    {
        PlayerPrefs.SetInt(SETTINGS_INITIALIZED_KEY, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Resets all settings to default values
    /// </summary>
    public static void ResetToDefaults()
    {
        SaveMasterVolume(1.0f);
        PlayerPrefs.DeleteKey(LANGUAGE_KEY);
        PlayerPrefs.Save();
        Debug.Log("Settings reset to defaults");
    }

    /// <summary>
    /// Deletes all saved settings
    /// </summary>
    public static void DeleteAllSettings()
    {
        PlayerPrefs.DeleteKey(MASTER_VOLUME_KEY);
        PlayerPrefs.DeleteKey(SETTINGS_INITIALIZED_KEY);
        PlayerPrefs.DeleteKey(LANGUAGE_KEY);
        PlayerPrefs.Save();
        Debug.Log("All settings deleted");
    }
}

