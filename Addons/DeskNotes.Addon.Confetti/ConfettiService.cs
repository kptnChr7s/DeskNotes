using DeskNotes.Abstractions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfColor = System.Windows.Media.Color;

namespace DeskNotes.Addon.Confetti;

internal sealed class ConfettiService : IDisposable
{
    private readonly Window _window;
    private readonly ConfettiSettingsStore _settings;
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly Random _rng = new();
    private readonly List<Particle> _particles = [];
    private readonly bool _ownsCanvas;

    public ConfettiService(Window window, ConfettiSettingsStore settings)
    {
        _window = window;
        _settings = settings;

        if (window.FindName("ConfettiFxCanvas") is Canvas existing)
        {
            _canvas = existing;
            _ownsCanvas = false;
        }
        else
        {
            var root = window.FindName("DiscoRoot") as Panel
                       ?? window.Content as Panel
                       ?? throw new InvalidOperationException("Confetti overlay host not found");

            _canvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false,
                ClipToBounds = true
            };
            Panel.SetZIndex(_canvas, 200);
            root.Children.Add(_canvas);
            _ownsCanvas = true;
        }

        SyncCanvasSize();
        if (_canvas.Parent is FrameworkElement parent)
            parent.SizeChanged += OnOverlayHostSizeChanged;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Animate();
    }

    public void Burst(AddonNote note)
    {
        var origin = FindBurstOrigin(note.Id) ?? GetFallbackOrigin();
        SpawnBurst(origin, note.AccentColor, 48);

        if (_settings.PlaySound)
            PlayCompletionSound();
    }

    private void OnOverlayHostSizeChanged(object sender, SizeChangedEventArgs e) => SyncCanvasSize();

    private void SyncCanvasSize()
    {
        if (_canvas.Parent is not FrameworkElement host)
            return;

        _canvas.Width = host.ActualWidth;
        _canvas.Height = host.ActualHeight;
    }

    private Point GetFallbackOrigin()
    {
        SyncCanvasSize();

        var w = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : _window.ActualWidth;
        var h = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : _window.ActualHeight;

        if (w <= 0) w = 400;
        if (h <= 0) h = 600;

        return new Point(w * 0.5, h * 0.45);
    }

    private void SpawnBurst(Point origin, string accentHex, int count)
    {
        SyncCanvasSize();

        var accent = ParseColor(accentHex);

        for (var i = 0; i < count; i++)
        {
            var angle = _rng.NextDouble() * Math.PI * 2;
            var speed = _rng.NextDouble() * 6 + 3;
            var useAccent = _rng.NextDouble() < 0.45;

            var shape = new Rectangle
            {
                Width = _rng.Next(5, 10),
                Height = _rng.Next(6, 12),
                RadiusX = 2,
                RadiusY = 2,
                Fill = new SolidColorBrush(useAccent ? accent : RandomColor()),
                Opacity = 0.95
            };

            var particle = new Particle
            {
                Shape = shape,
                X = origin.X,
                Y = origin.Y,
                Vx = Math.Cos(angle) * speed,
                Vy = Math.Sin(angle) * speed - _rng.NextDouble() * 4 - 2,
                RotationSpeed = _rng.NextDouble() * 10 - 5,
                Life = _rng.Next(40, 70)
            };

            _particles.Add(particle);
            _canvas.Children.Add(shape);
            Canvas.SetLeft(shape, particle.X);
            Canvas.SetTop(shape, particle.Y);

            if (_particles.Count == 1)
                _timer.Start();
        }
    }

    private void Animate()
    {
        var h = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : _window.ActualHeight;
        if (h <= 0) h = 600;

        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.X += p.Vx;
            p.Y += p.Vy;
            p.Vy += 0.18;
            p.Vx *= 0.98;
            p.Rotation += p.RotationSpeed;
            p.Life--;

            Canvas.SetLeft(p.Shape, p.X);
            Canvas.SetTop(p.Shape, p.Y);
            p.Shape.RenderTransform = new RotateTransform(p.Rotation, p.Shape.Width / 2, p.Shape.Height / 2);
            p.Shape.Opacity = Math.Max(0, p.Life / 40.0);

            if (p.Life <= 0 || p.Y > h + 30)
            {
                _canvas.Children.Remove(p.Shape);
                _particles.RemoveAt(i);
            }
        }

        if (_particles.Count == 0)
            _timer.Stop();
    }

    private Point? FindBurstOrigin(Guid noteId)
    {
        try
        {
            if (_window.FindName("todoList") is not ListBox listBox)
                return null;

            for (var i = 0; i < listBox.Items.Count; i++)
            {
                if (!TryGetItemId(listBox.Items[i], out var id) || id != noteId)
                    continue;

                if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container)
                    return null;

                var target = FindVisualChild<CheckBox>(container) as UIElement ?? container;
                var x = target.RenderSize.Width > 0 ? target.RenderSize.Width / 2 : target.DesiredSize.Width / 2;
                var y = target.RenderSize.Height > 0 ? target.RenderSize.Height / 2 : target.DesiredSize.Height / 2;

                return target.TransformToAncestor(_canvas).Transform(new Point(x, y));
            }
        }
        catch
        {
            // fall back to center burst
        }

        return null;
    }

    private static bool TryGetItemId(object item, out Guid id)
    {
        var prop = item.GetType().GetProperty("Id");
        if (prop?.GetValue(item) is Guid value)
        {
            id = value;
            return true;
        }

        id = default;
        return false;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;

            var found = FindVisualChild<T>(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private WpfColor RandomColor() =>
        ColorFromHsv(_rng.Next(0, 360), 0.75 + _rng.NextDouble() * 0.2, 0.95);

    private static WpfColor ParseColor(string hex)
    {
        try
        {
            return (WpfColor)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return ColorFromHsv(210, 0.8, 1);
        }
    }

    private static WpfColor ColorFromHsv(double hue, double saturation, double value)
    {
        var hi = (int)(hue / 60) % 6;
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

    private static void PlayCompletionSound()
    {
        Task.Run(async () =>
        {
            int[] notes = [523, 659, 784];
            foreach (var freq in notes)
            {
                try { Console.Beep(freq, 70); } catch { /* noop */ }
                await Task.Delay(75);
            }
        });
    }

    public void Dispose()
    {
        _timer.Stop();
        _particles.Clear();
        _canvas.Children.Clear();

        if (_canvas.Parent is FrameworkElement parent)
            parent.SizeChanged -= OnOverlayHostSizeChanged;

        if (_ownsCanvas && _canvas.Parent is Panel panel)
            panel.Children.Remove(_canvas);
    }

    private sealed class Particle
    {
        public required Rectangle Shape { get; init; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; init; }
        public int Life { get; set; }
    }
}