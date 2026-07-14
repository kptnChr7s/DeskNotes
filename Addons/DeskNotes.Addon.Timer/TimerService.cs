using System.Windows.Threading;

namespace DeskNotes.Addon.Timer;

internal enum TimerPhase
{
    Idle,
    Running,
    Paused
}

internal sealed class TimerService : IDisposable
{
    public static readonly int[] Presets = [5, 10, 15, 30, 45, 60];

    private readonly DispatcherTimer _timer;
    private readonly TimerSettingsStore _settings;

    public TimerPhase Phase { get; private set; } = TimerPhase.Idle;
    public int SelectedMinutes { get; private set; }
    public int RemainingSeconds { get; private set; }
    public int TotalSeconds { get; private set; }
    public bool IsFlyoutOpen { get; private set; }

    public event Action? Changed;

    public TimerService(TimerSettingsStore settings)
    {
        _settings = settings;
        SelectedMinutes = Math.Clamp(settings.LastMinutes, 1, 180);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTick();
    }

    public void OpenFlyout(int? minutes = null)
    {
        if (minutes.HasValue)
            SetMinutes(minutes.Value, persist: false);

        IsFlyoutOpen = true;
        Changed?.Invoke();
    }

    public void CloseFlyout()
    {
        if (!IsFlyoutOpen)
            return;

        IsFlyoutOpen = false;
        Changed?.Invoke();
    }

    public void ToggleFlyout()
    {
        if (IsFlyoutOpen)
            CloseFlyout();
        else
            OpenFlyout();
    }

    public void SetMinutes(int minutes, bool persist = true)
    {
        SelectedMinutes = Math.Clamp(minutes, 1, 180);

        if (persist)
        {
            _settings.LastMinutes = SelectedMinutes;
            _settings.Save();
        }

        Changed?.Invoke();
    }

    public void AdjustMinutes(int delta) => SetMinutes(SelectedMinutes + delta);

    public void Start()
    {
        TotalSeconds = SelectedMinutes * 60;
        RemainingSeconds = TotalSeconds;
        Phase = TimerPhase.Running;
        IsFlyoutOpen = false;

        _settings.LastMinutes = SelectedMinutes;
        _settings.Save();

        _timer.Start();
        Changed?.Invoke();
    }

    public void Pause()
    {
        if (Phase != TimerPhase.Running)
            return;

        Phase = TimerPhase.Paused;
        _timer.Stop();
        Changed?.Invoke();
    }

    public void Resume()
    {
        if (Phase != TimerPhase.Paused)
            return;

        Phase = TimerPhase.Running;
        _timer.Start();
        Changed?.Invoke();
    }

    public void Stop()
    {
        _timer.Stop();
        Phase = TimerPhase.Idle;
        RemainingSeconds = 0;
        TotalSeconds = 0;
        IsFlyoutOpen = false;
        Changed?.Invoke();
    }

    public double Progress =>
        TotalSeconds <= 0 ? 0 : (TotalSeconds - RemainingSeconds) / (double)TotalSeconds;

    public bool IsLowTime => Phase == TimerPhase.Running && RemainingSeconds <= 60;

    public string FormattedRemaining
    {
        get
        {
            var minutes = Math.Max(0, RemainingSeconds) / 60;
            var seconds = Math.Max(0, RemainingSeconds) % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }

    private void OnTick()
    {
        if (RemainingSeconds <= 0)
            return;

        RemainingSeconds--;

        if (RemainingSeconds <= 0)
        {
            _timer.Stop();
            Phase = TimerPhase.Idle;
            Changed?.Invoke();
            Completed?.Invoke();
            return;
        }

        Changed?.Invoke();
    }

    public event Action? Completed;

    public void Dispose() => _timer.Stop();
}