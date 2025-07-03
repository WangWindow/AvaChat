using System.Security.Cryptography;
using System.Text;

namespace AvaChat.Server.Models;

/// <summary>
/// 安全服务 - 提供密码加密、解密等安全功能
/// </summary>
public static class SecurityService
{
    private static readonly string EncryptionKey = "AvaChat_2025_Key_32Chars_Long!!!"; // 32字符密钥

    /// <summary>
    /// 加密密码（用于网络传输）
    /// </summary>
    public static string EncryptPassword(string password)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // 将IV和加密数据组合
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// 解密密码（从网络传输中解密）
    /// </summary>
    public static string DecryptPassword(string encryptedPassword)
    {
        var encryptedData = Convert.FromBase64String(encryptedPassword);

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);

        // 提取IV
        var iv = new byte[aes.IV.Length];
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // 提取加密数据
        var encryptedBytes = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    /// <summary>
    /// 生成密码哈希（已弃用 - 仅用于向后兼容）
    /// </summary>
    [Obsolete("现在使用加密传输，数据库存储明文密码")]
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "AvaChat_Salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// 验证密码（已弃用 - 仅用于向后兼容）
    /// </summary>
    [Obsolete("现在使用加密传输，数据库存储明文密码")]
    public static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    /// <summary>
    /// 生成随机用户ID
    /// </summary>
    public static string GenerateUserId()
    {
        var random = new Random();
        return random.Next(10000000, 99999999).ToString();
    }
}
