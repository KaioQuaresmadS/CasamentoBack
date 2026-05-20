using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace CasamentoAnaKaio.Tests;

public sealed class PaymentWebhookServiceTests
{
    [Fact]
    public async Task ProcessWebhookAsync_WithApprovedPayment_MarksContributionAsPaidOnlyAfterApiLookup()
    {
        var contribution = CreateContribution("pref-123");
        var payment = CreatePayment(contribution);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            contribution,
            payment,
            unitOfWork,
            new MercadoPagoPaymentDetails("mp-123", "approved", payment.ExternalReference, "pix", payment.Amount, null, null));

        var processed = await service.ProcessWebhookAsync(
            """{"data":{"id":"mp-123"},"status":"rejected"}""",
            ValidHeaders(),
            new Dictionary<string, string>(),
            CancellationToken.None);

        Assert.True(processed);
        Assert.Equal(PaymentStatus.Paid, contribution.PaymentStatus);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.NotNull(contribution.PaidAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithInvalidSignature_DoesNotUpdateContribution()
    {
        var contribution = CreateContribution("pref-123");
        var payment = CreatePayment(contribution);
        var unitOfWork = new FakeUnitOfWork();
        var service = new PaymentWebhookService(
            new FakeGiftContributionRepository(contribution),
            new FakePaymentRepository(payment),
            new FakeMercadoPagoPaymentClient(new MercadoPagoPaymentDetails("mp-123", "approved", payment.ExternalReference, "pix", payment.Amount, null, null)),
            new FakePaymentWebhookValidator(false),
            unitOfWork,
            NullLogger<PaymentWebhookService>.Instance);

        var processed = await service.ProcessWebhookAsync(
            """{"data":{"id":"mp-123"},"status":"approved"}""",
            ValidHeaders(),
            new Dictionary<string, string>(),
            CancellationToken.None);

        Assert.False(processed);
        Assert.Equal(PaymentStatus.Pending, contribution.PaymentStatus);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ProcessWebhookAsync_DuplicateApprovedWebhook_IsIdempotent()
    {
        var contribution = CreateContribution("pref-123");
        var payment = CreatePayment(contribution);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            contribution,
            payment,
            unitOfWork,
            new MercadoPagoPaymentDetails("mp-123", "approved", payment.ExternalReference, "pix", payment.Amount, null, null));

        await service.ProcessWebhookAsync("""{"data":{"id":"mp-123"}}""", ValidHeaders(), new Dictionary<string, string>(), CancellationToken.None);
        var firstPaidAt = contribution.PaidAt;
        await service.ProcessWebhookAsync("""{"data":{"id":"mp-123"}}""", ValidHeaders(), new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, contribution.PaymentStatus);
        Assert.Equal(firstPaidAt, contribution.PaidAt);
        Assert.Equal(2, unitOfWork.SaveChangesCount);
    }

    [Theory]
    [InlineData("approved", PaymentStatus.Paid)]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("in_process", PaymentStatus.Processing)]
    [InlineData("rejected", PaymentStatus.Failed)]
    [InlineData("cancelled", PaymentStatus.Cancelled)]
    [InlineData("refunded", PaymentStatus.Refunded)]
    [InlineData("charged_back", PaymentStatus.ChargedBack)]
    [InlineData("expired", PaymentStatus.Expired)]
    public void MapStatus_MapsMercadoPagoStatus(string mercadoPagoStatus, PaymentStatus expected)
    {
        Assert.Equal(expected, PaymentService.MapStatus(mercadoPagoStatus));
    }

    private static PaymentWebhookService CreateService(
        GiftContribution contribution,
        Payment payment,
        FakeUnitOfWork unitOfWork,
        MercadoPagoPaymentDetails paymentDetails)
    {
        return new PaymentWebhookService(
            new FakeGiftContributionRepository(contribution),
            new FakePaymentRepository(payment),
            new FakeMercadoPagoPaymentClient(paymentDetails),
            new FakePaymentWebhookValidator(true),
            unitOfWork,
            NullLogger<PaymentWebhookService>.Instance);
    }

    private static Dictionary<string, string> ValidHeaders()
    {
        return new Dictionary<string, string>
        {
            ["x-signature"] = "ts=1,v1=abc",
            ["x-request-id"] = "request-1"
        };
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
            "mercado-pago",
            string.Empty,
            providerPaymentId);
    }

    private static Payment CreatePayment(GiftContribution contribution)
    {
        var payment = new Payment(
            contribution.Id,
            contribution.Id.ToString("N"),
            "pix",
            contribution.Amount,
            contribution.ContributorName,
            "maria@example.com");
        payment.SetCheckoutPreference("pref-123", "https://mp.example/init", "https://mp.example/sandbox");
        return payment;
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

    private sealed class FakePaymentRepository(Payment? payment) : IPaymentRepository
    {
        public Task AddAsync(Payment payment, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(payment?.Id == id ? payment : null);
        public Task<Payment?> GetByMercadoPagoPaymentIdAsync(string mercadoPagoPaymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(payment?.MercadoPagoPaymentId == mercadoPagoPaymentId ? payment : null);
        }

        public Task<Payment?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken)
        {
            return Task.FromResult(payment?.ExternalReference == externalReference ? payment : null);
        }
    }

    private sealed class FakeMercadoPagoPaymentClient(MercadoPagoPaymentDetails paymentDetails) : IMercadoPagoPaymentClient
    {
        public Task<MercadoPagoPreferenceResult> CreateCheckoutPreferenceAsync(
            MercadoPagoPreferenceRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new MercadoPagoPreferenceResult("pref-123", "https://mp.example/init", "https://mp.example/sandbox"));
        }

        public Task<MercadoPagoPaymentDetails> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(paymentDetails);
        }
    }

    private sealed class FakePaymentWebhookValidator(bool isValid) : IPaymentWebhookValidator
    {
        public bool ValidateSignature(string dataId, string xRequestId, string xSignature) => isValid;
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
