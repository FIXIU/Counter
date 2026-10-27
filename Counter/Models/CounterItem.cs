using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using SQLite;

namespace Counter.Models;

[Table("counters")]
public partial class CounterItem : ObservableObject
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    public const string DefaultColorHex = "#E60033";

    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("initial_value")]
    public long InitialValue { get; set; }

    [ObservableProperty]
    [property: Column("current_value")]
    private long _currentValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContrastTextColor))]
    [property: Column("color_hex")]
    private string _colorHex = DefaultColorHex;

    [Column("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ObservableProperty]
    [property: Column("updated_at_utc")]
    [property: Indexed(Name = "IDX_COUNTERS_UPDATED_AT", Order = 1)]
    private DateTime _updatedAtUtc = DateTime.UtcNow;

    [ObservableProperty]
    [property: Column("is_synced")]
    private bool _isSynced;

    [Column("revision")]
    public long Revision { get; set; } = 1;

    [Ignore]
    public Color ContrastTextColor
    {
        get
        {
            var validHex = NormalizeColorHex(ColorHex);
            var color = Color.FromArgb(validHex);
            double luminance = 0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue;
            return luminance > 0.6 ? Color.FromArgb("#121212") : Colors.White;
        }
    }

    public static string NormalizeColorHex(string? rawHex)
    {
        if (!string.IsNullOrWhiteSpace(rawHex) && HexColorRegex.IsMatch(rawHex.Trim()))
        {
            return rawHex.Trim().ToUpperInvariant();
        }
        return DefaultColorHex;
    }
}
