namespace PassVault.Core.Models;

public class VaultData
{
    public int Version { get; set; } = 1;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public List<VaultItem> Items { get; set; } = new();
    public List<string> Folders { get; set; } = new();
}
