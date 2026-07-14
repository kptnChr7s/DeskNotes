namespace DeskNotes.Abstractions;

public interface IDeskNotesAddon
{
    AddonManifest Manifest { get; }
    Task InitializeAsync(IAddonContext context);
    Task ShutdownAsync();
}