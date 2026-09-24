using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using PassVault.Core.Crypto;
using PassVault.Core.Models;

namespace PassVault.Core.Services;

public class VaultStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string VaultFilePath { get; set; }

    public VaultStorageService(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            VaultFilePath = customPath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "PasStash");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            VaultFilePath = Path.Combine(folder, "passtash.dat");

            // Migrate from legacy PassVault if present and new vault doesn't exist yet
            string oldFolder = Path.Combine(appData, "PassVault");
            string oldFile = Path.Combine(oldFolder, "passvault.dat");
            if (!File.Exists(VaultFilePath) && File.Exists(oldFile))
            {
                try { File.Copy(oldFile, VaultFilePath, overwrite: true); } catch { }
            }
        }
    }

    public bool VaultExists() => File.Exists(VaultFilePath);

    public void SaveVault(VaultData data, string masterPassword)
    {
        data.LastModified = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(data, JsonOptions);
        byte[] encryptedBytes = EncryptionService.Encrypt(json, masterPassword);

        // Atomic write via temporary file
        string dir = Path.GetDirectoryName(VaultFilePath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = Path.Combine(dir, $"{Path.GetFileName(VaultFilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(tempPath, encryptedBytes);
            File.Move(tempPath, VaultFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }

    public VaultData LoadVault(string masterPassword)
    {
        if (!File.Exists(VaultFilePath))
        {
            throw new FileNotFoundException("Kasa dosyas\u0131 bulunamad\u0131.", VaultFilePath);
        }

        byte[] encryptedBytes = File.ReadAllBytes(VaultFilePath);
        string json = EncryptionService.Decrypt(encryptedBytes, masterPassword);
        var data = JsonSerializer.Deserialize<VaultData>(json, JsonOptions);

        return data ?? new VaultData();
    }

    public void ExportEncrypted(string targetFilePath, string masterPassword, VaultData data)
    {
        data.LastModified = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(data, JsonOptions);
        byte[] encryptedBytes = EncryptionService.Encrypt(json, masterPassword);
        File.WriteAllBytes(targetFilePath, encryptedBytes);
    }

    public void ExportJson(string targetFilePath, VaultData data)
    {
        string json = JsonSerializer.Serialize(data, IndentedJsonOptions);
        File.WriteAllText(targetFilePath, json, System.Text.Encoding.UTF8);
    }

    public VaultData ImportFromFile(string sourceFilePath, string masterPassword)
    {
        string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        if (ext == ".json")
        {
            string json = File.ReadAllText(sourceFilePath, System.Text.Encoding.UTF8);
            try
            {
                var vd = JsonSerializer.Deserialize<VaultData>(json, JsonOptions);
                if (vd != null && vd.Items.Count > 0) return vd;
            }
            catch { }

            var items = JsonSerializer.Deserialize<List<VaultItem>>(json, JsonOptions);
            return new VaultData { Items = items ?? new List<VaultItem>() };
        }
        else
        {
            byte[] encryptedBytes = File.ReadAllBytes(sourceFilePath);
            string json = EncryptionService.Decrypt(encryptedBytes, masterPassword);
            var data = JsonSerializer.Deserialize<VaultData>(json, JsonOptions);
            return data ?? new VaultData();
        }
    }
}