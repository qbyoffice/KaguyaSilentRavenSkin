using Godot;

public enum CharacterMode
{
    Live = 0,
    LiveCat = 1,
    Nsfw = 2,
    NsfwCat = 3
}

public static class HeadVisibilityBus
{
    private const string ModeJsonPath =
        "user://Kugaya.skin/KaguyaSilentRavenSkin/kaguyaMode.json";

    private static CharacterMode _currentMode = CharacterMode.Live;
    private static bool _initialized = false;

    public static event Action<CharacterMode> OnModeChanged;

    public static CharacterMode CurrentMode => _currentMode;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        _currentMode = LoadModeFromJson();
    }

    public static void SetMode(CharacterMode mode)
    {
        if (_currentMode != mode)
        {
            _currentMode = mode;
            OnModeChanged?.Invoke(_currentMode);
        }
    }

    public static bool IsHidden => _currentMode != CharacterMode.Live;

    private static CharacterMode LoadModeFromJson()
    {
        if (!Godot.FileAccess.FileExists(ModeJsonPath)) return CharacterMode.Live;

        using var file = Godot.FileAccess.Open(ModeJsonPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null) return CharacterMode.Live;

        string content = file.GetAsText();
        if (string.IsNullOrWhiteSpace(content)) return CharacterMode.Live;

        var json = new Json();
        if (json.Parse(content) != Error.Ok) return CharacterMode.Live;

        var data = json.Data;
        if (data.VariantType != Variant.Type.Dictionary) return CharacterMode.Live;

        var dict = data.AsGodotDictionary();
        if (dict.ContainsKey("mode"))
        {
            var mv = dict["mode"];
            if (mv.VariantType == Variant.Type.Int || mv.VariantType == Variant.Type.Float)
            {
                int m = (int)mv;
                if (m >= 0 && m <= 3) return (CharacterMode)m;
            }
        }
        return CharacterMode.Live;
    }
}