using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SampleLib;

public class AesEncryptedConverter<TModel> : ValueConverter<TModel, string>
{
    public AesEncryptedConverter(byte[] key, byte[] iv)
        : base(
            modelValue => Encrypt(Serialize(modelValue), key, iv),
            providerValue => Deserialize(Decrypt(providerValue, key, iv)))
    {
    }

    private static string Serialize(TModel value)
        => JsonSerializer.Serialize(value);

    private static TModel? Deserialize(string raw)
        => JsonSerializer.Deserialize<TModel>(raw);

    public static string Decrypt(string cipherText, byte[] key, byte[] iv)
    {
        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        using Aes aesAlg = Aes.Create();

        aesAlg.Key = key;
        aesAlg.IV = iv;

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new MemoryStream(cipherBytes);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    public static string Encrypt(string plainText, byte[] key, byte[] iv)
    {
        using Aes aesAlg = Aes.Create();

        aesAlg.Key = key;
        aesAlg.IV = iv;

        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msEncrypt = new MemoryStream();
        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        byte[] encrypted = msEncrypt.ToArray();
        return Convert.ToBase64String(encrypted);
    }
}