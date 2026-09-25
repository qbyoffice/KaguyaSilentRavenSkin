using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SilentSkinMod.Core.Startup;

internal static class StartupReminderSettings
{
    internal const string SaveDirectory = "user://Kugaya.skin/KaguyaSilentRavenSkin";

    internal static bool IsDisabled(string directory)
    {
        var data = ReadObject(Path.Combine(directory, "startupReminder.json"));
        return data["do_not_remind"]?.GetValue<bool>() == true;
    }

    internal static void Disable(string directory)
    {
        string path = Path.Combine(directory, "startupReminder.json");
        var data = ReadObject(path);
        data["version"] = 1;
        data["do_not_remind"] = true;
        WriteObject(path, data);
    }

    internal static void SwitchToLive(string directory)
    {
        string path = Path.Combine(directory, "kaguyaMode.json");
        var data = ReadObject(path);
        data["version"] = 2;
        data["mode"] = 0;
        data["hide_mask"] = false;
        data["hide_anquanku"] = false;
        WriteObject(path, data);
    }

    private static JsonObject ReadObject(string path)
    {
        if (!File.Exists(path)) return new JsonObject();
        return JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new JsonException($"[KaguyaSilentRavenSkin]配置根节点必须是 JSON 对象：{path}");
    }

    private static void WriteObject(string path, JsonObject data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".startup.tmp";
        try
        {
            File.WriteAllText(temporary, data.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
