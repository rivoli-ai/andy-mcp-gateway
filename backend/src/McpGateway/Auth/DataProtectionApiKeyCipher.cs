using McpGateway.Application.Auth;
using Microsoft.AspNetCore.DataProtection;

namespace McpGateway.Auth;

/// <summary>
/// <see cref="IApiKeyCipher"/> backed by ASP.NET Core DataProtection. Uses a stable purpose
/// string so the same cipher can decrypt rows across process restarts (assuming the
/// DataProtection key ring is persisted, which the framework defaults to on Linux/IIS).
/// </summary>
public sealed class DataProtectionApiKeyCipher : IApiKeyCipher
{
    private const string Purpose = "McpGateway.ApiKey.v1";
    private readonly IDataProtector _protector;

    public DataProtectionApiKeyCipher(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector(Purpose);

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
