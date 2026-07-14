namespace DeskNotes.Models;

public class WindowSettings
{
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;

    public double Width { get; set; } = 430;
    public double Height { get; set; } = 620;

    public bool TopMost { get; set; } = false;
}