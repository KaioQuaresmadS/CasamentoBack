using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Gifts;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using FluentValidation;
using FluentValidation.Results;

namespace CasamentoAnaKaio.Tests;

public sealed class GiftServiceTests
{
    [Fact]
    public async Task ListActiveAsync_ReturnsPaidGiftAsPurchased()
    {
        var gift = new Gift("Faqueiro Tramontina", "Faqueiro", "https://example.com/faqueiro.jpg", 78.75m);
        var contribution = new GiftContribution(
            gift,
            "Ruan",
            "11999999999",
            GiftContributionMode.FullGift,
            0,
            string.Empty,
            string.Empty,
            "mp-123");
        contribution.MarkAsPaid();
        gift.Contributions.Add(contribution);

        var service = new GiftService(
            new FakeGiftRepository(gift),
            new FakeUnitOfWork(),
            new NoopValidator<CreateGiftRequest>(),
            new NoopValidator<UpdateGiftRequest>());

        var gifts = await service.ListActiveAsync(CancellationToken.None);
        var response = Assert.Single(gifts);

        Assert.Equal(100, response.ReservedPercent);
        Assert.Equal(78.75m, response.ConfirmedAmount);
        Assert.Equal(78.75m, response.PaidAmount);
        Assert.True(response.IsPurchased);
        Assert.Equal("confirmed", response.PaymentStatus);
    }

    [Fact]
    public async Task ListActiveAsync_ReturnsMarkedGiftAsPurchased()
    {
        var gift = new Gift("Liquidificador", "Liquidificador Mondial", "https://example.com/liquidificador.jpg", 97.90m);
        gift.MarkAsPurchased();

        var service = new GiftService(
            new FakeGiftRepository(gift),
            new FakeUnitOfWork(),
            new NoopValidator<CreateGiftRequest>(),
            new NoopValidator<UpdateGiftRequest>());

        var gifts = await service.ListActiveAsync(CancellationToken.None);
        var response = Assert.Single(gifts);

        Assert.True(response.IsPurchased);
        Assert.Equal("confirmed", response.PaymentStatus);
    }

    private sealed class FakeGiftRepository(params Gift[] gifts) : IGiftRepository
    {
        public Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(gifts.FirstOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Gift>> ListActiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Gift>>(gifts);
        }

        public Task AddAsync(Gift gift, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Gift gift, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopValidator<T> : IValidator<T>
    {
        public ValidationResult Validate(T instance) => new();

        public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = default)
        {
            return Task.FromResult(new ValidationResult());
        }

        public ValidationResult Validate(IValidationContext context) => new();

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default)
        {
            return Task.FromResult(new ValidationResult());
        }

        public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

        public bool CanValidateInstancesOfType(Type type) => typeof(T).IsAssignableFrom(type);
    }
}
