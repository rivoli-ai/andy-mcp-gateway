namespace McpGateway.Application.Auth;

/// <summary>
/// Symmetric envelope around the plaintext API key. Production binding goes through
/// ASP.NET DataProtection; tests can stub it without pulling DataProtection in.
/// </summary>
public interface IApiKeyCipher
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
