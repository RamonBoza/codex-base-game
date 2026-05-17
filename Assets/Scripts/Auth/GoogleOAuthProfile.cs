using System;

[Serializable]
public sealed class GoogleOAuthProfile
{
    public string sub;
    public string email;
    public bool email_verified;
    public string name;
    public string picture;
}
