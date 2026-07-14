using DeskNotes.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace DeskNotes.Services;

public class StorageService
{
    private readonly string _filePath;
    private readonly DispatcherTimer _saveTimer;
    private ObservableCollection<TodoItem> _pending = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StorageService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskNotes");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "todo.json");

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            Write(_pending);
        };
    }

    public void Save(ObservableCollection<TodoItem> todos)
    {
        _pending = todos;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveImmediate(ObservableCollection<TodoItem> todos) => Write(todos);

    public ObservableCollection<TodoItem> Load()
    {
        if (!File.Exists(_filePath))
            return new ObservableCollection<TodoItem>();

        try
        {
            var json = File.ReadAllText(_filePath);
            var items = JsonSerializer.Deserialize<List<TodoItem>>(json, JsonOptions);

            if (items == null || items.Count == 0)
                return new ObservableCollection<TodoItem>();

            foreach (var item in items)
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();
            }

            return new ObservableCollection<TodoItem>(items);
        }
        catch
        {
            TryBackupCorruptFile();
            return new ObservableCollection<TodoItem>();
        }
    }

    private void Write(ObservableCollection<TodoItem> todos)
    {
        try
        {
            var json = JsonSerializer.Serialize(todos.ToList(), JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Speichern darf die App nicht abstürzen lassen.
        }
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            var backup = _filePath + $".bak.{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(_filePath, backup, overwrite: true);
        }
        catch
        {
            // Backup ist optional.
        }
    }
}