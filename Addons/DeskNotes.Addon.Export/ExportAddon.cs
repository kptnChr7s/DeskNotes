using DeskNotes.Abstractions;
using System.Windows;
using System.Windows.Controls;

namespace DeskNotes.Addon.Export;

public sealed class ExportAddon : IDeskNotesAddon
{
    private readonly ExportService _exportService = new();
    private IAddonContext? _context;

    public AddonManifest Manifest { get; } = AddonManifest.Create(
        "desknotes.export",
        "Export",
        "1.0.0",
        "Notizen als JSON oder Markdown exportieren");

    public Task InitializeAsync(IAddonContext context)
    {
        _context = context;

        context.RegisterTrayMenuItem("Notizen exportieren (JSON)...", () => ExportJson(), order: 20);
        context.RegisterTrayMenuItem("Notizen exportieren (Markdown)...", () => ExportMarkdown(), order: 21);
        context.RegisterSettingsSection("EXPORT", BuildSettingsPanel(), order: 50);

        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;

    private void ExportJson()
    {
        if (_context == null) return;
        _exportService.ExportJson(_context.GetNotes());
    }

    private void ExportMarkdown()
    {
        if (_context == null) return;
        _exportService.ExportMarkdown(_context.GetNotes());
    }

    private UIElement BuildSettingsPanel()
    {
        var panel = new StackPanel();

        var info = new TextBlock
        {
            Text = "Alle Notizen in eine Datei exportieren.",
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        panel.Children.Add(info);

        var jsonButton = new Button
        {
            Content = "Als JSON exportieren",
            Height = 36,
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        jsonButton.Click += (_, _) => ExportJson();
        panel.Children.Add(jsonButton);

        var mdButton = new Button
        {
            Content = "Als Markdown exportieren",
            Height = 36,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        mdButton.Click += (_, _) => ExportMarkdown();
        panel.Children.Add(mdButton);

        return panel;
    }
}