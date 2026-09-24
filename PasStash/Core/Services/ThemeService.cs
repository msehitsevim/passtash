using System;
using System.Windows;

namespace PassVault.Core.Services;

public enum AppTheme
{
    Dark,
    Light
}

public static class ThemeService
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static event Action<AppTheme>? OnThemeChanged;

    public static void ToggleTheme()
    {
        SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
    }

    public static void SetTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => SetTheme(theme));
            return;
        }

        CurrentTheme = theme;

        if (app?.Resources != null)
        {
            string themeFile = theme == AppTheme.Dark ? "Styles/ThemeDark.xaml" : "Styles/ThemeLight.xaml";
            ResourceDictionary? newDict = null;

            try
            {
                var packUri = new Uri($"pack://application:,,,/PasStash;component/{themeFile}", UriKind.Absolute);
                newDict = new ResourceDictionary { Source = packUri };
            }
            catch (Exception ex1)
            {
                System.Diagnostics.Debug.WriteLine($"Failed pack URI: {ex1.Message}");
                try
                {
                    var relUri = new Uri($"/PasStash;component/{themeFile}", UriKind.RelativeOrAbsolute);
                    newDict = new ResourceDictionary { Source = relUri };
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed relative URI: {ex2.Message}");
                }
            }

            if (newDict != null)
            {
                var merged = app.Resources.MergedDictionaries;
                ResourceDictionary? oldThemeDict = null;

                foreach (var d in merged)
                {
                    if (d.Source != null &&
                        (d.Source.OriginalString.Contains("Theme") || d.Source.OriginalString.Contains("Colors")))
                    {
                        oldThemeDict = d;
                        break;
                    }
                }

                if (oldThemeDict != null)
                {
                    int index = merged.IndexOf(oldThemeDict);
                    merged[index] = newDict;
                }
                else
                {
                    merged.Insert(0, newDict);
                }

                foreach (var key in newDict.Keys)
                {
                    app.Resources[key] = newDict[key];
                }

                app.MainWindow?.InvalidateVisual();
            }
        }

        OnThemeChanged?.Invoke(theme);
    }
}