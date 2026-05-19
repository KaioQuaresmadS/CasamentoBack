using CasamentoAnaKaio.Domain.Entities;
using CasamentoAnaKaio.Domain.Enums;

namespace CasamentoAnaKaio.Tests;

public class DomainRulesTests
{
    [Fact]
    public void FullGiftContribution_ShouldUseGiftFullPrice()
    {
        var amount = GiftContribution.CalculateAmount(420m, GiftContributionMode.FullGift, 0);

        Assert.Equal(420m, amount);
    }

    [Fact]
    public void QuotaContribution_ShouldUseFivePercentOfGiftPrice()
    {
        var amount = GiftContribution.CalculateAmount(900m, GiftContributionMode.Quota, 1);

        Assert.Equal(45m, amount);
    }

    [Fact]
    public void QuotaContribution_ShouldMultiplyQuotaQuantity()
    {
        var amount = GiftContribution.CalculateAmount(900m, GiftContributionMode.Quota, 4);

        Assert.Equal(180m, amount);
    }

    [Fact]
    public void GuestConfirmation_ShouldRejectNegativeGuestsCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GuestConfirmation("Ana Souza", "(11) 99999-9999", -1, true, null));
    }
}
