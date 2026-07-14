namespace DeskNotes.Abstractions;

public sealed class AddonNote
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string AccentColor { get; init; } = "#5FA8FF";
}