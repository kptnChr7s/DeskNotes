using DeskNotes.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace DeskNotes.Controls;

public class MarkdownTextBlock : TextBlock
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(string),
            typeof(MarkdownTextBlock),
            new PropertyMetadata(string.Empty, OnSourceChanged));

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public MarkdownTextBlock()
    {
        TextWrapping = TextWrapping.Wrap;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownTextBlock block)
            MarkdownRenderer.Apply(block, block.Source);
    }
}