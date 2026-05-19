using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Domain.Entities;

public sealed class Role
{
    private Role()
    {
    }

    public Role(string name, RoleType roleType, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da role é obrigatório.", nameof(name));

        Name = name.Trim();
        RoleType = roleType;
        Description = description.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public RoleType RoleType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public ICollection<User> Users { get; private set; } = new List<User>();

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
