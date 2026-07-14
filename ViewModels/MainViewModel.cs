using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeskNotes.Abstractions;
using DeskNotes.Core.Addons;
using DeskNotes.Models;
using DeskNotes.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace DeskNotes.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly StorageService _storage = new();
    private readonly EventBus? _eventBus;

    public ObservableCollection<TodoItem> Todos { get; }

    public ICollectionView FilteredTodos { get; }

    public static IReadOnlyList<string> AccentColors { get; } =
    [
        "#5FA8FF",
        "#7ED957",
        "#FFB347",
        "#FF6B9D",
        "#C77DFF",
        "#F4D35E"
    ];

    [ObservableProperty]
    private string newTaskText = string.Empty;

    [ObservableProperty]
    private TodoItem? selectedTodo;

    [ObservableProperty]
    private TaskFilter currentFilter = TaskFilter.All;

    [ObservableProperty]
    private int activeCount;

    [ObservableProperty]
    private int completedCount;

    [ObservableProperty]
    private bool isEmpty = true;

    public MainViewModel(EventBus? eventBus = null)
    {
        _eventBus = eventBus;
        Todos = _storage.Load();
        FilteredTodos = CollectionViewSource.GetDefaultView(Todos);
        FilteredTodos.Filter = FilterTodo;

        Todos.CollectionChanged += Todos_CollectionChanged;

        foreach (var todo in Todos)
            HookTodo(todo);

        UpdateCounts();
    }

    public void SetFilter(TaskFilter filter)
    {
        CurrentFilter = filter;
        FilteredTodos.Refresh();
        UpdateCounts();
    }

    [RelayCommand]
    private void ChangeFilter(TaskFilter filter) => SetFilter(filter);

    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskText))
            return;

        var todo = new TodoItem
        {
            Text = NewTaskText.Trim(),
            CreatedAt = DateTime.Now,
            AccentColor = AccentColors[Todos.Count % AccentColors.Count]
        };

        Todos.Insert(0, todo);
        NewTaskText = string.Empty;
        Save();
        UpdateCounts();
        _eventBus?.Publish(new NoteAdded { Note = ToAddonNote(todo) });
    }

    [RelayCommand]
    private void DeleteTask(TodoItem? todo)
    {
        var item = todo ?? SelectedTodo;
        if (item == null)
            return;

        Todos.Remove(item);
        Save();
        UpdateCounts();
        _eventBus?.Publish(new NoteDeleted { Note = ToAddonNote(item) });
    }

    [RelayCommand]
    private void StartEdit(TodoItem? todo)
    {
        if (todo == null)
            return;

        foreach (var item in Todos)
            item.IsEditing = false;

        todo.EditBackupText = todo.Text;
        todo.IsEditing = true;
    }

    [RelayCommand]
    private void SaveEdit(TodoItem? todo)
    {
        if (todo == null)
            return;

        todo.Text = todo.Text.Trim();
        if (string.IsNullOrWhiteSpace(todo.Text))
        {
            Todos.Remove(todo);
        }
        else
        {
            todo.IsEditing = false;
            todo.EditBackupText = null;
        }

        Save();
        UpdateCounts();
    }

    [RelayCommand]
    private void CancelEdit(TodoItem? todo)
    {
        if (todo == null)
            return;

        if (todo.EditBackupText != null)
            todo.Text = todo.EditBackupText;

        todo.IsEditing = false;
        todo.EditBackupText = null;
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var completed = Todos.Where(t => t.IsCompleted).ToList();
        if (completed.Count == 0)
            return;

        foreach (var item in completed)
            Todos.Remove(item);

        Save();
        UpdateCounts();
    }

    public void ReorderTodo(TodoItem dragged, int dropFilteredIndex)
    {
        var filtered = FilteredTodos.Cast<TodoItem>().ToList();
        var fromIndex = filtered.IndexOf(dragged);
        if (fromIndex < 0)
            return;

        dropFilteredIndex = Math.Clamp(dropFilteredIndex, 0, Math.Max(0, filtered.Count - 1));
        if (fromIndex == dropFilteredIndex)
            return;

        filtered.RemoveAt(fromIndex);
        filtered.Insert(dropFilteredIndex, dragged);

        var queue = new Queue<TodoItem>(filtered);
        var rebuilt = new List<TodoItem>();

        foreach (var todo in Todos)
        {
            if (FilterTodo(todo))
                rebuilt.Add(queue.Dequeue());
            else
                rebuilt.Add(todo);
        }

        ApplyTodoOrder(rebuilt);
        Save();
        UpdateCounts();
    }

    private void ApplyTodoOrder(IReadOnlyList<TodoItem> order)
    {
        for (var i = 0; i < order.Count; i++)
        {
            var desired = order[i];
            if (Todos[i] == desired)
                continue;

            var currentIndex = Todos.IndexOf(desired);
            if (currentIndex >= 0)
                Todos.Move(currentIndex, i);
        }
    }

    [RelayCommand]
    private void CycleAccentColor(TodoItem? todo)
    {
        if (todo == null)
            return;

        var index = -1;
        for (var i = 0; i < AccentColors.Count; i++)
        {
            if (AccentColors[i] == todo.AccentColor)
            {
                index = i;
                break;
            }
        }

        var next = (index + 1) % AccentColors.Count;
        todo.AccentColor = AccentColors[next];
        Save();
    }

    private bool FilterTodo(object obj)
    {
        if (obj is not TodoItem todo)
            return false;

        return CurrentFilter switch
        {
            TaskFilter.Active => !todo.IsCompleted,
            TaskFilter.Completed => todo.IsCompleted,
            _ => true
        };
    }

    private void Todos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TodoItem todo in e.NewItems)
                HookTodo(todo);
        }

        Save();
        UpdateCounts();
    }

    private void HookTodo(TodoItem todo) =>
        todo.PropertyChanged += Todo_PropertyChanged;

    private void Todo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TodoItem.IsEditing))
            return;

        Save();
        UpdateCounts();

        if (e.PropertyName is nameof(TodoItem.IsCompleted) && sender is TodoItem todo)
        {
            if (todo.IsCompleted)
                _eventBus?.Publish(new NoteCompleted { Note = ToAddonNote(todo) });

            FilteredTodos.Refresh();
        }
    }

    private void UpdateCounts()
    {
        ActiveCount = Todos.Count(t => !t.IsCompleted);
        CompletedCount = Todos.Count(t => t.IsCompleted);
        IsEmpty = !FilteredTodos.Cast<object>().Any();
        OnPropertyChanged(nameof(FilteredTodos));
    }

    private void Save() => _storage.Save(Todos);

    public void Flush() => _storage.SaveImmediate(Todos);

    private static AddonNote ToAddonNote(TodoItem item) => new()
    {
        Id = item.Id,
        Text = item.Text,
        IsCompleted = item.IsCompleted,
        CreatedAt = item.CreatedAt,
        CompletedAt = item.CompletedAt,
        AccentColor = item.AccentColor
    };
}