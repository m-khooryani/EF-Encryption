using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SampleLib;

public class AesEncryptedConverter<TModel> : ValueConverter<TModel, string>
{
    public AesEncryptedConverter(byte[] key)
        : base(
            modelValue => Encrypt(Serialize(modelValue), key),
            providerValue => Deserialize(Decrypt(providerValue, key)))
    {
    }

    private static string Serialize(TModel value)
        => JsonSerializer.Serialize(value);

    private static TModel? Deserialize(string raw)
        => JsonSerializer.Deserialize<TModel>(raw);

    private static string Encrypt(string plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        // Write IV first
        ms.Write(aes.IV, 0, aes.IV.Length);

        // Then write ciphertext
        using var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var sw = new StreamWriter(cryptoStream, Encoding.UTF8);
        sw.Write(plainText);

        var encryptedBytes = ms.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string Decrypt(string cipherBase64, byte[] key)
    {
        if (string.IsNullOrEmpty(cipherBase64))
        {
            return string.Empty;
        }

        var cipherBytes = Convert.FromBase64String(cipherBase64);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
        using var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cryptoStream, Encoding.UTF8);

        return sr.ReadToEnd();
    }
}