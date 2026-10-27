using System;
using System.Collections.Generic;

public static class HeadsetModelCatalog
{
    private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Pico
        { "A9210", "Pico 4 Ultra" },
        { "A8110", "Pico 4" },
        { "A7H10", "Pico Neo 3" },
        { "A7H11", "Pico Neo 3 Link" },
        { "A8S10", "Pico G3" },

        // Meta
        { "Quest 3",   "Meta Quest 3" },
        { "Quest 3S",  "Meta Quest 3S" },
        { "Quest 2",   "Meta Quest 2" },
        { "Quest Pro", "Meta Quest Pro" },
    };

    public static string ResolveDisplayName(string model)
    {
        if (string.IsNullOrEmpty(model)) return "Unknown";
        return Names.TryGetValue(model, out var pretty) ? pretty : model;
    }
}