using System.Windows;
using System.Windows.Controls;

namespace DeskNotes.Abstractions;

public static class AddonUiHelper
{
    public static TextBlock CreateDescription(string text) =>
        new()
        {
            Text = text,
            Style = TryStyle("SettingsDescription"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };

    public static TextBlock CreateFieldLabel(string text) =>
        new()
        {
            Text = text,
            Style = TryStyle("Text.Caption"),
            Margin = new Thickness(0, 0, 0, 6)
        };

    public static CheckBox CreateCheckBox(string content, bool isChecked, Action<bool> onChanged)
    {
        var checkBox = new CheckBox
        {
            Content = content,
            IsChecked = isChecked,
            Style = TryStyle("SettingsCheckBox")
        };
        checkBox.Checked += (_, _) => onChanged(true);
        checkBox.Unchecked += (_, _) => onChanged(false);
        return checkBox;
    }

    public static Button CreatePrimaryButton(string content, RoutedEventHandler onClick, Thickness? margin = null)
    {
        var button = new Button
        {
            Content = content,
            Style = TryStyle("SettingsPrimaryButton"),
            Margin = margin ?? new Thickness(0, 0, 0, 8)
        };
        button.Click += onClick;
        return button;
    }

    public static Button CreateSecondaryButton(string content, RoutedEventHandler onClick, Thickness? margin = null)
    {
        var button = new Button
        {
            Content = content,
            Style = TryStyle("SettingsSecondaryButton"),
            Margin = margin ?? new Thickness(0)
        };
        button.Click += onClick;
        return button;
    }

    public static ComboBox CreateComboBox() =>
        new()
        {
            Style = TryStyle("SettingsComboBox"),
            Margin = new Thickness(0, 0, 0, 8)
        };

    private static Style? TryStyle(string key)
    {
        try
        {
            return (Style)Application.Current.FindResource(key);
        }
        catch
        {
            return null;
        }
    }
}