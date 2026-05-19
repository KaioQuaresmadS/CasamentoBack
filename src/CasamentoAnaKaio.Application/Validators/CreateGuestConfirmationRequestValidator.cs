using CasamentoAnaKaio.Contracts.GuestConfirmations;
using FluentValidation;

namespace CasamentoAnaKaio.Application.Validators;

public sealed class CreateGuestConfirmationRequestValidator : AbstractValidator<CreateGuestConfirmationRequest>
{
    public CreateGuestConfirmationRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres.")
            .MaximumLength(160).WithMessage("Nome não pode ter mais de 160 caracteres.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefone é obrigatório.")
            .MinimumLength(10).WithMessage("Telefone deve ter no mínimo 10 caracteres.")
            .MaximumLength(30).WithMessage("Telefone não pode ter mais de 30 caracteres.");

        RuleFor(x => x.GuestsCount)
            .GreaterThan(0).WithMessage("Quantidade de convidados deve ser maior que 0.")
            .LessThanOrEqualTo(10).WithMessage("Quantidade de convidados não pode ultrapassar 10.");

        RuleFor(x => x.Notes)
            .MaximumLength(600).WithMessage("Observações não podem ter mais de 600 caracteres.");
    }
}
