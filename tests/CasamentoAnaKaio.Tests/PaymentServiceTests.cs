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

    [Fact]
    public async Task CreatePixAsync_CreatesDirectPixPayment()
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

        var response = await service.CreatePixAsync(
            new CreatePixPaymentRequest(gift.Id, "Maria Silva", "maria@example.com", 61m),
            CancellationToken.None);

        Assert.Equal("mp-pix-123", response.PaymentId);
        Assert.Equal("pending", response.Status);
        Assert.Equal("qr-code", response.QrCode);
        Assert.Equal("qr-code-base64", response.QrCodeBase64);
        Assert.Equal("https://mp.example/ticket", response.TicketUrl);
        Assert.StartsWith("pix-", response.ExternalReference);
        Assert.Single(contributionRepository.Contributions);
        Assert.Single(paymentRepository.Payments);
        Assert.Equal(61m, paymentRepository.Payments[0].Amount);
        Assert.Equal("Pix", paymentRepository.Payments[0].PaymentMethod);
        Assert.Equal("mp-pix-123", paymentRepository.Payments[0].MercadoPagoPaymentId);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateBoletoAsync_CreatesDirectBoletoPayment()
    {
        var gift = new Gift("Faqueiro Tramontina", "Faqueiro", "https://example.com/faqueiro.jpg", 78.75m);
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

        var response = await service.CreateBoletoAsync(
            new CreatePaymentRequest(gift.Id, "Ruan", "ruan@gmail.com", "11999999999", "FullGift", 0, "boleto"),
            CancellationToken.None);

        Assert.Equal("mp-boleto-123", response.PaymentId);
        Assert.Equal("pending", response.Status);
        Assert.Equal("https://mp.example/boleto", response.TicketUrl);
        Assert.Equal("https://mp.example/boleto", response.BoletoUrl);
        Assert.Equal("1234567890", response.Barcode);
        Assert.Equal("34191.79001 01043.510047 91020.150008 8 98760000007875", response.LinhaDigitavel);
        Assert.Equal("boleto", paymentRepository.Payments[0].PaymentMethod);
        Assert.Equal("mp-boleto-123", paymentRepository.Payments[0].MercadoPagoPaymentId);
        Assert.Equal("1234567890", paymentRepository.Payments[0].Barcode);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateAsync_WithBoletoMethod_CreatesDirectBoletoPayment()
    {
        var gift = new Gift("Faqueiro Tramontina", "Faqueiro", "https://example.com/faqueiro.jpg", 78.75m);
        var service = new PaymentService(
            new FakeGiftRepository(gift),
            new FakeGiftContributionRepository(),
            new FakePaymentRepository(),
            new FakeMercadoPagoPaymentClient(),
            new FakeUnitOfWork());

        var response = await service.CreateAsync(
            new CreatePaymentRequest(gift.Id, "Ruan", "ruan@gmail.com", "11999999999", "FullGift", 0, "boleto"),
            CancellationToken.None);

        Assert.Equal("boleto", response.PaymentMethod);
        Assert.Equal("mp-boleto-123", response.MercadoPagoPaymentId);
        Assert.Equal("https://mp.example/boleto", response.TicketUrl);
        Assert.Equal("https://mp.example/boleto", response.BoletoUrl);
        Assert.Equal("1234567890", response.Barcode);
        Assert.Equal("34191.79001 01043.510047 91020.150008 8 98760000007875", response.LinhaDigitavel);
    }

    [Fact]
    public async Task GetMercadoPagoStatusAsync_UpdatesLocalPaymentFromMercadoPagoPaymentId()
    {
        var gift = new Gift("Jantar", "Jantar especial", "https://example.com/jantar.jpg", 280m);
        var contributionRepository = new FakeGiftContributionRepository();
        var paymentRepository = new FakePaymentRepository();
        var client = new FakeMercadoPagoPaymentClient
        {
            StatusPaymentDetails = new MercadoPagoPaymentDetails(
                "mp-pix-123",
                "approved",
                null,
                "pix",
                "bank_transfer",
                "qr-code",
                "qr-code-base64",
                "https://mp.example/ticket")
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentService(
            new FakeGiftRepository(gift),
            contributionRepository,
            paymentRepository,
            client,
            unitOfWork);

        await service.CreatePixAsync(
            new CreatePixPaymentRequest(gift.Id, "Maria Silva", "maria@example.com", 61m),
            CancellationToken.None);

        var response = await service.GetMercadoPagoStatusAsync("mp-pix-123", CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Paid", response.Status);
        Assert.Equal(2, unitOfWork.SaveChangesCount);
    }

    private sealed class FakeGiftRepository(Gift gift) : IGiftRepository
    {
        public Task<Gift?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(gift.Id == id ? gift : null);
        public Task<IReadOnlyList<Gift>> ListActiveAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Gift>>(new[] { gift });
        public Task AddAsync(Gift gift, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(Gift gift, CancellationToken cancellationToken) => Task.CompletedTask;
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
        public MercadoPagoPaymentDetails? StatusPaymentDetails { get; init; }

        public Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
            MercadoPagoPreferenceRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(PreferenceResult);
        }

        public Task<MercadoPagoPaymentDetails> CreatePixPaymentAsync(
            MercadoPagoPixPaymentRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new MercadoPagoPaymentDetails(
                "mp-pix-123",
                "pending",
                request.ExternalReference,
                "pix",
                "bank_transfer",
                "qr-code",
                "qr-code-base64",
                "https://mp.example/ticket"));
        }

        public Task<MercadoPagoPaymentDetails> CreateBoletoPaymentAsync(
            MercadoPagoBoletoPaymentRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new MercadoPagoPaymentDetails(
                "mp-boleto-123",
                "pending",
                request.ExternalReference,
                "bolbradesco",
                "ticket",
                null,
                null,
                "https://mp.example/boleto",
                "1234567890",
                "34191.79001 01043.510047 91020.150008 8 98760000007875"));
        }

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StatusPaymentDetails ?? new MercadoPagoPaymentDetails(paymentId, "pending", string.Empty, null, null, null, null, null));
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
