using DeskNotes.Abstractions;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DeskNotes.Addon.Export;

internal sealed class ExportService
{
    public bool ExportJson(IReadOnlyList<AddonNote> notes)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Notizen als JSON exportieren",
            Filter = "JSON-Datei (*.json)|*.json",
            FileName = $"DeskNotes_{DateTime.Now:yyyy-MM-dd}.json"
        };

        if (dialog.ShowDialog() != true)
            return false;

        var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
        return true;
    }

    public bool ExportMarkdown(IReadOnlyList<AddonNote> notes)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Notizen als Markdown exportieren",
            Filter = "Markdown (*.md)|*.md",
            FileName = $"DeskNotes_{DateTime.Now:yyyy-MM-dd}.md"
        };

        if (dialog.ShowDialog() != true)
            return false;

        var sb = new StringBuilder();
        sb.AppendLine("# DeskNotes Export");
        sb.AppendLine();
        sb.AppendLine($"Exportiert am {DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine();

        foreach (var note in notes)
        {
            var box = note.IsCompleted ? "[x]" : "[ ]";
            sb.AppendLine($"- {box} {note.Text}");
            sb.AppendLine($"  - Erstellt: {note.CreatedAt:dd.MM.yyyy HH:mm}");
            if (note.CompletedAt.HasValue)
                sb.AppendLine($"  - Erledigt: {note.CompletedAt:dd.MM.yyyy HH:mm}");
            sb.AppendLine();
        }

        File.WriteAllText(dialog.FileName, sb.ToString());
        return true;
    }
}