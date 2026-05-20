using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace CasamentoAnaKaio.Tests;

public sealed class PaymentServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesMercadoPagoPreferenceAndKeepsPaymentPending()
    {
        var gift = new Gift("Jantar", "Jantar especial", "https://example.com/jantar.jpg", 280m);
        var contributionRepository = new FakeGiftContributionRepository();
        var paymentRepository = new FakePaymentRepository();
        var client = new FakeMercadoPagoPaymentClient();
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentService(
            new FakeGiftRepository(gift),
            contributionRepository,
            paymentRepository,
            client,
            unitOfWork,
            NullLogger<PaymentService>.Instance);

        var response = await service.CreateAsync(
            new CreatePaymentRequest(gift.Id, "Maria Silva", "maria@example.com", "11999999999", "pix", "FullGift", 0),
            "https://front.example",
            "https://back.example",
            CancellationToken.None);

        Assert.Equal("Pending", response.PaymentStatus);
        Assert.Equal("pref-123", response.PreferenceId);
        Assert.Single(contributionRepository.Contributions);
        Assert.Single(paymentRepository.Payments);
        Assert.Equal("pix", client.LastRequest?.PaymentMethod);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    private sealed class FakeGiftRepository(Gift gift) : IGiftRepository
    {
        public Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(gift.Id == id ? gift : null);
        public Task<IReadOnlyList<Gift>> ListActiveAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Gift>>(new[] { gift });
    }

    private sealed class FakeGiftContributionRepository : IGiftContributionRepository
    {
        public List<GiftContribution> Contributions { get; } = [];

        public Task AddAsync(GiftContribution contribution, CancellationToken cancellationToken)
        {
            Contributions.Add(contribution);
            return Task.CompletedTask;
        }

        public Task<GiftContribution?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Contributions.FirstOrDefault(x => x.Id == id));
        }

        public Task<GiftContribution?> GetByProviderPaymentIdAsync(string providerPaymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Contributions.FirstOrDefault(x => x.ProviderPaymentId == providerPaymentId));
        }
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Payments { get; } = [];

        public Task AddAsync(Payment payment, CancellationToken cancellationToken)
        {
            Payments.Add(payment);
            return Task.CompletedTask;
        }

        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Payments.FirstOrDefault(x => x.Id == id));
        public Task<Payment?> GetByMercadoPagoPaymentIdAsync(string mercadoPagoPaymentId, CancellationToken cancellationToken) => Task.FromResult(Payments.FirstOrDefault(x => x.MercadoPagoPaymentId == mercadoPagoPaymentId));
        public Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken) => Task.FromResult(Payments.FirstOrDefault(x => x.ExternalReference == externalReference));
    }

    private sealed class FakeMercadoPagoPaymentClient : IMercadoPagoPaymentClient
    {
        public MercadoPagoPreferenceRequest? LastRequest { get; private set; }

        public Task<MercadoPagoPreferenceResult> CreatePreferenceAsync(
            MercadoPagoPreferenceRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new MercadoPagoPreferenceResult("pref-123", "https://mp.example/init", "https://mp.example/sandbox"));
        }

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MercadoPagoPaymentDetails(paymentId, "pending", string.Empty, "pix", 280m, null, null));
        }
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
