using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Tests;

public sealed class PaymentWebhookServiceTests
{
    [Fact]
    public async Task ProcessWebhookAsync_WithApprovedPayment_MarksContributionAsPaid()
    {
        var contribution = CreateContribution("mp-123");
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentWebhookService(
            new FakeGiftContributionRepository(contribution),
            new FakePaymentWebhookValidator(true),
            unitOfWork);

        var processed = await service.ProcessWebhookAsync(
            """{"data":{"id":"mp-123"},"status":"approved"}""",
            "valid-signature",
            CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(PaymentStatus.Paid, contribution.PaymentStatus);
        Assert.NotNull(contribution.PaidAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithInvalidSignature_DoesNotUpdateContribution()
    {
        var contribution = CreateContribution("mp-123");
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentWebhookService(
            new FakeGiftContributionRepository(contribution),
            new FakePaymentWebhookValidator(false),
            unitOfWork);

        var processed = await service.ProcessWebhookAsync(
            """{"data":{"id":"mp-123"},"status":"approved"}""",
            "invalid-signature",
            CancellationToken.None);

        Assert.False(processed);
        Assert.Equal(PaymentStatus.Pending, contribution.PaymentStatus);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static GiftContribution CreateContribution(string providerPaymentId)
    {
        var gift = new Gift("Cafeteira", "Cafe da manha", "https://example.com/cafe.jpg", 360m);
        return new GiftContribution(
            gift,
            "Maria Silva",
            "11999999999",
            GiftContributionMode.FullGift,
            0,
            "pix@example.com",
            "pix-payload",
            providerPaymentId);
    }

    private sealed class FakeGiftContributionRepository(GiftContribution? contribution) : IGiftContributionRepository
    {
        public Task AddAsync(GiftContribution contribution, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<GiftContribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(contribution?.Id == id ? contribution : null);
        public Task<GiftContribution?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(contribution?.ProviderPaymentId == providerPaymentId ? contribution : null);
        }
    }

    private sealed class FakePaymentWebhookValidator(bool isValid) : IPaymentWebhookValidator
    {
        public bool ValidateSignature(string payload, string signature) => isValid;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }
}
