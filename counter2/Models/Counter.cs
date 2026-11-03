using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace counter2.Models;

public class Counter : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString();
    private string _name = string.Empty;
    private int _initialValue;
    private int _value;
    private string _colorHex = "#512BD4";

    public string Id
    {
        get => _id;
        set => _id = value;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public int InitialValue
    {
        get => _initialValue;
        set
        {
            if (_initialValue != value)
            {
                _initialValue = value;
                OnPropertyChanged();
            }
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (_colorHex != value)
            {
                _colorHex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayColor));
                OnPropertyChanged(nameof(DisplayBrush));
            }
        }
    }

    [JsonIgnore]
    public Color DisplayColor
    {
        get
        {
            try
            {
                return Color.FromArgb(_colorHex);
            }
            catch
            {
                return Color.FromArgb("#512BD4");
            }
        }
    }

    [JsonIgnore]
    public Brush DisplayBrush => new SolidColorBrush(DisplayColor);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
