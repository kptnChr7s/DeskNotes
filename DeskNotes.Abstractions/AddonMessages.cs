namespace DeskNotes.Abstractions;

public sealed class NoteInputSubmitting
{
    public required string Text { get; init; }
    public bool CancelDefault { get; set; }
    public bool ClearInput { get; set; }
}

public sealed class NoteAdded
{
    public required AddonNote Note { get; init; }
}

public sealed class NoteDeleted
{
    public required AddonNote Note { get; init; }
}

public sealed class NoteCompleted
{
    public required AddonNote Note { get; init; }
}

public sealed class AppShutdown;

public sealed class SettingsOpening
{
    public IList<AddonSettingsSection> Sections { get; } = [];
}