public sealed class GoogleOAuthResult
{
    public GoogleOAuthProfile Profile { get; private set; }
    public string IdToken { get; private set; }
    public string Error { get; private set; }
    public bool IsSuccess => string.IsNullOrEmpty(Error);

    public static GoogleOAuthResult Success(GoogleOAuthProfile profile, string idToken)
    {
        return new GoogleOAuthResult
        {
            Profile = profile,
            IdToken = idToken
        };
    }

    public static GoogleOAuthResult Failure(string error)
    {
        return new GoogleOAuthResult
        {
            Error = error
        };
    }
}
