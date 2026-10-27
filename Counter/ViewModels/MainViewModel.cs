using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Counter.Models;
using Counter.Services;

namespace Counter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public const long MaxSafeValue = 9_000_000_000_000_000L;
    public const long MinSafeValue = -9_000_000_000_000_000L;

    private readonly ICounterRepository _repository;
    private bool _isHydrated;

    public ObservableCollection<CounterItem> Counters { get; } = new();

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private string _newInitialValueText = "0";

    [ObservableProperty]
    private string _selectedColorHex = CounterItem.DefaultColorHex;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationError;

    public IReadOnlyList<string> AvailableColors { get; } =
    [
        "#E60033",
        "#FFBA00",
        "#1F3A8A",
        "#38B48B",
        "#8EBEDC",
        "#036635",
        "#8B5FBF",
        "#F08300"
    ];

    public MainViewModel(ICounterRepository repository)
    {
        _repository = repository;
    }

    [RelayCommand]
    public async Task LoadCountersAsync()
    {
        if (_isHydrated)
            return;

        var items = await _repository.HydrateAllAsync();
        Counters.Clear();
        foreach (var item in items)
        {
            Counters.Add(item);
        }

        _isHydrated = true;
    }

    [RelayCommand]
    private void SelectColor(string? colorHex)
    {
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            SelectedColorHex = CounterItem.NormalizeColorHex(colorHex);
        }
    }

    [RelayCommand]
    public async Task AddCounterAsync()
    {
        ClearValidationError();

        var trimmedName = (NewName ?? string.Empty).Trim();
        if (trimmedName.Length is 0 or > 64)
        {
            SetValidationError("Nazwa licznika musi mieć od 1 do 64 znaków.");
            return;
        }

        if (Counters.Any(c => string.Equals(c.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            SetValidationError("Licznik o takiej nazwie już istnieje.");
            return;
        }

        var rawValueText = string.IsNullOrWhiteSpace(NewInitialValueText) ? "0" : NewInitialValueText.Trim();
        if (!long.TryParse(rawValueText, out long parsedInitialValue) ||
            parsedInitialValue < MinSafeValue ||
            parsedInitialValue > MaxSafeValue)
        {
            SetValidationError("Podaj poprawną liczbę całkowitą dla wartości początkowej.");
            return;
        }

        var now = DateTime.UtcNow;
        var counter = new CounterItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = trimmedName,
            InitialValue = parsedInitialValue,
            CurrentValue = parsedInitialValue,
            ColorHex = CounterItem.NormalizeColorHex(SelectedColorHex),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsSynced = false,
            Revision = 1
        };

        Counters.Insert(0, counter);
        await _repository.InsertCounterAsync(counter);

        NewName = string.Empty;
        NewInitialValueText = "0";
    }

    [RelayCommand]
    public void Increment(CounterItem? counter)
    {
        if (counter == null || counter.CurrentValue >= MaxSafeValue)
            return;

        counter.CurrentValue++;
        CommitMutation(counter);
    }

    [RelayCommand]
    public void Decrement(CounterItem? counter)
    {
        if (counter == null || counter.CurrentValue <= MinSafeValue)
            return;

        counter.CurrentValue--;
        CommitMutation(counter);
    }

    [RelayCommand]
    public void ResetCounter(CounterItem? counter)
    {
        if (counter == null || counter.CurrentValue == counter.InitialValue)
            return;

        counter.CurrentValue = counter.InitialValue;
        CommitMutation(counter);
    }

    [RelayCommand]
    public async Task DeleteCounterAsync(CounterItem? counter)
    {
        if (counter == null)
            return;

        Counters.Remove(counter);
        await _repository.DeleteCounterAsync(counter.Id);
    }

    private void CommitMutation(CounterItem counter)
    {
        counter.Revision++;
        counter.UpdatedAtUtc = DateTime.UtcNow;
        counter.IsSynced = false;
        _repository.EnqueueAutoSave(counter);
    }

    private void SetValidationError(string message)
    {
        ValidationMessage = message;
        HasValidationError = true;
    }

    private void ClearValidationError()
    {
        ValidationMessage = string.Empty;
        HasValidationError = false;
    }
}
