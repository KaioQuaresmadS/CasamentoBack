namespace CasamentoAnaKaio.Domain.Entities;

public class Gift
{
    private Gift()
    {
    }

    public Gift(string name, string description, string imageUrl, decimal price, int reservedPercent = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do presente e obrigatorio.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Preco deve ser maior que zero.");
        }

        if (reservedPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedPercent), "Percentual reservado deve ficar entre 0 e 100.");
        }

        Name = name.Trim();
        Description = description.Trim();
        ImageUrl = imageUrl.Trim();
        Price = price;
        ReservedPercent = reservedPercent;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ImageUrl { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int ReservedPercent { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public ICollection<GiftContribution> Contributions { get; private set; } = new List<GiftContribution>();

    public void Update(string name, string description, string imageUrl, decimal price, int reservedPercent)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do presente e obrigatorio.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Preco deve ser maior que zero.");
        }

        if (reservedPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedPercent), "Percentual reservado deve ficar entre 0 e 100.");
        }

        Name = name.Trim();
        Description = description.Trim();
        ImageUrl = imageUrl.Trim();
        Price = price;
        ReservedPercent = reservedPercent;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
