using DeskNotes.Abstractions;
using DeskNotes.Core.Addons;
using DeskNotes.Models;
using DeskNotes.Services;
using DeskNotes.ViewModels;
using DeskNotes.Views;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using WpfApp = System.Windows.Application;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace DeskNotes;

public partial class MainWindow : Window
{
    private readonly AddonHost _addonHost = new();
    private readonly MainViewModel _viewModel;
    private readonly GlobalHotkeyService _hotkeyService = new();
    private readonly SettingsService _settingsService = new();
    private readonly WinForms.NotifyIcon _notifyIcon;

    private AppSettings _settings = new();
    private WpfPoint _dragStartPoint;
    private TodoItem? _dragTodo;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(_addonHost.EventBus);
        DataContext = _viewModel;

        _notifyIcon = CreateTrayIcon();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        LocationChanged += (_, _) => PersistWindowState();
        SizeChanged += (_, _) => PersistWindowState();
    }

    private WinForms.NotifyIcon CreateTrayIcon()
    {
        var icon = new WinForms.NotifyIcon
        {
            Visible = true,
            Text = "DeskNotes"
        };

        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "desknote-gelb.ico");
        if (File.Exists(iconPath))
            icon.Icon = new Drawing.Icon(iconPath);
        else if (!string.IsNullOrEmpty(Environment.ProcessPath))
            icon.Icon = Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);

        icon.DoubleClick += (_, _) => ShowFromTray();
        RefreshTrayMenu(icon);

        return icon;
    }

    private void RefreshTrayMenu(WinForms.NotifyIcon? icon = null)
    {
        icon ??= _notifyIcon;
        var menu = new WinForms.ContextMenuStrip();

        menu.Items.Add("Öffnen", null, (_, _) => ShowFromTray());
        menu.Items.Add("Neue Notiz", null, (_, _) => ShowFromTray(focusInput: true));

        foreach (var item in _addonHost.GetTrayMenuItems())
            menu.Items.Add(item.Label, null, (_, _) => item.Action());

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitApplication());

        icon.ContextMenuStrip = menu;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = _settingsService.Load();
        _settings.AutoStart = new AutoStartService().IsAutoStartEnabled();
        ApplyWindowSettings(_settings);

        if (Enum.TryParse<TaskFilter>(_settings.LastFilter, out var filter))
            _viewModel.SetFilter(filter);

        _hotkeyService.HotkeyPressed += HotkeyPressed;
        _hotkeyService.Register(this);

        await _addonHost.StartAsync(this, _notifyIcon, _viewModel);
        RefreshTrayMenu();
    }

    private void ApplyWindowSettings(AppSettings settings)
    {
        Width = Clamp(settings.Width, MinWidth, 1200);
        Height = Clamp(settings.Height, MinHeight, 900);

        if (IsPositionOnScreen(settings.Left, settings.Top))
        {
            Left = settings.Left;
            Top = settings.Top;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        Topmost = settings.TopMost;
    }

    private static bool IsPositionOnScreen(double left, double top) =>
        left >= SystemParameters.VirtualScreenLeft - 50
        && top >= SystemParameters.VirtualScreenTop - 50
        && left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
        && top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

    private static double Clamp(double value, double min, double max) =>
        Math.Max(min, Math.Min(max, value));

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        PersistWindowState(immediate: true);
        _viewModel.Flush();
        Hide();
    }

    private void PersistWindowState(bool immediate = false)
    {
        if (!IsLoaded)
            return;

        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Width = Width;
        _settings.Height = Height;
        _settings.TopMost = Topmost;
        _settings.LastFilter = _viewModel.CurrentFilter.ToString();
        _settings.AutoStart = new AutoStartService().IsAutoStartEnabled();

        if (immediate)
            _settingsService.SaveImmediate(_settings);
        else
            _settingsService.Save(_settings);
    }

    private void ShowFromTray(bool focusInput = false)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();

        if (focusInput)
        {
            NewTaskTextBox.Focus();
            Keyboard.Focus(NewTaskTextBox);
        }
    }

    private async void ExitApplication()
    {
        PersistWindowState(immediate: true);
        _viewModel.Flush();
        await _addonHost.StopAsync();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _hotkeyService.Dispose();
        _addonHost.Dispose();
        WpfApp.Current.Shutdown();
    }

    private void HotkeyPressed() =>
        Dispatcher.Invoke(() => ShowFromTray(focusInput: true));

    private void NewTaskTextBox_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = NewTaskTextBox.Text.Trim();

            var submitting = new NoteInputSubmitting { Text = text };
            _addonHost.EventBus.Publish(submitting);

            if (submitting.CancelDefault)
            {
                if (submitting.ClearInput)
                    NewTaskTextBox.Text = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                _viewModel.AddTaskCommand.Execute(null);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void TaskTextBox_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TodoItem todo })
            return;

        if (e.Key == Key.Enter)
        {
            _viewModel.SaveEditCommand.Execute(todo);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.CancelEditCommand.Execute(todo);
            e.Handled = true;
        }
    }

    private void TaskTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }

    private void AccentStrip_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TodoItem todo)
            _viewModel.CycleAccentColorCommand.Execute(todo);
    }

    private void NoteDragHandle_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TodoItem todo })
        {
            _dragTodo = todo;
            _dragStartPoint = e.GetPosition(null);
        }
    }

    private void NoteDragHandle_PreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragTodo == null)
            return;

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var dragged = _dragTodo;
        _dragTodo = null;
        DragDrop.DoDragDrop((DependencyObject)sender, dragged, WpfDragDropEffects.Move);
    }

    private void TodoList_DragOver(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(TodoItem)))
            e.Effects = WpfDragDropEffects.Move;
        else
            e.Effects = WpfDragDropEffects.None;

        e.Handled = true;
    }

    private void TodoList_Drop(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetData(typeof(TodoItem)) is not TodoItem dragged)
            return;

        var index = GetTodoDropIndex(todoList, e.GetPosition(todoList));
        _viewModel.ReorderTodo(dragged, index);
        e.Handled = true;
    }

    private static int GetTodoDropIndex(WpfListBox listBox, WpfPoint position)
    {
        for (var i = 0; i < listBox.Items.Count; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container)
                continue;

            var top = container.TranslatePoint(new WpfPoint(0, 0), listBox).Y;
            if (position.Y < top + container.ActualHeight / 2)
                return i;
        }

        return Math.Max(0, listBox.Items.Count - 1);
    }

    protected override void OnPreviewKeyDown(WpfKeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Delete &&
            !NewTaskTextBox.IsFocused &&
            !_viewModel.Todos.Any(t => t.IsEditing))
        {
            _viewModel.DeleteTaskCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        Height = Math.Max(MinHeight, Height + e.VerticalChange);
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        PersistWindowState();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(
            Topmost,
            _hotkeyService.IsRegistered,
            _hotkeyService.RegistrationError,
            _addonHost.GetSettingsSections(),
            _addonHost.GetManifests(),
            topMost =>
            {
                Topmost = topMost;
                PersistWindowState();
            },
            autoStart =>
            {
                _settings.AutoStart = autoStart;
                PersistWindowState(immediate: true);
            })
        {
            Owner = this
        };

        window.ShowDialog();
    }
}