using System.Windows.Input;
using PassVault.Core.Crypto;
using PassVault.Core.Services;

namespace PassVault.ViewModels;

public class GeneratorViewModel : BaseViewModel
{
    private int _length = 18;
    private bool _includeUppercase = true;
    private bool _includeLowercase = true;
    private bool _includeDigits = true;
    private bool _includeSymbols = true;
    private bool _avoidAmbiguous = true;
    private string _generatedPassword = string.Empty;
    private PasswordStrengthResult _strengthResult = new();

    public int Length
    {
        get => _length;
        set
        {
            if (SetProperty(ref _length, value))
            {
                Generate();
            }
        }
    }

    public bool IncludeUppercase
    {
        get => _includeUppercase;
        set
        {
            if (SetProperty(ref _includeUppercase, value))
            {
                EnsureAtLeastOneOption();
                Generate();
            }
        }
    }

    public bool IncludeLowercase
    {
        get => _includeLowercase;
        set
        {
            if (SetProperty(ref _includeLowercase, value))
            {
                EnsureAtLeastOneOption();
                Generate();
            }
        }
    }

    public bool IncludeDigits
    {
        get => _includeDigits;
        set
        {
            if (SetProperty(ref _includeDigits, value))
            {
                EnsureAtLeastOneOption();
                Generate();
            }
        }
    }

    public bool IncludeSymbols
    {
        get => _includeSymbols;
        set
        {
            if (SetProperty(ref _includeSymbols, value))
            {
                EnsureAtLeastOneOption();
                Generate();
            }
        }
    }

    public bool AvoidAmbiguous
    {
        get => _avoidAmbiguous;
        set
        {
            if (SetProperty(ref _avoidAmbiguous, value))
            {
                Generate();
            }
        }
    }

    public string GeneratedPassword
    {
        get => _generatedPassword;
        set => SetProperty(ref _generatedPassword, value);
    }

    public PasswordStrengthResult StrengthResult
    {
        get => _strengthResult;
        set => SetProperty(ref _strengthResult, value);
    }

    public ICommand RegenerateCommand { get; }
    public ICommand CopyCommand { get; }

    public Action<string>? OnPasswordSelected { get; set; }

    public GeneratorViewModel()
    {
        RegenerateCommand = new RelayCommand(Generate);
        CopyCommand = new RelayCommand(() =>
        {
            ClipboardService.CopyWithAutoClear(GeneratedPassword, "Oluşturulan Parola");
        });

        Generate();
    }

    private void EnsureAtLeastOneOption()
    {
        if (!IncludeUppercase && !IncludeLowercase && !IncludeDigits && !IncludeSymbols)
        {
            _includeLowercase = true;
            OnPropertyChanged(nameof(IncludeLowercase));
        }
    }

    public void Generate()
    {
        var options = new PasswordGeneratorOptions
        {
            Length = Length,
            IncludeUppercase = IncludeUppercase,
            IncludeLowercase = IncludeLowercase,
            IncludeDigits = IncludeDigits,
            IncludeSymbols = IncludeSymbols,
            AvoidAmbiguous = AvoidAmbiguous
        };

        GeneratedPassword = PasswordGenerator.Generate(options);
        StrengthResult = PasswordGenerator.EvaluateStrength(GeneratedPassword);
    }
}
