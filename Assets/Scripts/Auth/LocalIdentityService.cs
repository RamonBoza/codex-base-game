using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class LocalIdentityService
{
    private const string EmailProvider = "email";
    private const string GoogleProvider = "google";
    private const string AppleProvider = "apple-dev";
    private const string PasswordAlgorithm = "pbkdf2-sha256";
    private const int PasswordIterations = 210000;
    private const int PasswordSaltBytes = 16;
    private const int PasswordHashBytes = 32;
#if UNITY_EDITOR
    private const string EditorTestEmail = "test@cozy.local";
    private const string EditorTestPassword = "cozy-test-player";
    private const string EditorTestDisplayName = "Cozy Tester";
#endif

    private readonly string databasePath;
    private readonly string sessionPath;
    private StoredIdentityDatabase database;

    public LocalIdentityService()
    {
        string identityDirectory = Path.Combine(Application.persistentDataPath, "Identity");
        Directory.CreateDirectory(identityDirectory);

        databasePath = Path.Combine(identityDirectory, "local_identity.json");
        sessionPath = Path.Combine(identityDirectory, "session.json");
        database = LoadDatabase();
    }

    public bool TryLoadSession(out AuthSession session)
    {
        session = null;

        if (!File.Exists(sessionPath))
        {
            return false;
        }

        try
        {
            session = JsonUtility.FromJson<AuthSession>(File.ReadAllText(sessionPath));
            return session != null && session.IsValid && FindUserById(session.userId) != null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load local auth session: {exception.Message}");
            return false;
        }
    }

    public bool RegisterWithEmail(string email, string password, string displayName, out AuthSession session, out string error)
    {
        session = null;
        email = NormalizeEmail(email);
        displayName = NormalizeDisplayName(displayName, email);

        if (!IsValidEmail(email))
        {
            error = "Introduce un email valido.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            error = "La contrasena debe tener al menos 6 caracteres.";
            return false;
        }

        if (FindUserByEmail(email) != null)
        {
            error = "Ya existe una cuenta con ese email.";
            return false;
        }

        string salt = CreatePasswordSalt();
        StoredUser user = new StoredUser
        {
            userId = Guid.NewGuid().ToString("N"),
            email = email,
            displayName = displayName,
            provider = EmailProvider,
            passwordAlgorithm = PasswordAlgorithm,
            passwordIterations = PasswordIterations,
            passwordSalt = salt,
            passwordHash = HashPasswordPbkdf2(password, salt, PasswordIterations),
            createdAtUtc = DateTime.UtcNow.ToString("O")
        };

        database.users.Add(user);
        SaveDatabase();

        session = CreateSession(user, EmailProvider);
        SaveSession(session);
        error = null;
        return true;
    }

    public bool LoginWithEmail(string email, string password, out AuthSession session, out string error)
    {
        session = null;
        email = NormalizeEmail(email);

        StoredUser user = FindUserByEmail(email);

        if (user == null || !VerifyPassword(user, password))
        {
            error = "Email o contrasena incorrectos.";
            return false;
        }

        UpgradePasswordHashIfNeeded(user, password);
        session = CreateSession(user, EmailProvider);
        SaveSession(session);
        error = null;
        return true;
    }

#if UNITY_EDITOR
    public bool LoginWithEditorTestUser(out AuthSession session, out string error)
    {
        session = null;
        StoredUser user = FindUserByEmail(EditorTestEmail);

        if (user == null)
        {
            string salt = CreatePasswordSalt();
            user = new StoredUser
            {
                userId = Guid.NewGuid().ToString("N"),
                email = EditorTestEmail,
                displayName = EditorTestDisplayName,
                provider = EmailProvider,
                passwordAlgorithm = PasswordAlgorithm,
                passwordIterations = PasswordIterations,
                passwordSalt = salt,
                passwordHash = HashPasswordPbkdf2(EditorTestPassword, salt, PasswordIterations),
                createdAtUtc = DateTime.UtcNow.ToString("O")
            };

            database.users.Add(user);
            SaveDatabase();
        }
        else
        {
            user.displayName = EditorTestDisplayName;
            user.provider = EmailProvider;
            EnsurePasswordLogin(user, EditorTestPassword);
        }

        session = CreateSession(user, EmailProvider);
        SaveSession(session);
        error = null;
        return true;
    }
#endif

    public bool LoginWithGoogle(GoogleOAuthProfile profile, string idToken, out AuthSession session, out string error)
    {
        session = null;

        if (profile == null || string.IsNullOrWhiteSpace(profile.sub))
        {
            error = "Google no devolvio un perfil valido.";
            return false;
        }

        string email = NormalizeEmail(profile.email);

        if (!IsValidEmail(email) || !profile.email_verified)
        {
            error = "Google no devolvio un email verificado.";
            return false;
        }

        StoredUser user = FindUserByProviderSubject(GoogleProvider, profile.sub);

        user ??= FindUserByEmail(email);

        if (user == null)
        {
            user = new StoredUser
            {
                userId = Guid.NewGuid().ToString("N"),
                email = email,
                displayName = NormalizeDisplayName(profile.name, email),
                provider = GoogleProvider,
                externalSubject = profile.sub,
                avatarUrl = profile.picture,
                createdAtUtc = DateTime.UtcNow.ToString("O")
            };

            database.users.Add(user);
        }
        else
        {
            user.email = email;
            user.displayName = NormalizeDisplayName(profile.name, email);
            user.provider = GoogleProvider;
            user.externalSubject = profile.sub;
            user.avatarUrl = profile.picture;
        }

        session = CreateSession(user, GoogleProvider);
        session.sessionToken = null;
        session.sessionToken = CreateGoogleBackedDevelopmentToken(session, idToken);
        SaveDatabase();
        SaveSession(session);
        error = null;
        return true;
    }

    public bool LoginWithDevelopmentProvider(string provider, out AuthSession session, out string error)
    {
        session = null;

        if (provider != AppleProvider)
        {
            error = "Proveedor de identidad no soportado.";
            return false;
        }

        string email = "apple.player@local.cozy";
        StoredUser user = FindUserByEmail(email);

        if (user == null)
        {
            user = new StoredUser
            {
                userId = Guid.NewGuid().ToString("N"),
                email = email,
                displayName = "Apple Player",
                provider = provider,
                createdAtUtc = DateTime.UtcNow.ToString("O")
            };

            database.users.Add(user);
            SaveDatabase();
        }

        session = CreateSession(user, provider);
        SaveSession(session);
        error = null;
        return true;
    }

    public void ClearSession()
    {
        if (File.Exists(sessionPath))
        {
            File.Delete(sessionPath);
        }
    }

    private StoredIdentityDatabase LoadDatabase()
    {
        if (!File.Exists(databasePath))
        {
            return new StoredIdentityDatabase();
        }

        try
        {
            StoredIdentityDatabase loadedDatabase = JsonUtility.FromJson<StoredIdentityDatabase>(File.ReadAllText(databasePath));
            return EnsureDatabaseShape(loadedDatabase);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load local identity database: {exception.Message}");
            return new StoredIdentityDatabase();
        }
    }

    private void SaveDatabase()
    {
        File.WriteAllText(databasePath, JsonUtility.ToJson(database, true));
    }

    private void SaveSession(AuthSession session)
    {
        File.WriteAllText(sessionPath, JsonUtility.ToJson(session, true));
    }

    private AuthSession CreateSession(StoredUser user, string provider)
    {
        user.lastLoginAtUtc = DateTime.UtcNow.ToString("O");
        SaveDatabase();

        AuthSession session = new AuthSession
        {
            userId = user.userId,
            email = user.email,
            displayName = user.displayName,
            provider = provider,
            externalSubject = user.externalSubject,
            avatarUrl = user.avatarUrl,
            issuedAtUtc = user.lastLoginAtUtc
        };

        session.sessionToken = CreateDevelopmentToken(session);
        return session;
    }

    private StoredUser FindUserByEmail(string email)
    {
        return database.users.Find(user => user.email == email);
    }

    private StoredUser FindUserById(string userId)
    {
        return database.users.Find(user => user.userId == userId);
    }

    private StoredUser FindUserByProviderSubject(string provider, string externalSubject)
    {
        return database.users.Find(user => user.provider == provider && user.externalSubject == externalSubject);
    }

    private static string NormalizeEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string displayName, string email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        int atIndex = email.IndexOf('@');
        return atIndex > 0 ? email.Substring(0, atIndex) : "Player";
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".");
    }

    private static string CreatePasswordSalt()
    {
        byte[] saltBytes = new byte[PasswordSaltBytes];

        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            generator.GetBytes(saltBytes);
        }

        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPasswordPbkdf2(string password, string salt, int iterations)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, iterations, HashAlgorithmName.SHA256))
        {
            return Convert.ToBase64String(pbkdf2.GetBytes(PasswordHashBytes));
        }
    }

    private static string HashPasswordSha256Legacy(string password, string salt)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    private static bool VerifyPassword(StoredUser user, string password)
    {
        string expectedHash;

        if (user.passwordAlgorithm == PasswordAlgorithm)
        {
            int iterations = user.passwordIterations > 0 ? user.passwordIterations : PasswordIterations;
            expectedHash = HashPasswordPbkdf2(password, user.passwordSalt, iterations);
        }
        else
        {
            expectedHash = HashPasswordSha256Legacy(password, user.passwordSalt);
        }

        return FixedTimeEquals(expectedHash, user.passwordHash);
    }

    private void UpgradePasswordHashIfNeeded(StoredUser user, string password)
    {
        if (user.passwordAlgorithm == PasswordAlgorithm && user.passwordIterations >= PasswordIterations)
        {
            return;
        }

        user.passwordAlgorithm = PasswordAlgorithm;
        user.passwordIterations = PasswordIterations;
        user.passwordSalt = CreatePasswordSalt();
        user.passwordHash = HashPasswordPbkdf2(password, user.passwordSalt, user.passwordIterations);
        SaveDatabase();
    }

    private void EnsurePasswordLogin(StoredUser user, string password)
    {
        if (user.passwordAlgorithm == PasswordAlgorithm && user.passwordIterations >= PasswordIterations && !string.IsNullOrEmpty(user.passwordSalt) && !string.IsNullOrEmpty(user.passwordHash))
        {
            return;
        }

        user.passwordAlgorithm = PasswordAlgorithm;
        user.passwordIterations = PasswordIterations;
        user.passwordSalt = CreatePasswordSalt();
        user.passwordHash = HashPasswordPbkdf2(password, user.passwordSalt, user.passwordIterations);
        SaveDatabase();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);
        int difference = leftBytes.Length ^ rightBytes.Length;
        int length = Mathf.Min(leftBytes.Length, rightBytes.Length);

        for (int i = 0; i < length; i++)
        {
            difference |= leftBytes[i] ^ rightBytes[i];
        }

        return difference == 0;
    }

    private static string CreateDevelopmentToken(AuthSession session)
    {
        string header = Base64UrlEncode("{\"alg\":\"dev-local\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode(JsonUtility.ToJson(session));
        string signature = Base64UrlEncode(HashPasswordSha256Legacy(payload, session.userId));
        return $"{header}.{payload}.{signature}";
    }

    private static string CreateGoogleBackedDevelopmentToken(AuthSession session, string idToken)
    {
        string tokenSeed = string.IsNullOrEmpty(idToken) ? session.userId : idToken;
        string header = Base64UrlEncode("{\"alg\":\"google-dev-local\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode(JsonUtility.ToJson(session));
        string signature = Base64UrlEncode(HashPasswordSha256Legacy(payload, tokenSeed));
        return $"{header}.{payload}.{signature}";
    }

    private static string Base64UrlEncode(string value)
    {
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static StoredIdentityDatabase EnsureDatabaseShape(StoredIdentityDatabase loadedDatabase)
    {
        if (loadedDatabase == null)
        {
            return new StoredIdentityDatabase();
        }

        if (loadedDatabase.users == null)
        {
            loadedDatabase.users = new List<StoredUser>();
        }

        return loadedDatabase;
    }

    [Serializable]
    private sealed class StoredIdentityDatabase
    {
        public List<StoredUser> users = new List<StoredUser>();
    }

    [Serializable]
    private sealed class StoredUser
    {
        public string userId;
        public string email;
        public string displayName;
        public string provider;
        public string externalSubject;
        public string avatarUrl;
        public string passwordAlgorithm;
        public int passwordIterations;
        public string passwordSalt;
        public string passwordHash;
        public string createdAtUtc;
        public string lastLoginAtUtc;
    }
}
