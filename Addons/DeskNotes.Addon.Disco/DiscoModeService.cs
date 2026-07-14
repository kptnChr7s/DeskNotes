using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using WpfApp = System.Windows.Application;
using WpfColor = System.Windows.Media.Color;

namespace DeskNotes.Addon.Disco;

public sealed class DiscoModeService : IDisposable
{
    private const int Bpm = 128;
    private static readonly string[] PartyTitles =
    [
        "🕺 DISCO NOTES 💃",
        "🪩 GET DOWN! 🪩",
        "✨ SATURDAY NIGHT ✨",
        "🔥 FUNKY DESK 🔥",
        "💥 BOOGIE TIME 💥",
        "🌈 NEON NOTES 🌈"
    ];

    private static readonly string[] PartySubtitles =
    [
        "Tanzfläche aktiviert",
        "Groove im Gange",
        "Alle Notizen auf der Tanzfläche",
        "Spiegelkugel-Modus ON",
        "Legende des Schreibtischs"
    ];

    private readonly Window _window;
    private readonly DispatcherTimer _beatTimer;
    private readonly DispatcherTimer _frameTimer;
    private readonly Random _rng = new();

    private Border? _mainBorder;
    private Grid? _contentRoot;
    private ScaleTransform? _scaleTransform;
    private RotateTransform? _rotateTransform;
    private Canvas? _fxCanvas;
    private Border? _strobe;
    private UniformGrid? _floorLights;
    private Canvas? _ballCanvas;
    private RotateTransform? _ballSpin;
    private TextBlock? _titleText;
    private TextBlock? _subtitleText;
    private System.Windows.Controls.ListBox? _todoList;
    private Border? _headerBorder;
    private Border? _filterBar;
    private Border? _composerOuter;

    private WinForms.NotifyIcon? _tray;
    private string _originalTrayText = "DeskNotes";

    private bool _active;
    private int _beat;
    private int _megaLevel;
    private double _hue;
    private double _laserAngle;
    private double _pulsePhase;
    private int _titleIndex;

    private System.Windows.Media.Brush? _savedShellBackground;
    private System.Windows.Media.Brush? _savedBorderBrush;
    private Thickness _savedBorderThickness;
    private Effect? _savedEffect;
    private string _originalTitle = "DeskNotes";
    private string _originalSubtitle = "Desktop Notizen";

    private readonly List<ConfettiPiece> _confetti = [];
    private readonly List<Line> _lasers = [];
    private readonly Dictionary<Border, double> _noteWigglePhase = [];

    public bool IsActive => _active;

    public DiscoModeService(Window window)
    {
        _window = window;
        _originalTitle = window.Title;

        _beatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(60_000.0 / Bpm)
        };
        _beatTimer.Tick += OnBeat;

        _frameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _frameTimer.Tick += OnFrame;
    }

    public void AttachTray(WinForms.NotifyIcon tray)
    {
        _tray = tray;
        _originalTrayText = tray.Text;
    }

    public void Bind()
    {
        _mainBorder = _window.FindName("MainBorder") as Border;
        _contentRoot = _window.FindName("DiscoContentRoot") as Grid;
        _fxCanvas = _window.FindName("DiscoFxCanvas") as Canvas;
        _strobe = _window.FindName("DiscoStrobe") as Border;
        _floorLights = _window.FindName("DiscoFloorLights") as UniformGrid;
        _ballCanvas = _window.FindName("DiscoBallCanvas") as Canvas;
        _titleText = _window.FindName("AppTitleText") as TextBlock;
        _subtitleText = _window.FindName("AppSubtitleText") as TextBlock;
        _todoList = _window.FindName("todoList") as System.Windows.Controls.ListBox;
        _headerBorder = _window.FindName("HeaderBorder") as Border;
        _filterBar = _window.FindName("FilterBarBorder") as Border;
        _composerOuter = _window.FindName("ComposerOuterBorder") as Border;

        if (_contentRoot?.RenderTransform is TransformGroup group)
        {
            _scaleTransform = group.Children.OfType<ScaleTransform>().FirstOrDefault();
            _rotateTransform = group.Children.OfType<RotateTransform>().FirstOrDefault();
        }

        if (_ballCanvas?.RenderTransform is RotateTransform spin)
            _ballSpin = spin;

        BuildDiscoBall();
        BuildFloorLights();
        BuildLasers();
    }

    public void Toggle()
    {
        if (_active)
            Deactivate();
        else
            Activate();
    }

    private void Activate()
    {
        if (_mainBorder == null)
            Bind();

        _active = true;
        _beat = 0;
        _megaLevel++;
        _hue = _rng.Next(0, 360);
        _noteWigglePhase.Clear();

        CacheOriginalState();
        ShowDiscoLayers(true);
        BuildLasers();
        SpawnConfetti(100);
        PlayFanfare();
        PlaySystemSound(SystemSoundType.Exclamation);

        _beatTimer.Start();
        _frameTimer.Start();
        ApplyBeatVisuals(flashStrobe: true);
    }

    private void Deactivate()
    {
        _active = false;
        _beatTimer.Stop();
        _frameTimer.Stop();

        PlayLightsOn();
        PlaySystemSound(SystemSoundType.Asterisk);
        RestoreOriginalState();
        ShowDiscoLayers(false);
        ClearFxCanvas();
        _confetti.Clear();
        _noteWigglePhase.Clear();

        if (_tray != null)
            _tray.Text = _originalTrayText;
    }

    private void CacheOriginalState()
    {
        if (_mainBorder != null)
        {
            _savedShellBackground = _mainBorder.Background;
            _savedBorderBrush = _mainBorder.BorderBrush;
            _savedBorderThickness = _mainBorder.BorderThickness;
            _savedEffect = _mainBorder.Effect;
        }

        if (_subtitleText != null)
            _originalSubtitle = _subtitleText.Text;
    }

    private void RestoreOriginalState()
    {
        _window.Title = _originalTitle;

        if (_titleText != null)
            _titleText.Text = "DeskNotes";

        if (_subtitleText != null)
            _subtitleText.Text = _originalSubtitle;

        if (_mainBorder != null)
        {
            _mainBorder.ClearValue(Border.BackgroundProperty);
            _mainBorder.Background = _savedShellBackground
                ?? (System.Windows.Media.Brush)WpfApp.Current.Resources["ShellBackgroundBrush"];

            _mainBorder.BorderBrush = _savedBorderBrush
                ?? (System.Windows.Media.Brush)WpfApp.Current.Resources["CardBorder"];

            _mainBorder.BorderThickness = _savedBorderThickness;
            _mainBorder.Effect = _savedEffect;
        }

        if (_headerBorder != null)
            _headerBorder.ClearValue(Border.BackgroundProperty);

        if (_filterBar != null)
            _filterBar.ClearValue(Border.BackgroundProperty);

        if (_composerOuter != null)
            _composerOuter.ClearValue(Border.BackgroundProperty);

        ResetNoteCards();
        ResetTransforms();
    }

    private void ShowDiscoLayers(bool visible)
    {
        var v = visible ? Visibility.Visible : Visibility.Collapsed;

        if (_fxCanvas != null) _fxCanvas.Visibility = v;
        if (_ballCanvas != null) _ballCanvas.Visibility = v;
        if (_floorLights != null) _floorLights.Visibility = v;
    }

    private void OnBeat(object? sender, EventArgs e)
    {
        if (!_active) return;

        _beat++;
        _hue = (_hue + 47) % 360;
        _titleIndex = (_titleIndex + 1) % PartyTitles.Length;

        ApplyBeatVisuals(flashStrobe: _beat % 2 == 0);
        PlayKick();

        if (_beat % 4 == 0)
            SpawnConfetti(18);

        if (_megaLevel >= 2 && _beat % 16 == 0)
            TriggerMegaSpin();
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        if (!_active) return;

        _pulsePhase += 0.14;
        _laserAngle = (_laserAngle + 2.8) % 360;

        AnimatePulse();
        AnimateBall();
        AnimateLasers();
        AnimateConfetti();
        AnimateNoteWiggle();
        AnimateRainbowText();
    }

    private void ApplyBeatVisuals(bool flashStrobe)
    {
        var color = ColorFromHsv(_hue, 0.92, 0.95);
        var accent = ColorFromHsv((_hue + 120) % 360, 0.85, 1.0);
        var brush = new SolidColorBrush(color);

        _window.Title = PartyTitles[_titleIndex];

        if (_titleText != null)
            _titleText.Text = PartyTitles[_titleIndex];

        if (_subtitleText != null)
            _subtitleText.Text = PartySubtitles[_titleIndex % PartySubtitles.Length];

        if (_mainBorder != null)
        {
            _mainBorder.Background = brush;
            _mainBorder.BorderBrush = new SolidColorBrush(accent);
            _mainBorder.BorderThickness = new Thickness(2);

            var glow = new DropShadowEffect
            {
                Color = accent,
                BlurRadius = 36,
                ShadowDepth = 0,
                Opacity = 0.85
            };
            _mainBorder.Effect = glow;
        }

        if (_headerBorder != null)
            _headerBorder.Background = new SolidColorBrush(ColorFromHsv((_hue + 40) % 360, 0.7, 0.35));

        if (_filterBar != null)
            _filterBar.Background = new SolidColorBrush(ColorFromHsv((_hue + 200) % 360, 0.6, 0.28));

        if (_composerOuter != null)
            _composerOuter.Background = new SolidColorBrush(ColorFromHsv((_hue + 80) % 360, 0.75, 0.5));

        PulseFloorLights();
        BlinkFilterLeds();
        CycleNoteCardColors();

        if (flashStrobe && _strobe != null)
        {
            _strobe.Opacity = _beat % 4 == 0 ? 0.28 : 0.12;
            _strobe.Background = new SolidColorBrush(
                _beat % 4 == 0 ? WpfColor.FromArgb(180, 255, 255, 255) : ColorFromHsv(_hue, 0.5, 1));
        }

        if (_tray != null)
            _tray.Text = _beat % 2 == 0 ? "🪩 DISCO NOTES 🪩" : _originalTrayText;
    }

    private void AnimatePulse()
    {
        if (_scaleTransform == null) return;

        var pulse = 1.0 + Math.Sin(_pulsePhase) * 0.018;
        _scaleTransform.ScaleX = pulse;
        _scaleTransform.ScaleY = pulse;

        if (_rotateTransform != null && _megaLevel >= 2)
            _rotateTransform.Angle = Math.Sin(_pulsePhase * 0.5) * 3.5;
    }

    private void TriggerMegaSpin()
    {
        if (_rotateTransform == null) return;

        var animation = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = _rotateTransform.Angle,
            To = _rotateTransform.Angle + 360,
            Duration = TimeSpan.FromSeconds(1.4),
            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
        };
        _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
    }

    private void ResetTransforms()
    {
        _scaleTransform?.SetValue(ScaleTransform.ScaleXProperty, 1.0);
        _scaleTransform?.SetValue(ScaleTransform.ScaleYProperty, 1.0);
        _rotateTransform?.SetValue(RotateTransform.AngleProperty, 0.0);

        if (_strobe != null)
            _strobe.Opacity = 0;
    }

    private void BuildDiscoBall()
    {
        if (_ballCanvas == null) return;

        _ballCanvas.Children.Clear();

        var ball = new Ellipse
        {
            Width = 46,
            Height = 46,
            Fill = new RadialGradientBrush(
                WpfColor.FromRgb(255, 255, 255),
                WpfColor.FromRgb(120, 120, 140))
            {
                GradientOrigin = new System.Windows.Point(0.35, 0.35),
                Center = new System.Windows.Point(0.35, 0.35)
            },
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 1.2
        };

        Canvas.SetLeft(ball, 2);
        Canvas.SetTop(ball, 2);
        _ballCanvas.Children.Add(ball);

        for (var i = 0; i < 10; i++)
        {
            var sq = new System.Windows.Shapes.Rectangle
            {
                Width = 7,
                Height = 7,
                Fill = new SolidColorBrush(ColorFromHsv(i * 36, 0.4, 1)),
                Opacity = 0.85,
                RadiusX = 1,
                RadiusY = 1
            };
            var angle = i * 36 * Math.PI / 180;
            Canvas.SetLeft(sq, 23 + Math.Cos(angle) * 14);
            Canvas.SetTop(sq, 23 + Math.Sin(angle) * 14);
            _ballCanvas.Children.Add(sq);
        }
    }

    private void AnimateBall()
    {
        if (_ballSpin != null)
            _ballSpin.Angle = (_ballSpin.Angle + 4.5) % 360;
    }

    private void BuildFloorLights()
    {
        if (_floorLights == null) return;

        _floorLights.Children.Clear();

        for (var i = 0; i < 12; i++)
        {
            _floorLights.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(2, 0, 2, 0),
                Background = new SolidColorBrush(ColorFromHsv(i * 30, 0.9, 0.9))
            });
        }
    }

    private void PulseFloorLights()
    {
        if (_floorLights == null) return;

        for (var i = 0; i < _floorLights.Children.Count; i++)
        {
            if (_floorLights.Children[i] is Border light)
            {
                var hue = (_hue + i * 30 + _beat * 20) % 360;
                light.Background = new SolidColorBrush(ColorFromHsv(hue, 0.95, _beat % 2 == 0 ? 1.0 : 0.55));
            }
        }
    }

    private void BuildLasers()
    {
        if (_fxCanvas == null) return;

        foreach (var laser in _lasers)
            _fxCanvas.Children.Remove(laser);

        _lasers.Clear();

        for (var i = 0; i < 5; i++)
        {
            var line = new Line
            {
                Stroke = new SolidColorBrush(ColorFromHsv(i * 72, 0.9, 1)),
                StrokeThickness = 2,
                Opacity = 0.45,
                X1 = 0,
                Y1 = 0,
                X2 = 200,
                Y2 = 0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            _lasers.Add(line);
            _fxCanvas.Children.Add(line);
        }
    }

    private void AnimateLasers()
    {
        if (_fxCanvas == null || _lasers.Count == 0) return;

        var w = _fxCanvas.ActualWidth > 0 ? _fxCanvas.ActualWidth : 400;
        var h = _fxCanvas.ActualHeight > 0 ? _fxCanvas.ActualHeight : 600;
        var cx = w / 2;
        var cy = h / 2;

        for (var i = 0; i < _lasers.Count; i++)
        {
            var angle = (_laserAngle + i * 36) * Math.PI / 180;
            var len = Math.Max(w, h) * 0.9;
            _lasers[i].X1 = cx;
            _lasers[i].Y1 = cy;
            _lasers[i].X2 = cx + Math.Cos(angle) * len;
            _lasers[i].Y2 = cy + Math.Sin(angle) * len;
            _lasers[i].Stroke = new SolidColorBrush(ColorFromHsv((_hue + i * 50) % 360, 0.85, 1));
        }
    }

    private void SpawnConfetti(int count)
    {
        if (_fxCanvas == null) return;

        var w = _fxCanvas.ActualWidth > 0 ? _fxCanvas.ActualWidth : 400;

        for (var i = 0; i < count; i++)
        {
            var shape = new System.Windows.Shapes.Rectangle
            {
                Width = _rng.Next(5, 11),
                Height = _rng.Next(6, 14),
                Fill = new SolidColorBrush(ColorFromHsv(_rng.Next(0, 360), 0.85, 1)),
                RadiusX = 2,
                RadiusY = 2,
                Opacity = 0.9
            };

            var piece = new ConfettiPiece
            {
                Shape = shape,
                X = _rng.NextDouble() * w,
                Y = -_rng.Next(10, 80),
                Vx = _rng.NextDouble() * 3 - 1.5,
                Vy = _rng.NextDouble() * 2 + 2,
                RotationSpeed = _rng.NextDouble() * 8 - 4
            };

            _confetti.Add(piece);
            _fxCanvas.Children.Add(shape);
            Canvas.SetLeft(shape, piece.X);
            Canvas.SetTop(shape, piece.Y);
        }
    }

    private void AnimateConfetti()
    {
        if (_fxCanvas == null) return;

        var h = _fxCanvas.ActualHeight > 0 ? _fxCanvas.ActualHeight : 600;

        for (var i = _confetti.Count - 1; i >= 0; i--)
        {
            var p = _confetti[i];
            p.X += p.Vx;
            p.Y += p.Vy;
            p.Vy += 0.12;
            p.Rotation += p.RotationSpeed;

            Canvas.SetLeft(p.Shape, p.X);
            Canvas.SetTop(p.Shape, p.Y);
            p.Shape.RenderTransform = new RotateTransform(p.Rotation, p.Shape.Width / 2, p.Shape.Height / 2);

            if (p.Y > h + 20)
            {
                _fxCanvas.Children.Remove(p.Shape);
                _confetti.RemoveAt(i);
            }
        }
    }

    private void ClearFxCanvas()
    {
        if (_fxCanvas == null) return;

        _fxCanvas.Children.Clear();
        _lasers.Clear();
    }

    private void BlinkFilterLeds()
    {
        if (_filterBar == null) return;

        var buttons = FindVisualChildren<System.Windows.Controls.Control>(_filterBar)
            .Where(c => c is System.Windows.Controls.Button or System.Windows.Controls.Primitives.ButtonBase)
            .ToList();
        for (var i = 0; i < buttons.Count; i++)
        {
            buttons[i].Background = new SolidColorBrush(
                ColorFromHsv((_hue + i * 60) % 360, 0.8, _beat % 2 == 0 ? 0.7 : 0.35));
        }
    }

    private void CycleNoteCardColors()
    {
        if (_todoList == null) return;

        for (var i = 0; i < _todoList.Items.Count; i++)
        {
            if (_todoList.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container)
                continue;

            var card = FindNoteCardBorder(container);
            if (card == null) continue;

            card.Background = new SolidColorBrush(ColorFromHsv((_hue + i * 37) % 360, 0.55, 0.32));
            card.BorderBrush = new SolidColorBrush(ColorFromHsv((_hue + i * 37 + 60) % 360, 0.9, 0.85));

            if (!_noteWigglePhase.ContainsKey(card))
                _noteWigglePhase[card] = _rng.NextDouble() * Math.PI * 2;
        }
    }

    private void AnimateNoteWiggle()
    {
        foreach (var (card, phase) in _noteWigglePhase.ToList())
        {
            if (!card.IsLoaded)
            {
                _noteWigglePhase.Remove(card);
                continue;
            }

            var angle = Math.Sin(_pulsePhase * 2.5 + phase) * 4.5;
            card.RenderTransform = new RotateTransform(angle, card.ActualWidth / 2, card.ActualHeight / 2);
        }
    }

    private void AnimateRainbowText()
    {
        if (_todoList == null) return;

        for (var i = 0; i < _todoList.Items.Count; i++)
        {
            if (_todoList.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container)
                continue;

            var text = FindNoteTextBlock(container);
            if (text == null) continue;

            text.Foreground = new SolidColorBrush(ColorFromHsv((_hue + i * 25 + (int)(_pulsePhase * 40)) % 360, 0.75, 1));
        }
    }

    private void ResetNoteCards()
    {
        if (_todoList == null) return;

        for (var i = 0; i < _todoList.Items.Count; i++)
        {
            if (_todoList.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container)
                continue;

            var card = FindNoteCardBorder(container);
            if (card == null) continue;

            card.ClearValue(Border.BackgroundProperty);
            card.ClearValue(Border.BorderBrushProperty);
            card.RenderTransform = null;

            var text = FindNoteTextBlock(container);
            text?.ClearValue(TextBlock.ForegroundProperty);
        }
    }

    private static void PlayFanfare()
    {
        Task.Run(async () =>
        {
            int[] notes = [262, 330, 392, 523, 659, 784, 1047];
            foreach (var freq in notes)
            {
                try { Console.Beep(freq, 90); } catch { /* noop */ }
                await Task.Delay(95);
            }
        });
    }

    private static void PlayKick()
    {
        Task.Run(() =>
        {
            try { Console.Beep(110, 40); } catch { /* noop */ }
        });
    }

    private static void PlayLightsOn()
    {
        Task.Run(async () =>
        {
            int[] notes = [784, 659, 523, 392, 330];
            foreach (var freq in notes)
            {
                try { Console.Beep(freq, 80); } catch { /* noop */ }
                await Task.Delay(85);
            }
        });
    }

    private static void PlaySystemSound(SystemSoundType type)
    {
        try
        {
            switch (type)
            {
                case SystemSoundType.Exclamation: SystemSounds.Exclamation.Play(); break;
                case SystemSoundType.Asterisk: SystemSounds.Asterisk.Play(); break;
            }
        }
        catch { /* noop */ }
    }

    private enum SystemSoundType { Exclamation, Asterisk }

    private static WpfColor ColorFromHsv(double hue, double saturation, double value)
    {
        var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        var f = hue / 60 - Math.Floor(hue / 60);

        var p = value * (1 - saturation);
        var q = value * (1 - f * saturation);
        var t = value * (1 - (1 - f) * saturation);

        return hi switch
        {
            0 => WpfColor.FromRgb((byte)(value * 255), (byte)(t * 255), (byte)(p * 255)),
            1 => WpfColor.FromRgb((byte)(q * 255), (byte)(value * 255), (byte)(p * 255)),
            2 => WpfColor.FromRgb((byte)(p * 255), (byte)(value * 255), (byte)(t * 255)),
            3 => WpfColor.FromRgb((byte)(p * 255), (byte)(q * 255), (byte)(value * 255)),
            4 => WpfColor.FromRgb((byte)(t * 255), (byte)(p * 255), (byte)(value * 255)),
            _ => WpfColor.FromRgb((byte)(value * 255), (byte)(p * 255), (byte)(q * 255))
        };
    }

    private static Border? FindNoteCardBorder(ListBoxItem container)
    {
        var borders = FindVisualChildren<Border>(container);
        return borders
            .Where(b => b.ActualWidth > 80 || b.Padding.Left >= 10)
            .OrderByDescending(b => b.ActualWidth)
            .FirstOrDefault();
    }

    private static TextBlock? FindNoteTextBlock(ListBoxItem container)
    {
        return FindVisualChildren<TextBlock>(container)
            .Where(t => t.FontSize >= 14)
            .OrderByDescending(t => t.FontSize)
            .FirstOrDefault();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private static List<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var list = new List<T>();
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                list.Add(match);

            list.AddRange(FindVisualChildren<T>(child));
        }
        return list;
    }

    public void Dispose()
    {
        if (_active)
            Deactivate();

        _beatTimer.Stop();
        _frameTimer.Stop();
    }

    private sealed class ConfettiPiece
    {
        public required System.Windows.Shapes.Rectangle Shape { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; init; }
        public double Vy { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; init; }
    }
}