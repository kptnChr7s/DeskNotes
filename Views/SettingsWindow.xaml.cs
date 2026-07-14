using DeskNotes.Abstractions;
using DeskNotes.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        AddonSectionsHost.Children.Clear();

        foreach (var section in sections)
        {
            DetachFromParent(section.Content);

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

    private static void DetachFromParent(UIElement element)
    {
        if (element is not FrameworkElement frameworkElement)
            return;

        switch (frameworkElement.Parent)
        {
            case System.Windows.Controls.Panel panel:
                panel.Children.Remove(element);
                break;
            case Decorator decorator:
                decorator.Child = null;
                break;
            case ContentControl contentControl:
                contentControl.Content = null;
                break;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}