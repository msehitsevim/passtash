using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using PassVault.Core.Crypto;
using PassVault.Core.Models;
using PassVault.Core.Services;

namespace PassVault.ViewModels;

public enum AppView
{
    Setup,
    Unlock,
    Vault
}

public class MainViewModel : BaseViewModel
{
    private readonly VaultStorageService _storage;
    private string _masterPassword = string.Empty;
    private VaultData _vaultData = new();

    // â”€â”€ Navigation â”€â”€
    private AppView _currentView;
    public AppView CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    // â”€â”€ Setup / Unlock fields â”€â”€
    private string _passwordInput = string.Empty;
    public string PasswordInput
    {
        get => _passwordInput;
        set
        {
            if (SetProperty(ref _passwordInput, value))
            {
                SetupStrength = PasswordGenerator.EvaluateStrength(value);
            }
        }
    }

    private string _passwordConfirm = string.Empty;
    public string PasswordConfirm
    {
        get => _passwordConfirm;
        set => SetProperty(ref _passwordConfirm, value);
    }

    private PasswordStrengthResult _setupStrength = new();
    public PasswordStrengthResult SetupStrength
    {
        get => _setupStrength;
        set => SetProperty(ref _setupStrength, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // â”€â”€ Vault items â”€â”€
    public ObservableCollection<VaultItem> AllItems { get; } = new();
    public ObservableCollection<VaultItem> FilteredItems { get; } = new();
    public ObservableCollection<string> Folders { get; } = new();

    private VaultItem? _selectedItem;
    public VaultItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                IsEditing = false;
                if (value != null)
                {
                    EditingItem = value.Clone();
                    SelectedItemPasswordStrength = PasswordGenerator.EvaluateStrength(value.Password);
                }
                else
                {
                    EditingItem = null;
                    SelectedItemPasswordStrength = new();
                }
                IsPasswordVisible = false;
            }
        }
    }

    private VaultItem? _editingItem;
    public VaultItem? EditingItem
    {
        get => _editingItem;
        set => SetProperty(ref _editingItem, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private bool _isPasswordVisible;
    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }

    private PasswordStrengthResult _selectedItemPasswordStrength = new();
    public PasswordStrengthResult SelectedItemPasswordStrength
    {
        get => _selectedItemPasswordStrength;
        set => SetProperty(ref _selectedItemPasswordStrength, value);
    }

    // â”€â”€ Dedicated Editing Fields (Two-Way Observable) â”€â”€
    private string _editTitle = string.Empty;
    public string EditTitle
    {
        get => _editTitle;
        set => SetProperty(ref _editTitle, value);
    }

    private string _editUsername = string.Empty;
    public string EditUsername
    {
        get => _editUsername;
        set => SetProperty(ref _editUsername, value);
    }

    private string _editPassword = string.Empty;
    public string EditPassword
    {
        get => _editPassword;
        set => SetProperty(ref _editPassword, value);
    }

    private string _editUrl = string.Empty;
    public string EditUrl
    {
        get => _editUrl;
        set => SetProperty(ref _editUrl, value);
    }

    private string _editNotes = string.Empty;
    public string EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    private VaultCategory _editCategory = VaultCategory.Login;
    public VaultCategory EditCategory
    {
        get => _editCategory;
        set
        {
            if (SetProperty(ref _editCategory, value))
            {
                OnPropertyChanged(nameof(IsEditSecureNote));
                OnPropertyChanged(nameof(IsEditNotSecureNote));
            }
        }
    }

    public bool IsEditSecureNote => EditCategory == VaultCategory.SecureNote;
    public bool IsEditNotSecureNote => EditCategory != VaultCategory.SecureNote;

    // â”€â”€ Search / Filter â”€â”€
    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    private VaultCategory? _selectedCategory;
    public VaultCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    private bool _showFavoritesOnly;
    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            if (SetProperty(ref _showFavoritesOnly, value))
            {
                ApplyFilter();
            }
        }
    }

    private string? _selectedFolder;
    public string? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                ApplyFilter();
            }
        }
    }

    public int TotalItemCount => AllItems.Count;

    // â”€â”€ Generator â”€â”€
    private bool _isGeneratorOpen;
    public bool IsGeneratorOpen
    {
        get => _isGeneratorOpen;
        set => SetProperty(ref _isGeneratorOpen, value);
    }

    private GeneratorViewModel _generatorVM = new();
    public GeneratorViewModel GeneratorVM
    {
        get => _generatorVM;
        set => SetProperty(ref _generatorVM, value);
    }

    // â”€â”€ Inactivity auto-lock â”€â”€
    private readonly DispatcherTimer _inactivityTimer;
    private DateTime _lastActivity = DateTime.UtcNow;
    private const int AutoLockMinutes = 15;

    // â”€â”€ Categories List for ComboBox â”€â”€
    public IEnumerable<VaultCategory> AvailableCategories { get; } = Enum.GetValues<VaultCategory>();

    // â”€â”€ Commands â”€â”€
    public ICommand CreateVaultCommand { get; }
    public ICommand UnlockCommand { get; }
    public ICommand LockCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand StartEditCommand { get; }
    public ICommand SaveItemCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand CopyUsernameCommand { get; }
    public ICommand CopyPasswordCommand { get; }
    public ICommand TogglePasswordVisibilityCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand OpenGeneratorCommand { get; }
    public ICommand CloseGeneratorCommand { get; }
    public ICommand UseGeneratedPasswordCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public bool IsDarkTheme => ThemeService.CurrentTheme == AppTheme.Dark;
    public ICommand ToggleThemeCommand { get; }
    public ICommand ExportVaultCommand { get; }
    public ICommand ImportVaultCommand { get; }

    public MainViewModel()
    {
        _storage = new VaultStorageService();

        ClipboardService.OnClipboardNotification += msg => StatusMessage = msg;

        CurrentView = _storage.VaultExists() ? AppView.Unlock : AppView.Setup;

        CreateVaultCommand = new RelayCommand(CreateVault);
        UnlockCommand = new RelayCommand(UnlockVault);
        LockCommand = new RelayCommand(LockVault);

        AddItemCommand = new RelayCommand(AddItem);
        EditItemCommand = new RelayCommand(StartEdit);
        StartEditCommand = new RelayCommand(StartEdit);
        SaveItemCommand = new RelayCommand(SaveItem);
        CancelEditCommand = new RelayCommand(CancelEdit);
        DeleteItemCommand = new RelayCommand(DeleteItem);

        CopyUsernameCommand = new RelayCommand(CopyUsername);
        CopyPasswordCommand = new RelayCommand(CopyPassword);
        TogglePasswordVisibilityCommand = new RelayCommand(() => IsPasswordVisible = !IsPasswordVisible);

        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);

        OpenGeneratorCommand = new RelayCommand(() =>
        {
            GeneratorVM = new GeneratorViewModel();
            IsGeneratorOpen = true;
        });
        CloseGeneratorCommand = new RelayCommand(() => IsGeneratorOpen = false);
        UseGeneratedPasswordCommand = new RelayCommand(UseGeneratedPassword);

        ClearFilterCommand = new RelayCommand(ClearFilter);
        SelectCategoryCommand = new RelayCommand(p =>
        {
            if (p is VaultCategory cat)
            {
                SelectedCategory = SelectedCategory == cat ? null : cat;
            }
            else if (p is string s && Enum.TryParse<VaultCategory>(s, out var parsedCat))
            {
                SelectedCategory = SelectedCategory == parsedCat ? null : parsedCat;
            }
        });

        ToggleThemeCommand = new RelayCommand(() =>
        {
            ThemeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
            StatusMessage = ThemeService.CurrentTheme == AppTheme.Dark ? "Koyu tema aktif" : "A\u00E7\u0131k tema aktif";
        });
        ThemeService.OnThemeChanged += _ => OnPropertyChanged(nameof(IsDarkTheme));

        ExportVaultCommand = new RelayCommand(ExportVault);
        ImportVaultCommand = new RelayCommand(ImportVault);

        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _inactivityTimer.Tick += (_, _) =>
        {
            if (CurrentView == AppView.Vault && (DateTime.UtcNow - _lastActivity).TotalMinutes >= AutoLockMinutes)
            {
                LockVault();
            }
        };
    }

    public void ResetInactivityTimer()
    {
        _lastActivity = DateTime.UtcNow;
    }

    // â”€â”€ Setup â”€â”€
    private async void CreateVault()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            ErrorMessage = "Ana parola bo\u015F olamaz.";
            return;
        }

        if (PasswordInput.Length < 8)
        {
            ErrorMessage = "Ana parola en az 8 karakter olmal\u0131d\u0131r.";
            return;
        }

        if (PasswordInput != PasswordConfirm)
        {
            ErrorMessage = "Parolalar birbiriyle e\u015Fle\u015Fmiyor.";
            return;
        }

        IsLoading = true;
        try
        {
            _masterPassword = PasswordInput;
            _vaultData = new VaultData
            {
                Items = new List<VaultItem>(),
                Folders = new List<string> { "\u0130\u015F", "Ki\u015Fisel", "Finans" }
            };

            await Task.Run(() => _storage.SaveVault(_vaultData, _masterPassword));

            LoadVaultDataIntoCollections();
            CurrentView = AppView.Vault;
            StatusMessage = "Kasa ba\u015Far\u0131yla olu\u015Fturuldu.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kasa olu\u015Fturulamad\u0131: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            PasswordInput = string.Empty;
            PasswordConfirm = string.Empty;
        }
    }

    // â”€â”€ Unlock â”€â”€
    private async void UnlockVault()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            ErrorMessage = "L\u00FCtfen ana parolan\u0131z\u0131 girin.";
            return;
        }

        IsLoading = true;
        try
        {
            string enteredPwd = PasswordInput;
            VaultData loadedData = await Task.Run(() => _storage.LoadVault(enteredPwd));

            _masterPassword = enteredPwd;
            _vaultData = loadedData;

            LoadVaultDataIntoCollections();
            CurrentView = AppView.Vault;
            StatusMessage = "Kasa a\u00E7\u0131ld\u0131.";
        }
        catch (CryptographicException)
        {
            ErrorMessage = "Ana parola hatal\u0131. L\u00FCtfen tekrar deneyin.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Kasa a\u00E7\u0131lamad\u0131: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            PasswordInput = string.Empty;
        }
    }

    // â”€â”€ Lock â”€â”€
    public void LockVault()
    {
        _masterPassword = string.Empty;
        _vaultData = new VaultData();
        AllItems.Clear();
        FilteredItems.Clear();
        Folders.Clear();
        SelectedItem = null;
        EditingItem = null;
        IsEditing = false;
        CurrentView = AppView.Unlock;
        StatusMessage = "Kasa kilitlendi.";
    }

    private void LoadVaultDataIntoCollections()
    {
        AllItems.Clear();
        foreach (var item in _vaultData.Items)
        {
            AllItems.Add(item);
        }

        Folders.Clear();
        foreach (var folder in _vaultData.Folders)
        {
            Folders.Add(folder);
        }

        OnPropertyChanged(nameof(TotalItemCount));
        ApplyFilter();
    }

    private async void PersistVault()
    {
        if (string.IsNullOrEmpty(_masterPassword)) return;

        _vaultData.Items = AllItems.ToList();
        _vaultData.Folders = Folders.ToList();
        try
        {
            await Task.Run(() => _storage.SaveVault(_vaultData, _masterPassword));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Kaydetme hatas\u0131: {ex.Message}";
        }
    }

    // â”€â”€ CRUD â”€â”€
    private void AddItem()
    {
        var category = SelectedCategory ?? VaultCategory.Login;
        string defaultTitle = category switch
        {
            VaultCategory.SecureNote => "Yeni Not",
            VaultCategory.Card => "Yeni Kart",
            VaultCategory.Server => "Yeni Sunucu",
            _ => "Yeni Kay\u0131t"
        };

        var newItem = new VaultItem
        {
            Title = defaultTitle,
            Category = category
        };
        AllItems.Add(newItem);
        OnPropertyChanged(nameof(TotalItemCount));
        ApplyFilter();
        SelectedItem = newItem;

        // Initialize editing fields
        EditTitle = newItem.Title;
        EditUsername = string.Empty;
        EditPassword = string.Empty;
        EditUrl = string.Empty;
        EditNotes = string.Empty;
        EditCategory = category;

        EditingItem = newItem.Clone();
        IsEditing = true;
        ResetInactivityTimer();
    }

    private void StartEdit()
    {
        if (SelectedItem == null) return;

        EditTitle = SelectedItem.Title;
        EditUsername = SelectedItem.Username;
        EditPassword = SelectedItem.Password;
        EditUrl = SelectedItem.Url;
        EditNotes = SelectedItem.Notes;
        EditCategory = SelectedItem.Category;

        EditingItem = SelectedItem.Clone();
        IsEditing = true;
        ResetInactivityTimer();
    }

    private void SaveItem()
    {
        if (SelectedItem == null) return;

        var targetItem = SelectedItem;
        targetItem.UpdatedAt = DateTime.UtcNow;

        targetItem.Title = string.IsNullOrWhiteSpace(EditTitle) ? "\u0130simsiz Kay\u0131t" : EditTitle.Trim();
        targetItem.Category = EditCategory;
        targetItem.Notes = EditNotes ?? string.Empty;

        if (EditCategory == VaultCategory.SecureNote)
        {
            targetItem.Username = string.Empty;
            targetItem.Password = string.Empty;
            targetItem.Url = string.Empty;
        }
        else
        {
            targetItem.Username = EditUsername ?? string.Empty;
            targetItem.Password = EditPassword ?? string.Empty;
            targetItem.Url = EditUrl ?? string.Empty;
        }

        IsEditing = false;
        SelectedItemPasswordStrength = PasswordGenerator.EvaluateStrength(targetItem.Password);
        ApplyFilter();
        SelectedItem = targetItem;
        PersistVault();
        StatusMessage = $"\"{targetItem.Title}\" kaydedildi.";
        ResetInactivityTimer();
    }

    private void CancelEdit()
    {
        IsEditing = false;
        if (SelectedItem != null)
        {
            EditingItem = SelectedItem.Clone();
        }
    }

    private void DeleteItem(object? param = null)
    {
        var itemToDelete = param as VaultItem ?? SelectedItem;
        if (itemToDelete == null) return;

        var result = MessageBox.Show(
            $"\"{itemToDelete.Title}\" kayd\u0131n\u0131 silmek istedi\u011Finizden emin misiniz?",
            "Kayd\u0131 Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            AllItems.Remove(itemToDelete);
            OnPropertyChanged(nameof(TotalItemCount));
            ApplyFilter();

            if (SelectedItem == itemToDelete)
            {
                SelectedItem = FilteredItems.FirstOrDefault();
            }

            PersistVault();
            StatusMessage = $"\"{itemToDelete.Title}\" silindi.";
            ResetInactivityTimer();
        }
    }

    // â”€â”€ Clipboard operations â”€â”€
    private void CopyUsername()
    {
        if (SelectedItem == null || string.IsNullOrEmpty(SelectedItem.Username)) return;
        ClipboardService.CopyWithAutoClear(SelectedItem.Username, "Kullan\u0131c\u0131 ad\u0131");
        ResetInactivityTimer();
    }

    private void CopyPassword()
    {
        if (SelectedItem == null || string.IsNullOrEmpty(SelectedItem.Password)) return;
        ClipboardService.CopyWithAutoClear(SelectedItem.Password, "Parola");
        ResetInactivityTimer();
    }

    // â”€â”€ Favorite â”€â”€
    private void ToggleFavorite(object? param = null)
    {
        var item = param as VaultItem ?? SelectedItem;
        if (item == null) return;

        item.IsFavorite = !item.IsFavorite;
        OnPropertyChanged(nameof(SelectedItem));
        ApplyFilter();
        PersistVault();
        ResetInactivityTimer();
    }

    // â”€â”€ Generator â”€â”€
    private void UseGeneratedPassword()
    {
        if (IsEditing)
        {
            EditPassword = GeneratorVM.GeneratedPassword;
            if (EditingItem != null)
            {
                EditingItem.Password = GeneratorVM.GeneratedPassword;
                OnPropertyChanged(nameof(EditingItem));
            }
        }
        IsGeneratorOpen = false;
        ResetInactivityTimer();
    }

    // â”€â”€ Filter â”€â”€
    private void ApplyFilter()
    {
        FilteredItems.Clear();

        IEnumerable<VaultItem> items = AllItems;

        if (SelectedCategory.HasValue)
        {
            items = items.Where(i => i.Category == SelectedCategory.Value);
        }

        if (ShowFavoritesOnly)
        {
            items = items.Where(i => i.IsFavorite);
        }

        if (!string.IsNullOrEmpty(SelectedFolder))
        {
            items = items.Where(i => i.Folder == SelectedFolder);
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string q = SearchQuery.Trim().ToLowerInvariant();
            items = items.Where(i =>
                (i.Title?.ToLowerInvariant().Contains(q) ?? false) ||
                (i.Username?.ToLowerInvariant().Contains(q) ?? false) ||
                (i.Url?.ToLowerInvariant().Contains(q) ?? false) ||
                (i.Notes?.ToLowerInvariant().Contains(q) ?? false));
        }

        foreach (var item in items)
        {
            FilteredItems.Add(item);
        }
    }

    private void ClearFilter()
    {
        SearchQuery = string.Empty;
        SelectedCategory = null;
        ShowFavoritesOnly = false;
        SelectedFolder = null;
        ApplyFilter();
    }

    // â”€â”€ Export / Import Backup â”€â”€
    private void ExportVault()
    {
        if (CurrentView != AppView.Vault || string.IsNullOrEmpty(_masterPassword)) return;

        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Sand\u0131\u011F\u0131 D\u0131\u015Fa Aktar (Yedekle)",
            Filter = "PasStash \u015Eifreli Kasa (*.stash)|*.stash|\u015Eifresiz JSON (*.json)|*.json",
            FileName = $"PasStash_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.stash",
            AddExtension = true
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                _vaultData.Items = AllItems.ToList();
                _vaultData.Folders = Folders.ToList();

                string ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                if (ext == ".json")
                {
                    var result = MessageBox.Show(
                        "D\u0130KKAT: JSON format\u0131 \u015Fifresiz d\u00FCz metin i\u00E7erir. Bu dosyay\u0131 g\u00FCvenli olmayan yerlerde payla\u015Fmay\u0131n\u0131z.\n\nDevam etmek istiyor musunuz?",
                        "G\u00FCvenlik Uyar\u0131s\u0131",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;
                    _storage.ExportJson(sfd.FileName, _vaultData);
                }
                else
                {
                    _storage.ExportEncrypted(sfd.FileName, _masterPassword, _vaultData);
                }

                StatusMessage = "Kasa yede\u011Fi ba\u015Far\u0131yla d\u0131\u015Fa aktar\u0131ld\u0131.";
                MessageBox.Show(
                    "Kasa yede\u011Finiz ba\u015Far\u0131yla kaydedildi:\n\n" + sfd.FileName,
                    "D\u0131\u015Fa Aktarma Ba\u015Far\u0131l\u0131",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"D\u0131\u015Fa aktarma hatas\u0131: {ex.Message}";
                MessageBox.Show($"D\u0131\u015Fa aktar\u0131l\u0131rken hata olu\u015Ftu:\n\n{ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        ResetInactivityTimer();
    }

    private void ImportVault()
    {
        if (CurrentView != AppView.Vault || string.IsNullOrEmpty(_masterPassword)) return;

        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Kasa Yede\u011Fi \u0130\u00E7e Aktar",
            Filter = "Desteklenen Yedekler (*.stash;*.dat;*.json)|*.stash;*.dat;*.json|T\u00FCm Dosyalar (*.*)|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                var importedData = _storage.ImportFromFile(ofd.FileName, _masterPassword);
                if (importedData == null || importedData.Items == null || importedData.Items.Count == 0)
                {
                    MessageBox.Show("Se\u00E7ilen dosyada i\u00E7e aktar\u0131lacak kay\u0131t bulunamad\u0131.", "Uyar\u0131", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int addedCount = 0;
                var existingIds = new HashSet<Guid>(AllItems.Select(x => x.Id));

                foreach (var item in importedData.Items)
                {
                    if (existingIds.Contains(item.Id))
                    {
                        item.Id = Guid.NewGuid();
                    }
                    AllItems.Add(item);
                    existingIds.Add(item.Id);
                    addedCount++;
                }

                if (importedData.Folders != null)
                {
                    foreach (var folder in importedData.Folders)
                    {
                        if (!string.IsNullOrWhiteSpace(folder) && !Folders.Contains(folder))
                        {
                            Folders.Add(folder);
                        }
                    }
                }

                OnPropertyChanged(nameof(TotalItemCount));
                ApplyFilter();
                PersistVault();

                StatusMessage = $"{addedCount} adet kay\u0131t ba\u015Far\u0131yla i\u00E7e aktar\u0131ld\u0131.";
                MessageBox.Show(
                    $"{addedCount} adet kay\u0131t kasan\u0131za ba\u015Far\u0131yla eklendi!",
                    "\u0130\u00E7e Aktarma Ba\u015Far\u0131l\u0131",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"\u0130\u00E7e aktarma hatas\u0131: {ex.Message}";
                MessageBox.Show($"\u0130\u00E7e aktar\u0131l\u0131rken hata olu\u015Ftu. Dosya \u015Fifrelenmi\u015F ise ana parolan\u0131z\u0131n bu yedekle e\u015Fle\u015Fti\u011Finden emin olunuz.\n\nDetay: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        ResetInactivityTimer();
    }
}