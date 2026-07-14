using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace DeskNotes.Models;

public partial class TodoItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string text = string.Empty;

    [ObservableProperty]
    private bool isCompleted;

    [ObservableProperty]
    private DateTime createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime? completedAt;

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private string accentColor = "#5FA8FF";

    [JsonIgnore]
    public string? EditBackupText { get; set; }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletedAt = value ? DateTime.Now : null;
    }
}