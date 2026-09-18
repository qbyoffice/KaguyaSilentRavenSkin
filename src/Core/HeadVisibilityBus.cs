using System;

public enum CharacterMode
{
    Live = 0,     
    LiveCat = 1,  
    Nsfw = 2,    
    NsfwCat = 3  
}

public static class HeadVisibilityBus
{
    private static CharacterMode _currentMode = CharacterMode.Live;

    public static event Action<CharacterMode> OnModeChanged;

    public static CharacterMode CurrentMode => _currentMode;

    public static void SetMode(CharacterMode mode)
    {
        if (_currentMode != mode)
        {
            _currentMode = mode;
            OnModeChanged?.Invoke(_currentMode);
        }
    }
    
    public static bool IsHidden => _currentMode != CharacterMode.Live;
}