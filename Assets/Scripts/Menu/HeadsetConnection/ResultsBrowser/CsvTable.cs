using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

public static class CsvTable
{
    public static string FormatAsText(string raw, int maxChars = 16000, int columnWidth = 20)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "(пусто)";

        if (raw.Length > maxChars)
            raw = raw.Substring(0, maxChars) + "\n\n… (файл обрезан)";

        var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return "(нет данных)";

        // Основной разделитель: если в файле есть ';' — используем ';', иначе ','
        char sep = raw.IndexOf(';') >= 0 ? ';' : ',';

        var sb = new StringBuilder();
        var csvBuffer = new List<List<string>>();

        void FlushCsv()
        {
            if (csvBuffer.Count == 0) return;

            foreach (var row in csvBuffer)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append(row[i]);
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            csvBuffer.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.IndexOf(sep) < 0)
            {
                // Не CSV-строка: свободный текст, выводим как есть
                FlushCsv();
                sb.AppendLine(line);
            }
            else
            {
                csvBuffer.Add(ParseLine(line, sep));
            }
        }

        FlushCsv();

        return sb.ToString();
    }

    private static List<string> ParseLine(string line, char sep)
    {
        var cells = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == sep && !inQuotes)
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        cells.Add(sb.ToString().Trim());
        return cells;
    }
}
