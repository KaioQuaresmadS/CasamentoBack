namespace CasamentoAnaKaio.Domain.Entities;

public sealed class User
{
    private User()
    {
    }

    public User(string email, string name, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(passwordHash));

        Email = email.Trim().ToLowerInvariant();
        Name = name.Trim();
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public ICollection<Role> Roles { get; private set; } = new List<Role>();

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Hash de senha é obrigatório.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddRole(Role role)
    {
        if (!Roles.Contains(role))
            Roles.Add(role);
    }

    public void RemoveRole(Role role)
    {
        Roles.Remove(role);
    }

    public bool HasRole(string roleName)
    {
        return Roles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }
}
