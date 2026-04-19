using Microsoft.Maui.Storage;

namespace NewsApp.Mobile.Services;

/// <summary>
/// Interface para armazenamento seguro de segredos no dispositivo móvel.
/// Utiliza criptografia de hardware do SO (Android Keystore).
/// </summary>
public interface ISecureStorageService
{
    Task SaveSecretAsync(string keyName, string value);
    Task<string?> GetSecretAsync(string keyName);
    void RemoveSecret(string keyName);
}

public class SecureStorageService : ISecureStorageService
{
    public async Task SaveSecretAsync(string keyName, string value)
    {
        await SecureStorage.Default.SetAsync(keyName, value);
    }

    public async Task<string?> GetSecretAsync(string keyName)
    {
        return await SecureStorage.Default.GetAsync(keyName);
    }

    public void RemoveSecret(string keyName)
    {
        SecureStorage.Default.Remove(keyName);
    }
}
