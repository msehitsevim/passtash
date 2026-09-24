using System.Security.Cryptography;
using System.Text;

namespace PassVault.Core.Crypto;

public class PasswordGeneratorOptions
{
    public int Length { get; set; } = 16;
    public bool IncludeUppercase { get; set; } = true;
    public bool IncludeLowercase { get; set; } = true;
    public bool IncludeDigits { get; set; } = true;
    public bool IncludeSymbols { get; set; } = true;
    public bool AvoidAmbiguous { get; set; } = true;
}

public enum PasswordStrengthLevel
{
    VeryWeak,
    Weak,
    Fair,
    Strong,
    VeryStrong
}

public class PasswordStrengthResult
{
    public PasswordStrengthLevel Level { get; set; }
    public string Label { get; set; } = string.Empty;
    public double EntropyBits { get; set; }
    public string HexColor { get; set; } = "#EF4444";
    public double ScorePercent { get; set; }
}

public static class PasswordGenerator
{
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // without I, O when ambiguous
    private const string UppercaseAll = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz"; // without l when ambiguous
    private const string LowercaseAll = "abcdefghijklmnopqrstuvwxyz";
    private const string DigitChars = "23456789"; // without 0, 1 when ambiguous
    private const string DigitAll = "0123456789";
    private const string SymbolChars = "!@#$%^&*()-_=+[]{}|;:,.<>?";

    public static string Generate(PasswordGeneratorOptions options)
    {
        var charPool = new StringBuilder();
        var guaranteedChars = new List<char>();

        string upper = options.AvoidAmbiguous ? UppercaseChars : UppercaseAll;
        string lower = options.AvoidAmbiguous ? LowercaseChars : LowercaseAll;
        string digits = options.AvoidAmbiguous ? DigitChars : DigitAll;
        string symbols = SymbolChars;

        if (options.IncludeUppercase)
        {
            charPool.Append(upper);
            guaranteedChars.Add(upper[RandomNumberGenerator.GetInt32(upper.Length)]);
        }
        if (options.IncludeLowercase)
        {
            charPool.Append(lower);
            guaranteedChars.Add(lower[RandomNumberGenerator.GetInt32(lower.Length)]);
        }
        if (options.IncludeDigits)
        {
            charPool.Append(digits);
            guaranteedChars.Add(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
        }
        if (options.IncludeSymbols)
        {
            charPool.Append(symbols);
            guaranteedChars.Add(symbols[RandomNumberGenerator.GetInt32(symbols.Length)]);
        }

        if (charPool.Length == 0)
        {
            // Default fallback
            charPool.Append(lower).Append(digits);
        }

        string pool = charPool.ToString();
        var result = new List<char>(guaranteedChars);

        int remaining = options.Length - guaranteedChars.Count;
        for (int i = 0; i < remaining; i++)
        {
            result.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);
        }

        // Fisher-Yates cryptographically secure shuffle
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return new string(result.ToArray());
    }

    public static PasswordStrengthResult EvaluateStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordStrengthResult
            {
                Level = PasswordStrengthLevel.VeryWeak,
                Label = "Çok Zayıf",
                EntropyBits = 0,
                HexColor = "#EF4444",
                ScorePercent = 0
            };
        }

        int poolSize = 0;
        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

        if (hasLower) poolSize += 26;
        if (hasUpper) poolSize += 26;
        if (hasDigit) poolSize += 10;
        if (hasSymbol) poolSize += 32;

        if (poolSize == 0) poolSize = 1;

        double entropy = password.Length * Math.Log2(poolSize);

        // Score based on entropy and length
        if (password.Length < 8 || entropy < 30)
        {
            return new PasswordStrengthResult
            {
                Level = PasswordStrengthLevel.VeryWeak,
                Label = "Çok Zayıf",
                EntropyBits = Math.Round(entropy, 1),
                HexColor = "#EF4444",
                ScorePercent = 20
            };
        }
        if (password.Length < 10 || entropy < 45)
        {
            return new PasswordStrengthResult
            {
                Level = PasswordStrengthLevel.Weak,
                Label = "Zayıf",
                EntropyBits = Math.Round(entropy, 1),
                HexColor = "#F97316",
                ScorePercent = 40
            };
        }
        if (password.Length < 14 || entropy < 60)
        {
            return new PasswordStrengthResult
            {
                Level = PasswordStrengthLevel.Fair,
                Label = "Orta",
                EntropyBits = Math.Round(entropy, 1),
                HexColor = "#EAB308",
                ScorePercent = 65
            };
        }
        if (password.Length < 18 || entropy < 80)
        {
            return new PasswordStrengthResult
            {
                Level = PasswordStrengthLevel.Strong,
                Label = "Güçlü",
                EntropyBits = Math.Round(entropy, 1),
                HexColor = "#10B981",
                ScorePercent = 85
            };
        }

        return new PasswordStrengthResult
        {
            Level = PasswordStrengthLevel.VeryStrong,
            Label = "Çok Güçlü",
            EntropyBits = Math.Round(entropy, 1),
            HexColor = "#06B6D4",
            ScorePercent = 100
        };
    }
}
