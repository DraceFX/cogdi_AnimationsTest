using System.IO;
using UnityEngine;

public static class AdbPathResolver
{
    public static string Resolve(string overridePath)
    {
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath)) return overridePath;

        var exe = GetExecutableName();
        var inStreaming = Path.Combine(Application.streamingAssetsPath, "platform-tools", exe);
        if (File.Exists(inStreaming)) return inStreaming;

        var nextToExe = Path.Combine(GetBuildFolder(), "platform-tools", exe);
        if (File.Exists(nextToExe)) return nextToExe;

        Debug.LogWarning("adb.exe не найден в StreamingAssets или рядом с билдом — пробуем PATH.");
        return exe;
    }

    private static string GetExecutableName()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return "adb.exe";
#else
        return "adb";
#endif
    }

    private static string GetBuildFolder() => Path.GetDirectoryName(Application.dataPath);
}