using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class LocalIdentityService
{
    private const string EmailProvider = "email";
    private const string GoogleProvider = "google-dev";
    private const string AppleProvider = "apple-dev";

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

        string salt = Guid.NewGuid().ToString("N");
        StoredUser user = new StoredUser
        {
            userId = Guid.NewGuid().ToString("N"),
            email = email,
            displayName = displayName,
            provider = EmailProvider,
            passwordSalt = salt,
            passwordHash = HashPassword(password, salt),
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

        if (user == null || user.passwordHash != HashPassword(password, user.passwordSalt))
        {
            error = "Email o contrasena incorrectos.";
            return false;
        }

        session = CreateSession(user, EmailProvider);
        SaveSession(session);
        error = null;
        return true;
    }

    public bool LoginWithDevelopmentProvider(string provider, out AuthSession session, out string error)
    {
        session = null;

        if (provider != GoogleProvider && provider != AppleProvider)
        {
            error = "Proveedor de identidad no soportado.";
            return false;
        }

        string email = provider == GoogleProvider ? "google.player@local.cozy" : "apple.player@local.cozy";
        StoredUser user = FindUserByEmail(email);

        if (user == null)
        {
            user = new StoredUser
            {
                userId = Guid.NewGuid().ToString("N"),
                email = email,
                displayName = provider == GoogleProvider ? "Google Player" : "Apple Player",
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

    private static string HashPassword(string password, string salt)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    private static string CreateDevelopmentToken(AuthSession session)
    {
        string header = Base64UrlEncode("{\"alg\":\"dev-local\",\"typ\":\"JWT\"}");
        string payload = Base64UrlEncode(JsonUtility.ToJson(session));
        string signature = Base64UrlEncode(HashPassword(payload, session.userId));
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
        public string passwordSalt;
        public string passwordHash;
        public string createdAtUtc;
        public string lastLoginAtUtc;
    }
}
