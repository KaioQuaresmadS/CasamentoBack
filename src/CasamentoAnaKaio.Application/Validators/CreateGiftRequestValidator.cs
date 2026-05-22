using CasamentoAnaKaio.Contracts.Gifts;
using FluentValidation;

namespace CasamentoAnaKaio.Application.Validators;

public sealed class CreateGiftRequestValidator : AbstractValidator<CreateGiftRequest>
{
    public CreateGiftRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(140).WithMessage("Nome não pode ter mais de 140 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .MaximumLength(600).WithMessage("Descrição não pode ter mais de 600 caracteres.");

        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("URL da imagem é obrigatória.")
            .MaximumLength(1000).WithMessage("URL da imagem não pode ter mais de 1000 caracteres.")
            .Must(BeValidUrl).WithMessage("URL da imagem deve ser válida.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Preço deve ser maior que zero.");

        RuleFor(x => x.ReservedPercent)
            .InclusiveBetween(0, 100).WithMessage("Percentual reservado deve ficar entre 0 e 100.");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
