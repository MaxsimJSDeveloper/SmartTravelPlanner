using System;

namespace SmartTravelPlanner.ViewModels;

public class FieldViewModel : ViewModelBase
{
    private string _text = string.Empty;
    private string? _error;

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value ?? string.Empty))
            {
                Error = null;
            }
        }
    }

    public string? Error
    {
        get => _error;
        private set
        {
            if (SetProperty(ref _error, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => _error is not null;

    public bool Validate(Func<string?, string?> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        Error = validator(Text);
        return Error is null;
    }

    public void SetError(string message) => Error = message;

    public void Reset()
    {
        Text = string.Empty;
        Error = null;
    }
}
