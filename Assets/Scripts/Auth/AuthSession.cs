using System;

[Serializable]
public sealed class AuthSession
{
    public string userId;
    public string email;
    public string displayName;
    public string provider;
    public string sessionToken;
    public string issuedAtUtc;

    public bool IsValid => !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionToken);
}
