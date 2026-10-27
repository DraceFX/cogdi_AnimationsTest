using System;
using System.Collections.Generic;
using System.Linq;

public static class AdbOutputParsers
{
    public static List<HeadsetDevice> ParseDevicesList(string output)
    {
        var list = new List<HeadsetDevice>();
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("List of devices") || line.StartsWith("*")) continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[1] != "device") continue;

            var modelPart = parts.FirstOrDefault(p => p.StartsWith("model:"));
            var model = modelPart?.Substring("model:".Length) ?? "Unknown";

            list.Add(new HeadsetDevice
            {
                Serial = parts[0],
                Model = model,
                DisplayModel = HeadsetModelCatalog.ResolveDisplayName(model)
            });
        }
        return list;
    }

    public static int ParseBatteryLevel(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var t = line.Trim();
            if (!t.StartsWith("level:")) continue;
            if (int.TryParse(t.Substring("level:".Length).Trim(), out int level)) return level;
        }
        return -1;
    }

    public static (long total, long free) ParseDf(string output)
    {
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("Filesystem")) continue;

            if (line.IndexOf("/data", StringComparison.Ordinal) < 0 && line.IndexOf("/storage/emulated/0", StringComparison.Ordinal) < 0) continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            if (long.TryParse(parts[1], out long totalKb) && long.TryParse(parts[3], out long freeKb) && totalKb > 0) return (totalKb * 1024, freeKb * 1024);
        }
        return (0, 0);
    }

    public static (long total, long free) ParseStatFs(string output)
    {
        long blockSize = 0, totalBlocks = 0, freeBlocks = 0;

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (line.StartsWith("Block size:"))
            {
                var after = line.Substring("Block size:".Length).Trim();
                var numStr = new string(after.TakeWhile(char.IsDigit).ToArray());
                long.TryParse(numStr, out blockSize);
            }
            else if (line.StartsWith("Blocks:"))
            {
                var tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length - 1; i++)
                {
                    if (tokens[i].Equals("Total:", StringComparison.OrdinalIgnoreCase))
                    {
                        long.TryParse(tokens[i + 1].TrimEnd(','), out totalBlocks);
                    }
                    else if (tokens[i].Equals("Free:", StringComparison.OrdinalIgnoreCase))
                    {
                        long.TryParse(tokens[i + 1].TrimEnd(','), out freeBlocks);
                    }
                }
            }
        }

        if (blockSize > 0 && totalBlocks > 0) return (totalBlocks * blockSize, freeBlocks * blockSize);
        return (0, 0);
    }
}