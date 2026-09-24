using System.Text.Json.Serialization;

namespace PassVault.Core.Models;

public class VaultItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public VaultCategory Category { get; set; } = VaultCategory.Login;
    public string Folder { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public VaultItem Clone()
    {
        return new VaultItem
        {
            Id = this.Id,
            Title = this.Title,
            Username = this.Username,
            Password = this.Password,
            Url = this.Url,
            Notes = this.Notes,
            Category = this.Category,
            Folder = this.Folder,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt
        };
    }
}
