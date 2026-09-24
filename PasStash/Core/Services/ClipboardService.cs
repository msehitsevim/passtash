using System.Windows;
using System.Windows.Threading;

namespace PassVault.Core.Services;

public class ClipboardService
{
    private static DispatcherTimer? _clearTimer;
    private static string? _lastCopiedValue;

    public static event Action<string>? OnClipboardNotification;

    public static void CopyWithAutoClear(string text, string description = "Veri", int clearAfterSeconds = 30)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            Clipboard.SetText(text);
            _lastCopiedValue = text;

            _clearTimer?.Stop();
            _clearTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(clearAfterSeconds)
            };
            _clearTimer.Tick += (s, e) =>
            {
                _clearTimer.Stop();
                try
                {
                    // Only clear if the clipboard still contains what we copied
                    if (Clipboard.ContainsText() && Clipboard.GetText() == _lastCopiedValue)
                    {
                        Clipboard.Clear();
                        OnClipboardNotification?.Invoke("Güvenlik nedeniyle pano temizlendi.");
                    }
                }
                catch { }
            };
            _clearTimer.Start();

            OnClipboardNotification?.Invoke($"{description} panoya kopyalandı! ({clearAfterSeconds} sn sonra silinecek)");
        }
        catch (Exception ex)
        {
            OnClipboardNotification?.Invoke($"Panoya kopyalanamadı: {ex.Message}");
        }
    }
}
