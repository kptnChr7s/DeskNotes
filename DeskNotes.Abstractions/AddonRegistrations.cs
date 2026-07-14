using System.Windows;

namespace DeskNotes.Abstractions;

public sealed class AddonTrayMenuItem
{
    public required string Label { get; init; }
    public required Action Action { get; init; }
    public int Order { get; init; } = 100;
}

public sealed class AddonSettingsSection
{
    public required string Title { get; init; }
    public required UIElement Content { get; init; }
    public int Order { get; init; } = 100;
}