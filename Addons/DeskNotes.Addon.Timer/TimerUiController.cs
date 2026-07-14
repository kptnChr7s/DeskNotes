using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfApp = System.Windows.Application;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfCursors = System.Windows.Input.Cursors;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfPanel = System.Windows.Controls.Panel;
using WinForms = System.Windows.Forms;

namespace DeskNotes.Addon.Timer;

internal sealed class TimerUiController : IDisposable
{
    private readonly Window _window;
    private readonly TimerService _service;
    private readonly TimerSettingsStore _settings;
    private readonly WinForms.NotifyIcon? _tray;

    private readonly ContentPresenter? _flyoutHost;
    private readonly WpfPanel? _statsPanel;

    private Border? _flyoutCard;
    private TextBlock? _minutesLabel;
    private readonly List<WpfButton> _presetButtons = [];
    private Border? _runningPill;
    private TextBlock? _runningTimeLabel;
    private Border? _progressFill;
    private WpfButton? _pauseButton;

    private string? _originalTrayText;
    private bool _clickOutsideHooked;

    public TimerUiController(
        Window window,
        TimerService service,
        TimerSettingsStore settings,
        WinForms.NotifyIcon? tray)
    {
        _window = window;
        _service = service;
        _settings = settings;
        _tray = tray;

        _flyoutHost = window.FindName("AddonTimerFlyoutHost") as ContentPresenter;
        _statsPanel = window.FindName("StatsPillPanel") as WpfPanel;

        _service.Changed += OnServiceChanged;
        _service.Completed += OnTimerCompleted;
    }

    public void OpenFlyout(int? minutes = null)
    {
        _service.OpenFlyout(minutes);
        ShowWindowIfNeeded();
    }

    private void OnServiceChanged() =>
        _window.Dispatcher.BeginInvoke(RefreshUi);

    private void RefreshUi()
    {
        if (_service.IsFlyoutOpen)
            ShowFlyout();
        else
            HideFlyout();

        if (_service.Phase is TimerPhase.Running or TimerPhase.Paused)
            ShowRunningPill();
        else
            HideRunningPill();

        UpdateTrayText();
    }

    private void ShowFlyout()
    {
        if (_flyoutHost == null)
            return;

        EnsureFlyoutBuilt();
        _flyoutHost.Content = _flyoutCard;
        UpdateFlyoutState();
        HookClickOutside();

        if (_flyoutCard != null)
            FadeIn(_flyoutCard);
    }

    private void HideFlyout()
    {
        UnhookClickOutside();

        if (_flyoutHost != null)
            _flyoutHost.Content = null;
    }

    private void EnsureFlyoutBuilt()
    {
        if (_flyoutCard != null)
            return;

        _flyoutCard = new Border
        {
            Background = Brush("BackgroundSecondary"),
            BorderBrush = Brush("BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(11),
            Padding = new Thickness(14, 12, 14, 12),
            MaxWidth = 420,
            HorizontalAlignment = WpfHorizontalAlignment.Left,
            Opacity = 0
        };

        var root = new StackPanel();

        var header = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "TIMER",
            Style = TryStyle("Text.SectionLabel"),
            Margin = new Thickness(2, 0, 0, 0)
        };
        Grid.SetColumn(title, 0);
        header.Children.Add(title);

        var closeButton = CreateIconButton("\uE711", "Schließen");
        closeButton.Click += (_, _) => _service.CloseFlyout();
        Grid.SetColumn(closeButton, 1);
        header.Children.Add(closeButton);
        root.Children.Add(header);

        var stepperRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        stepperRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stepperRow.ColumnDefinitions.Add(new ColumnDefinition());
        stepperRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        stepperRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var minus = CreateIconButton("\uE738", "1 Minute weniger");
        minus.Click += (_, _) => _service.AdjustMinutes(-1);
        Grid.SetColumn(minus, 0);
        stepperRow.Children.Add(minus);

        _minutesLabel = new TextBlock
        {
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("TextPrimary"),
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_minutesLabel, 1);
        stepperRow.Children.Add(_minutesLabel);

        var plus = CreateIconButton("\uE710", "1 Minute mehr");
        plus.Click += (_, _) => _service.AdjustMinutes(1);
        Grid.SetColumn(plus, 2);
        stepperRow.Children.Add(plus);

        var startButton = new WpfButton
        {
            Content = "Start",
            Height = 34,
            MinWidth = 72,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(14, 0, 14, 0),
            Background = Brush("AccentGradientBrush"),
            Foreground = WpfBrushes.White,
            BorderThickness = new Thickness(0),
            Cursor = WpfCursors.Hand,
            FontWeight = FontWeights.SemiBold
        };
        startButton.Click += (_, _) => _service.Start();
        Grid.SetColumn(startButton, 3);
        stepperRow.Children.Add(startButton);
        root.Children.Add(stepperRow);

        var presetsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
        foreach (var preset in TimerService.Presets)
        {
            var button = CreatePresetButton(preset);
            presetsWrap.Children.Add(button);
            _presetButtons.Add(button);
        }

        root.Children.Add(presetsWrap);
        _flyoutCard.Child = root;
    }

    private WpfButton CreatePresetButton(int minutes)
    {
        var button = new WpfButton
        {
            Content = minutes.ToString(),
            Height = 30,
            MinWidth = 44,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(10, 0, 10, 0),
            Background = WpfBrushes.Transparent,
            BorderBrush = Brush("CardBorder"),
            BorderThickness = new Thickness(1),
            Foreground = Brush("TextSecondary"),
            Cursor = WpfCursors.Hand,
            FontSize = 12.5,
            FontWeight = FontWeights.Medium,
            Tag = minutes
        };

        button.Click += (_, _) => _service.SetMinutes(minutes);
        return button;
    }

    private void UpdateFlyoutState()
    {
        if (_minutesLabel != null)
            _minutesLabel.Text = $"{_service.SelectedMinutes} min";

        foreach (var button in _presetButtons)
        {
            if (button.Tag is not int preset)
                continue;

            var active = preset == _service.SelectedMinutes;
            button.Background = active ? Brush("FilterChipActive") : WpfBrushes.Transparent;
            button.Foreground = active ? Brush("TextPrimary") : Brush("TextSecondary");
            button.BorderBrush = active ? Brush("AccentBlue") : Brush("CardBorder");
        }
    }

    private void ShowRunningPill()
    {
        if (_statsPanel == null)
            return;

        EnsureRunningPillBuilt();

        if (_runningPill!.Parent == null)
            _statsPanel.Children.Add(_runningPill);

        UpdateRunningPill();
    }

    private void HideRunningPill()
    {
        if (_runningPill?.Parent is WpfPanel panel)
            panel.Children.Remove(_runningPill);
    }

    private void EnsureRunningPillBuilt()
    {
        if (_runningPill != null)
            return;

        var pillStyle = TryStyle("StatPillBorder");
        _runningPill = new Border
        {
            Style = pillStyle,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 8, 4)
        };

        if (_service.IsLowTime)
            _runningPill.Background = Brush("BrandGoldSoft");

        var layout = new Grid { MinWidth = 150 };
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var progressTrack = new Border
        {
            Background = Brush("BorderSubtle"),
            CornerRadius = new CornerRadius(2, 2, 0, 0),
            Height = 3,
            HorizontalAlignment = WpfHorizontalAlignment.Stretch
        };
        Grid.SetRow(progressTrack, 0);

        var progressGrid = new Grid();
        _progressFill = new Border
        {
            Background = Brush("AccentGradientBrush"),
            HorizontalAlignment = WpfHorizontalAlignment.Left,
            Width = 0
        };
        progressGrid.Children.Add(_progressFill);
        progressTrack.Child = progressGrid;
        layout.Children.Add(progressTrack);

        var content = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 0)
        };
        Grid.SetRow(content, 1);

        var clock = new TextBlock
        {
            Text = "\uE121",
            FontFamily = new WpfFontFamily("Segoe MDL2 Assets"),
            FontSize = 11,
            Foreground = Brush("AccentYellow"),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(clock);

        _runningTimeLabel = new TextBlock
        {
            FontFamily = new WpfFontFamily("Segoe UI"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("TextPrimary"),
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(_runningTimeLabel);

        _pauseButton = CreateIconButton("\uE769", "Pause");
        _pauseButton.Margin = new Thickness(8, 0, 0, 0);
        _pauseButton.Click += (_, _) =>
        {
            if (_service.Phase == TimerPhase.Running)
                _service.Pause();
            else if (_service.Phase == TimerPhase.Paused)
                _service.Resume();
        };
        content.Children.Add(_pauseButton);

        var stopButton = CreateIconButton("\uE711", "Stop");
        stopButton.Margin = new Thickness(4, 0, 0, 0);
        stopButton.Click += (_, _) => _service.Stop();
        content.Children.Add(stopButton);

        layout.Children.Add(content);
        _runningPill.Child = layout;
    }

    private void UpdateRunningPill()
    {
        if (_runningTimeLabel == null || _runningPill == null || _progressFill == null || _pauseButton == null)
            return;

        _runningTimeLabel.Text = _service.FormattedRemaining;
        _pauseButton.ToolTip = _service.Phase == TimerPhase.Paused ? "Fortsetzen" : "Pause";
        _pauseButton.Content = new TextBlock
        {
            Text = _service.Phase == TimerPhase.Paused ? "\uE768" : "\uE769",
            FontFamily = new WpfFontFamily("Segoe MDL2 Assets"),
            FontSize = 11,
            Foreground = Brush("TextSecondary")
        };

        _runningPill.Background = _service.IsLowTime
            ? Brush("BrandGoldSoft")
            : Brush("BackgroundElevated");

        var trackWidth = 150.0;
        _progressFill.Width = Math.Max(0, trackWidth * _service.Progress);
    }

    private void OnTimerCompleted()
    {
        _window.Dispatcher.BeginInvoke(() =>
        {
            TimerSoundPlayer.Play(_settings.SoundProfile);

            if (_tray != null)
            {
                _tray.ShowBalloonTip(
                    2500,
                    "DeskNotes Timer",
                    "Die Zeit ist um!",
                    WinForms.ToolTipIcon.Info);
            }

            HideRunningPill();
            UpdateTrayText();
        });
    }

    private void UpdateTrayText()
    {
        if (_tray == null)
            return;

        _originalTrayText ??= _tray.Text;

        if (_service.Phase is TimerPhase.Running or TimerPhase.Paused)
            _tray.Text = $"DeskNotes · {_service.FormattedRemaining}";
        else
            _tray.Text = string.IsNullOrWhiteSpace(_originalTrayText) ? "DeskNotes" : _originalTrayText;
    }

    private void HookClickOutside()
    {
        if (_clickOutsideHooked)
            return;

        _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        _clickOutsideHooked = true;
    }

    private void UnhookClickOutside()
    {
        if (!_clickOutsideHooked)
            return;

        _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
        _clickOutsideHooked = false;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_service.IsFlyoutOpen || _flyoutCard == null)
            return;

        if (IsDescendantOf(_flyoutCard, e.OriginalSource as DependencyObject))
            return;

        _service.CloseFlyout();
    }

    private static bool IsDescendantOf(DependencyObject parent, DependencyObject? child)
    {
        while (child != null)
        {
            if (child == parent)
                return true;

            child = VisualTreeHelper.GetParent(child);
        }

        return false;
    }

    private void ShowWindowIfNeeded()
    {
        if (!_window.IsVisible)
            _window.Show();

        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;

        _window.Activate();
    }

    private static WpfButton CreateIconButton(string glyph, string toolTip)
    {
        return new WpfButton
        {
            Width = 30,
            Height = 30,
            Background = WpfBrushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = WpfCursors.Hand,
            ToolTip = toolTip,
            Content = new TextBlock
            {
                Text = glyph,
                FontFamily = new WpfFontFamily("Segoe MDL2 Assets"),
                FontSize = 11,
                Foreground = Brush("TextSecondary")
            }
        };
    }

    private static void FadeIn(UIElement element)
    {
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    private static WpfBrush Brush(string key) =>
        (WpfBrush)WpfApp.Current.FindResource(key);

    private static Style? TryStyle(string key)
    {
        try
        {
            return (Style)WpfApp.Current.FindResource(key);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        UnhookClickOutside();
        _service.Changed -= OnServiceChanged;
        _service.Completed -= OnTimerCompleted;
        HideFlyout();
        HideRunningPill();

        if (_tray != null && _originalTrayText != null)
            _tray.Text = _originalTrayText;
    }
}