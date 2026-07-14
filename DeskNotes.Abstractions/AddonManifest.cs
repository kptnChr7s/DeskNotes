namespace DeskNotes.Abstractions;

public sealed class AddonManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = "DeskNotes";

    public static AddonManifest Create(string id, string name, string version, string description = "") =>
        new()
        {
            Id = id,
            Name = name,
            Version = version,
            Description = description
        };
}