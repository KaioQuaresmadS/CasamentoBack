using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Contracts.Payments;
using CasamentoAnaKaio.Domain.Entities;

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
            unitOfWork);

        var response = await service.CreateAsync(
            new CreatePaymentRequest(gift.Id, "Maria Silva", "maria@example.com", "11999999999", "FullGift", 0, "pix"),
            CancellationToken.None);

        Assert.Equal("Pending", response.Status);
        Assert.Equal("pref-123", response.PreferenceId);
        Assert.Equal("https://mp.example/init", response.InitPoint);
        Assert.Equal("https://mp.example/sandbox", response.SandboxInitPoint);
        Assert.Equal("https://mp.example/sandbox", response.CheckoutUrl);
        Assert.Equal(response.CheckoutUrl, response.PaymentUrl);
        Assert.Single(contributionRepository.Contributions);
        Assert.Single(paymentRepository.Payments);
        Assert.Equal("pix", client.LastRequest?.PaymentMethod);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenMercadoPagoDoesNotReturnPaymentUrl()
    {
        var gift = new Gift("Jantar", "Jantar especial", "https://example.com/jantar.jpg", 280m);
        var service = new PaymentService(
            new FakeGiftRepository(gift),
            new FakeGiftContributionRepository(),
            new FakePaymentRepository(),
            new FakeMercadoPagoPaymentClient
            {
                PreferenceResult = new MercadoPagoPreferenceResult("pref-empty", string.Empty, string.Empty)
            },
            new FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreatePaymentRequest(gift.Id, "Maria Silva", "maria@example.com", "11999999999", "FullGift", 0, "pix"),
            CancellationToken.None));

        Assert.Contains("link de pagamento", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_AllowsCheckoutProWithoutSelectedPaymentMethod()
    {
        var gift = new Gift("Jantar", "Jantar especial", "https://example.com/jantar.jpg", 280m);
        var client = new FakeMercadoPagoPaymentClient();
        var service = new PaymentService(
            new FakeGiftRepository(gift),
            new FakeGiftContributionRepository(),
            new FakePaymentRepository(),
            client,
            new FakeUnitOfWork());

        var response = await service.CreateAsync(
            new CreatePaymentRequest(gift.Id, "Maria Silva", "maria@example.com", "11999999999", "FullGift", 0, null),
            CancellationToken.None);

        Assert.Equal("mercado_pago", response.PaymentMethod);
        Assert.Equal("mercado_pago", client.LastRequest?.PaymentMethod);
        Assert.Equal("https://mp.example/sandbox", response.SandboxInitPoint);
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
        public MercadoPagoPreferenceResult PreferenceResult { get; init; } =
            new("pref-123", "https://mp.example/init", "https://mp.example/sandbox");

        public Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
            MercadoPagoPreferenceRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(PreferenceResult);
        }

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MercadoPagoPaymentDetails(paymentId, "pending", string.Empty, null, null, null, null, null));
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
