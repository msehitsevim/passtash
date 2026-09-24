using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PassVault.Core.Crypto;

public class EncryptionService
{
    private static readonly byte[] MagicBytes = "PVLT"u8.ToArray();
    private const byte CurrentFormatVersion = 1;
    public const int DefaultIterations = 600_000;
    private const int SaltSize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32; // AES-256

    /// <summary>
    /// Encrypts plaintext data using AES-256-GCM with a PBKDF2-HMAC-SHA256 derived master key.
    /// </summary>
    public static byte[] Encrypt(string plainText, string masterPassword, int iterations = DefaultIterations)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentException.ThrowIfNullOrWhiteSpace(masterPassword);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] key = DeriveKey(masterPassword, salt, iterations);

        byte[] ciphertext = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        try
        {
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);

            // Assemble file format
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(MagicBytes);
            writer.Write(CurrentFormatVersion);
            writer.Write(iterations);
            writer.Write(salt);
            writer.Write(nonce);
            writer.Write(tag);
            writer.Write(ciphertext);

            return ms.ToArray();
        }
        finally
        {
            // Wipe sensitive key and plaintext memory immediately
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    /// <summary>
    /// Decrypts vault file bytes using AES-256-GCM.
    /// Throws CryptographicException if password is incorrect or data is corrupted/tampered with.
    /// </summary>
    public static string Decrypt(byte[] encryptedData, string masterPassword)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentException.ThrowIfNullOrWhiteSpace(masterPassword);

        int minHeaderSize = MagicBytes.Length + 1 + sizeof(int) + SaltSize + NonceSize + TagSize;
        if (encryptedData.Length < minHeaderSize)
        {
            throw new CryptographicException("Geçersiz kasa dosyası veya bozuk veri.");
        }

        using var ms = new MemoryStream(encryptedData);
        using var reader = new BinaryReader(ms);

        byte[] magic = reader.ReadBytes(MagicBytes.Length);
        if (!magic.SequenceEqual(MagicBytes))
        {
            throw new CryptographicException("Bu dosya geçerli bir PassVault kasası değil.");
        }

        byte version = reader.ReadByte();
        if (version != CurrentFormatVersion)
        {
            throw new CryptographicException($"Desteklenmeyen kasa dosya sürümü: {version}");
        }

        int iterations = reader.ReadInt32();
        if (iterations < 10_000 || iterations > 5_000_000)
        {
            throw new CryptographicException("Kasa dosyasındaki şifreleme parametreleri geçersiz.");
        }

        byte[] salt = reader.ReadBytes(SaltSize);
        byte[] nonce = reader.ReadBytes(NonceSize);
        byte[] tag = reader.ReadBytes(TagSize);
        byte[] ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position));

        byte[] key = DeriveKey(masterPassword, salt, iterations);
        byte[] plaintextBytes = new byte[ciphertext.Length];

        try
        {
            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Ana parola hatalı veya kasa dosyası bütünlüğü bozulmuş.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    /// <summary>
    /// Derives a 256-bit AES key from the master password and salt using PBKDF2-HMAC-SHA256.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );
    }
}
