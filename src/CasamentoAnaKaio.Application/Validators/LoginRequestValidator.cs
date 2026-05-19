using CasamentoAnaKaio.Contracts.Authentication;
using FluentValidation;

namespace CasamentoAnaKaio.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email deve ser válido.")
            .MaximumLength(256).WithMessage("Email não pode ter mais de 256 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(128).WithMessage("Senha não pode ter mais de 128 caracteres.");
    }
}
