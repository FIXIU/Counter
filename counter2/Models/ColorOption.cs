namespace counter2.Models;

public class ColorOption
{
    public string Name { get; set; } = string.Empty;
    public string Hex { get; set; } = "#512BD4";

    public override string ToString() => Name;
}
