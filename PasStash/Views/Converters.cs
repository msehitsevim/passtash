using PassVault.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PassVault.Core.Models;
using PassVault.Core.Services;

namespace PassVault.Views;

/// <summary>Converts an AppView enum to Visibility based on ConverterParameter string.</summary>
public class ViewToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppView currentView && parameter is string targetViewName)
        {
            return currentView.ToString() == targetViewName ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts boolean to Visibility. ConverterParameter='Invert' flips the result.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolVal = value is bool b && b;
        bool invert = parameter is string s && s == "Invert";
        if (invert) boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts null to Visibility. Null or empty => Collapsed.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s == "Invert";
        bool isNull = value == null || (value is string str && string.IsNullOrEmpty(str));
        if (invert) isNull = !isNull;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts a password strength hex color string to a SolidColorBrush.</summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts VaultCategory to an icon geometry key.</summary>
public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VaultCategory cat)
        {
            string key = cat switch
            {
                VaultCategory.Login => "IconKey",
                VaultCategory.SecureNote => "IconNote",
                VaultCategory.Card => "IconCard",
                VaultCategory.Server => "IconServer",
                _ => "IconKey"
            };
            return Application.Current.TryFindResource(key) ?? Geometry.Empty;
        }
        return Geometry.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts VaultCategory to a vibrant visual gradient brush for badges and item avatars.</summary>
public class CategoryToBrushConverter : IValueConverter
{
    private static readonly LinearGradientBrush LoginBrush = new(Color.FromRgb(59, 130, 246), Color.FromRgb(6, 182, 212), new Point(0, 0), new Point(1, 1));
    private static readonly LinearGradientBrush NoteBrush = new(Color.FromRgb(139, 92, 246), Color.FromRgb(168, 85, 247), new Point(0, 0), new Point(1, 1));
    private static readonly LinearGradientBrush CardBrush = new(Color.FromRgb(16, 185, 129), Color.FromRgb(5, 150, 105), new Point(0, 0), new Point(1, 1));
    private static readonly LinearGradientBrush ServerBrush = new(Color.FromRgb(236, 72, 153), Color.FromRgb(244, 63, 94), new Point(0, 0), new Point(1, 1));
    private static readonly LinearGradientBrush DefaultBrush = new(Color.FromRgb(245, 158, 11), Color.FromRgb(217, 119, 6), new Point(0, 0), new Point(1, 1));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VaultCategory cat)
        {
            return cat switch
            {
                VaultCategory.Login => LoginBrush,
                VaultCategory.SecureNote => NoteBrush,
                VaultCategory.Card => CardBrush,
                VaultCategory.Server => ServerBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts VaultCategory to Turkish display name.</summary>
public class CategoryToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VaultCategory cat)
        {
            return cat switch
            {
                VaultCategory.Login => "Oturum A\u00E7ma",
                VaultCategory.SecureNote => "G\u00FCvenli Not",
                VaultCategory.Card => "Kredi Kart\u0131",
                VaultCategory.Server => "Sunucu",
                _ => cat.ToString()
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Provides an intelligent subtitle for vault list items (Note preview for SecureNote, Username for Login, etc.).</summary>
public class VaultItemSubtitleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VaultItem item)
        {
            switch (item.Category)
            {
                case VaultCategory.SecureNote:
                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        string[] lines = item.Notes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            string firstLine = lines[0].Trim();
                            return firstLine.Length > 42 ? firstLine.Substring(0, 42) + "..." : firstLine;
                        }
                    }
                    return "G\u00FCvenli Not";

                case VaultCategory.Card:
                    return !string.IsNullOrWhiteSpace(item.Username) ? item.Username : "\u00D6deme Kart\u0131";

                case VaultCategory.Server:
                    if (!string.IsNullOrWhiteSpace(item.Url)) return item.Url;
                    if (!string.IsNullOrWhiteSpace(item.Username)) return item.Username;
                    return "Sunucu";

                case VaultCategory.Login:
                default:
                    if (!string.IsNullOrWhiteSpace(item.Username)) return item.Username;
                    if (!string.IsNullOrWhiteSpace(item.Url)) return item.Url;
                    return "Oturum A\u00E7ma";
            }
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Returns Collapsed if Category == SecureNote, else Visible. Parameter 'Invert' returns Visible for SecureNote.</summary>
public class SecureNoteVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSecureNote = value is VaultCategory cat && cat == VaultCategory.SecureNote;
        bool invert = parameter is string s && s == "Invert";
        if (invert) return isSecureNote ? Visibility.Visible : Visibility.Collapsed;
        return isSecureNote ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Returns Visible if item is a SecureNote OR if it has non-empty Notes.</summary>
public class NoteCardVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is VaultItem item)
        {
            if (item.Category == VaultCategory.SecureNote) return Visibility.Visible;
            return !string.IsNullOrWhiteSpace(item.Notes) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Returns dynamic height for notes edit box (160 for SecureNote, 80 for other categories).</summary>
public class NoteBoxHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is VaultCategory cat && cat == VaultCategory.SecureNote) ? 160.0 : 80.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts a boolean IsFavorite to a star icon geometry.</summary>
public class FavoriteToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isFav = value is bool b && b;
        string key = isFav ? "IconStar" : "IconStarOutline";
        return Application.Current.TryFindResource(key) ?? Geometry.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts a password string to masked dots or reveals it.</summary>
public class PasswordMaskConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is string password && values[1] is bool isVisible)
        {
            return isVisible ? password : new string('\u2022', Math.Min(password.Length, 30));
        }
        return new string('\u2022', 8);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts strength score (0-100) to a width percentage for progress bar.</summary>
public class ScoreToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            return new GridLength(Math.Max(score, 2), GridUnitType.Star);
        }
        return new GridLength(2, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Complement converter for strength bar.</summary>
public class ScoreToRemainingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            return new GridLength(Math.Max(100 - score, 0), GridUnitType.Star);
        }
        return new GridLength(98, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Converts bool IsDarkTheme to moon/sun emoji.</summary>
public class ThemeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool isDark && isDark) ? "\u2600" : "\uD83C\uDF19";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}