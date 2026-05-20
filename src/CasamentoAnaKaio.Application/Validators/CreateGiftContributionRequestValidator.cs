using CasamentoAnaKaio.Contracts.GiftContributions;
using FluentValidation;

namespace CasamentoAnaKaio.Application.Validators;

public sealed class CreateGiftContributionRequestValidator : AbstractValidator<CreateGiftContributionRequest>
{
    public CreateGiftContributionRequestValidator()
    {
        RuleFor(x => x.GiftId)
            .NotEmpty().WithMessage("ID do presente é obrigatório.");

        RuleFor(x => x.ContributorName)
            .NotEmpty().WithMessage("Nome do contribuinte é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(160).WithMessage("Nome não pode ter mais de 160 caracteres.");

        RuleFor(x => x.ContributorPhone)
            .NotEmpty().WithMessage("Telefone é obrigatório.")
            .MinimumLength(10).WithMessage("Telefone deve ter no mínimo 10 caracteres.")
            .MaximumLength(30).WithMessage("Telefone não pode ter mais de 30 caracteres.");

        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Modo de contribuição é obrigatório.")
            .Must(mode => mode.Equals("FullGift", StringComparison.OrdinalIgnoreCase) ||
                         mode.Equals("Quota", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Modo deve ser 'FullGift' ou 'Quota'.");

        RuleFor(x => x.QuotaQuantity)
            .GreaterThan(0).When(x => x.Mode.Equals("Quota", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Quantidade de cotas deve ser maior que 0.");
    }
}
