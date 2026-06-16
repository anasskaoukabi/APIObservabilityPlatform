using System;
using System.Collections.Generic;

namespace Dashboard.Web.Services;

public sealed class UserSession
{
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime AuthenticatedAt { get; set; }
}

public sealed class AuthService
{
    // Scoped pour chaque onglet/connexion utilisateur
    public UserSession? CurrentUser { get; private set; }

    // Registre utilisateur en mémoire statique pour persister entre les sessions/connexions
    private static readonly Dictionary<string, (string Email, string Password)> _users = new(StringComparer.OrdinalIgnoreCase)
    {
        { "admin", ("admin@observability.local", "admin123") }
    };
    private static readonly object _lock = new();

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => CurrentUser != null;

    public bool Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        lock (_lock)
        {
            if (_users.TryGetValue(username.Trim(), out var credentials) && credentials.Password == password)
            {
                CurrentUser = new UserSession
                {
                    Username = username.Trim(),
                    Email = credentials.Email,
                    AuthenticatedAt = DateTime.UtcNow
                };
                OnAuthStateChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool Register(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return false;

        lock (_lock)
        {
            var cleanUsername = username.Trim();
            if (_users.ContainsKey(cleanUsername))
                return false;

            _users[cleanUsername] = (email.Trim(), password);

            // Connecter l'utilisateur automatiquement après inscription
            CurrentUser = new UserSession
            {
                Username = cleanUsername,
                Email = email.Trim(),
                AuthenticatedAt = DateTime.UtcNow
            };
        }
        OnAuthStateChanged?.Invoke();
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        OnAuthStateChanged?.Invoke();
    }
}
