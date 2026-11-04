using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using counter2.Models;

namespace counter2;

public partial class MainPage : ContentPage
{
    private static readonly string FilePath = Path.Combine(FileSystem.AppDataDirectory, "counters.json");

    public ObservableCollection<Counter> Counters { get; } = new();

    public static readonly List<ColorOption> AvailableColors = new()
    {
        new ColorOption { Name = "Fioletowy", Hex = "#512BD4" },
        new ColorOption { Name = "Niebieski", Hex = "#2563EB" },
        new ColorOption { Name = "Zielony", Hex = "#16A34A" },
        new ColorOption { Name = "Morski", Hex = "#0D9488" },
        new ColorOption { Name = "Pomarańczowy", Hex = "#EA580C" },
        new ColorOption { Name = "Czerwony", Hex = "#DC2626" },
        new ColorOption { Name = "Różowy", Hex = "#DB2777" },
        new ColorOption { Name = "Ciemnoszary", Hex = "#475569" },
    };

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;

        ColorPicker.ItemsSource = AvailableColors;
        ColorPicker.SelectedIndex = 0;

        LoadCounters();
        UpdateState();
    }

    private async void OnAddCounterClicked(object? sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Błąd", "Wprowadź nazwę licznika.", "OK");
            return;
        }

        int initialValue = 0;
        if (!string.IsNullOrWhiteSpace(InitialValueEntry.Text))
        {
            if (!int.TryParse(InitialValueEntry.Text.Trim(), out initialValue))
            {
                await DisplayAlertAsync("Błąd", "Wartość początkowa musi być poprawną liczbą całkowitą.", "OK");
                return;
            }
        }

        var selectedColor = ColorPicker.SelectedItem as ColorOption;
        string colorHex = selectedColor?.Hex ?? "#512BD4";

        var newCounter = new Counter
        {
            Name = name,
            InitialValue = initialValue,
            Value = initialValue,
            ColorHex = colorHex
        };

        AttachCounterEvents(newCounter);
        Counters.Add(newCounter);
        SaveCounters();
        UpdateState();

        NameEntry.Text = string.Empty;
        InitialValueEntry.Text = string.Empty;
    }

    private void OnIncrementClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Counter counter)
        {
            counter.Value++;
            SaveCounters();
        }
    }

    private void OnDecrementClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Counter counter)
        {
            counter.Value--;
            SaveCounters();
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Counter counter)
        {
            counter.Value = counter.InitialValue;
            SaveCounters();
        }
    }

    private async void OnDeleteCounterClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Counter counter)
        {
            bool confirm = await DisplayAlertAsync(
                "Usuwanie licznika",
                $"Czy na pewno chcesz usunąć licznik \"{counter.Name}\"?",
                "Tak",
                "Nie");

            if (confirm)
            {
                Counters.Remove(counter);
                SaveCounters();
                UpdateState();
            }
        }
    }

    private async void OnChangeColorClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Counter counter)
        {
            string[] colorNames = AvailableColors.Select(c => c.Name).ToArray();
            string selected = await DisplayActionSheetAsync("Wybierz kolor licznika", "Anuluj", null, colorNames);

            if (!string.IsNullOrEmpty(selected) && selected != "Anuluj")
            {
                var match = AvailableColors.FirstOrDefault(c => c.Name == selected);
                if (match != null)
                {
                    counter.ColorHex = match.Hex;
                    SaveCounters();
                }
            }
        }
    }

    private void AttachCounterEvents(Counter counter)
    {
        counter.PropertyChanged += (s, e) =>
        {
            if (e?.PropertyName == nameof(Counter.Value))
            {
                SaveCounters();
            }
        };
    }

    private void SaveCounters()
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Counters, options);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd podczas zapisu danych: {ex.Message}");
        }
    }

    private void LoadCounters()
    {
        Counters.Clear();

        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                var items = JsonSerializer.Deserialize<List<Counter>>(json);
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        AttachCounterEvents(item);
                        Counters.Add(item);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas wczytywania danych: {ex.Message}");
            }
        }

        var defaultCounter = new Counter
        {
            Name = "Główny licznik",
            InitialValue = 0,
            Value = 0,
            ColorHex = "#512BD4"
        };
        AttachCounterEvents(defaultCounter);
        Counters.Add(defaultCounter);
        SaveCounters();
    }

    private void UpdateState()
    {
        EmptyStateLabel.IsVisible = Counters.Count == 0;
        CounterCountLabel.Text = $"Liczba: {Counters.Count}";
    }
}
