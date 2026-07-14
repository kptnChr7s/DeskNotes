using DeskNotes.Abstractions;
using DeskNotes.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeskNotes.Views;

public partial class SettingsWindow : Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(
        bool topMost,
        bool hotkeyRegistered,
        string? hotkeyError,
        IReadOnlyList<AddonSettingsSection> addonSections,
        IReadOnlyList<AddonManifest> loadedAddons,
        Action<bool>? onTopMostChanged = null,
        Action<bool>? onAutoStartChanged = null)
    {
        InitializeComponent();

        ViewModel = new SettingsViewModel(
            topMost,
            hotkeyRegistered,
            hotkeyError,
            loadedAddons,
            onTopMostChanged,
            onAutoStartChanged);

        DataContext = ViewModel;
        BuildAddonSections(addonSections);
    }

    private void BuildAddonSections(IReadOnlyList<AddonSettingsSection> sections)
    {
        foreach (var section in sections)
        {
            var label = new TextBlock
            {
                Text = section.Title,
                Style = (Style)FindResource("Text.SectionLabel")
            };
            AddonSectionsHost.Children.Add(label);

            var card = new Border
            {
                Style = (Style)FindResource("SettingsCard"),
                Child = section.Content
            };
            AddonSectionsHost.Children.Add(card);
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}