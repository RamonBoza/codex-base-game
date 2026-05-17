using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class GoogleOAuthService
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
    private const string Scope = "openid email profile";

    public IEnumerator SignIn(string clientId, int timeoutSeconds, Action<GoogleOAuthResult> onComplete)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure("Configura un Google OAuth Client ID de tipo Desktop app."));
            yield break;
        }

        TcpListener listener = null;
        Task<LoopbackResponse> loopbackTask = null;
        string redirectUri = null;
        string codeVerifier = CreateCodeVerifier();
        string state = CreateSecureToken(32);

        try
        {
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            redirectUri = $"http://127.0.0.1:{port}";
            loopbackTask = Task.Run(() => WaitForLoopbackResponse(listener, state));
        }
        catch (Exception exception)
        {
            SafeStop(listener);
            onComplete?.Invoke(GoogleOAuthResult.Failure($"No se pudo abrir el listener local de OAuth: {exception.Message}"));
            yield break;
        }

        string authorizationUrl = BuildAuthorizationUrl(clientId.Trim(), redirectUri, state, CreateCodeChallenge(codeVerifier));
        Application.OpenURL(authorizationUrl);

        float deadline = Time.realtimeSinceStartup + Mathf.Max(30, timeoutSeconds);

        while (!loopbackTask.IsCompleted && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (!loopbackTask.IsCompleted)
        {
            SafeStop(listener);
            onComplete?.Invoke(GoogleOAuthResult.Failure("Tiempo agotado esperando la respuesta de Google."));
            yield break;
        }

        if (loopbackTask.IsFaulted)
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure(loopbackTask.Exception?.GetBaseException().Message ?? "Error recibiendo la respuesta de Google."));
            yield break;
        }

        LoopbackResponse loopbackResponse = loopbackTask.Result;

        if (!string.IsNullOrEmpty(loopbackResponse.Error))
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure($"Google devolvio error: {loopbackResponse.Error}"));
            yield break;
        }

        if (string.IsNullOrEmpty(loopbackResponse.Code))
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure("Google no devolvio un authorization code."));
            yield break;
        }

        TokenResponse tokenResponse = null;
        string tokenError = null;
        yield return ExchangeCodeForTokens(clientId.Trim(), redirectUri, codeVerifier, loopbackResponse.Code, result =>
        {
            tokenResponse = result.Token;
            tokenError = result.Error;
        });

        if (!string.IsNullOrEmpty(tokenError))
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure(tokenError));
            yield break;
        }

        GoogleOAuthProfile profile = null;
        string profileError = null;
        yield return FetchUserProfile(tokenResponse.access_token, result =>
        {
            profile = result.Profile;
            profileError = result.Error;
        });

        if (!string.IsNullOrEmpty(profileError))
        {
            onComplete?.Invoke(GoogleOAuthResult.Failure(profileError));
            yield break;
        }

        onComplete?.Invoke(GoogleOAuthResult.Success(profile, tokenResponse.id_token));
    }

    private static IEnumerator ExchangeCodeForTokens(string clientId, string redirectUri, string codeVerifier, string code, Action<TokenExchangeResult> onComplete)
    {
        string body = FormEncode(new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "code", code },
            { "code_verifier", codeVerifier },
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri }
        });

        using (UnityWebRequest request = new UnityWebRequest(TokenEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(TokenExchangeResult.Failure($"Token exchange fallo: {request.error} {request.downloadHandler.text}"));
                yield break;
            }

            TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                onComplete?.Invoke(TokenExchangeResult.Failure("Google no devolvio access_token."));
                yield break;
            }

            onComplete?.Invoke(TokenExchangeResult.Success(tokenResponse));
        }
    }

    private static IEnumerator FetchUserProfile(string accessToken, Action<ProfileFetchResult> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(UserInfoEndpoint))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(ProfileFetchResult.Failure($"No se pudo leer el perfil de Google: {request.error} {request.downloadHandler.text}"));
                yield break;
            }

            GoogleOAuthProfile profile = JsonUtility.FromJson<GoogleOAuthProfile>(request.downloadHandler.text);

            if (profile == null || string.IsNullOrEmpty(profile.sub))
            {
                onComplete?.Invoke(ProfileFetchResult.Failure("El perfil de Google no contiene subject."));
                yield break;
            }

            onComplete?.Invoke(ProfileFetchResult.Success(profile));
        }
    }

    private static LoopbackResponse WaitForLoopbackResponse(TcpListener listener, string expectedState)
    {
        try
        {
            using (TcpClient client = listener.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                string requestLine = reader.ReadLine();

                while (!string.IsNullOrEmpty(reader.ReadLine()))
                {
                }

                LoopbackResponse response = ParseLoopbackRequest(requestLine, expectedState);
                string body = response.IsSuccess
                    ? "<html><body><h1>Login completado</h1><p>Puedes volver al juego.</p></body></html>"
                    : $"<html><body><h1>Login no completado</h1><p>{WebUtility.HtmlEncode(response.Error)}</p></body></html>";

                writer.Write("HTTP/1.1 200 OK\r\n");
                writer.Write("Content-Type: text/html; charset=utf-8\r\n");
                writer.Write($"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\n");
                writer.Write("Connection: close\r\n\r\n");
                writer.Write(body);
                writer.Flush();

                return response;
            }
        }
        finally
        {
            SafeStop(listener);
        }
    }

    private static LoopbackResponse ParseLoopbackRequest(string requestLine, string expectedState)
    {
        if (string.IsNullOrEmpty(requestLine))
        {
            return LoopbackResponse.Failure("Respuesta local vacia.");
        }

        string[] parts = requestLine.Split(' ');

        if (parts.Length < 2)
        {
            return LoopbackResponse.Failure("Respuesta local no valida.");
        }

        Uri callbackUri = new Uri("http://127.0.0.1" + parts[1]);
        Dictionary<string, string> query = ParseQuery(callbackUri.Query);

        if (!query.TryGetValue("state", out string state) || state != expectedState)
        {
            return LoopbackResponse.Failure("State OAuth no coincide.");
        }

        if (query.TryGetValue("error", out string error))
        {
            return LoopbackResponse.Failure(error);
        }

        return query.TryGetValue("code", out string code)
            ? LoopbackResponse.Success(code)
            : LoopbackResponse.Failure("Authorization code ausente.");
    }

    private static string BuildAuthorizationUrl(string clientId, string redirectUri, string state, string codeChallenge)
    {
        return AuthorizationEndpoint + "?" + FormEncode(new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "redirect_uri", redirectUri },
            { "response_type", "code" },
            { "scope", Scope },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" },
            { "state", state },
            { "prompt", "select_account" }
        });
    }

    private static string CreateCodeVerifier()
    {
        return CreateSecureToken(64);
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }
    }

    private static string CreateSecureToken(int byteCount)
    {
        byte[] bytes = new byte[byteCount];

        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(bytes);
        }

        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string FormEncode(Dictionary<string, string> values)
    {
        StringBuilder builder = new StringBuilder();

        foreach (KeyValuePair<string, string> value in values)
        {
            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(value.Key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value.Value));
        }

        return builder.ToString();
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        string trimmedQuery = query.StartsWith("?") ? query.Substring(1) : query;

        foreach (string pair in trimmedQuery.Split('&'))
        {
            if (string.IsNullOrEmpty(pair))
            {
                continue;
            }

            string[] parts = pair.Split(new[] { '=' }, 2);
            string key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static void SafeStop(TcpListener listener)
    {
        try
        {
            listener?.Stop();
        }
        catch
        {
        }
    }

    [Serializable]
    private sealed class TokenResponse
    {
        public string access_token = string.Empty;
        public int expires_in = 0;
        public string id_token = string.Empty;
        public string refresh_token = string.Empty;
        public string scope = string.Empty;
        public string token_type = string.Empty;
    }

    private sealed class LoopbackResponse
    {
        public string Code { get; private set; }
        public string Error { get; private set; }
        public bool IsSuccess => string.IsNullOrEmpty(Error);

        public static LoopbackResponse Success(string code)
        {
            return new LoopbackResponse { Code = code };
        }

        public static LoopbackResponse Failure(string error)
        {
            return new LoopbackResponse { Error = error };
        }
    }

    private sealed class TokenExchangeResult
    {
        public TokenResponse Token { get; private set; }
        public string Error { get; private set; }

        public static TokenExchangeResult Success(TokenResponse token)
        {
            return new TokenExchangeResult { Token = token };
        }

        public static TokenExchangeResult Failure(string error)
        {
            return new TokenExchangeResult { Error = error };
        }
    }

    private sealed class ProfileFetchResult
    {
        public GoogleOAuthProfile Profile { get; private set; }
        public string Error { get; private set; }

        public static ProfileFetchResult Success(GoogleOAuthProfile profile)
        {
            return new ProfileFetchResult { Profile = profile };
        }

        public static ProfileFetchResult Failure(string error)
        {
            return new ProfileFetchResult { Error = error };
        }
    }
}
