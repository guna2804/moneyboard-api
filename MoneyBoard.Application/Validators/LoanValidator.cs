using FluentValidation;
using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Validators
{
    public class LoanValidator
    {
        public class CreateLoanValidator : AbstractValidator<CreateLoanDto>
        {
            public CreateLoanValidator()
            {
                RuleFor(x => x.CounterpartyName)
                    .NotEmpty().WithMessage("Counterparty name is required.")
                    .MaximumLength(100);

                RuleFor(x => x.Role)
                    .NotEmpty().WithMessage("Role is required.")
                    .Must(r => r == "Lender" || r == "Borrower").WithMessage("Role must be 'Lender' or 'Borrower'.");

                RuleFor(x => x.Principal)
                    .GreaterThan(0).WithMessage("Principal must be greater than zero.");

                RuleFor(x => x.InterestRate)
                    .GreaterThanOrEqualTo(0).WithMessage("Interest rate must be non-negative.");

                RuleFor(x => x.InterestType)
                    .IsInEnum().WithMessage("Invalid interest type.");



                RuleFor(x => x.StartDate)
                    .NotEmpty().WithMessage("Start date is required.");

                RuleFor(x => x.RepaymentFrequency)
                    .IsInEnum().WithMessage("Invalid repayment frequency.");

                RuleFor(x => x.Currency)
                    .IsInEnum().WithMessage("Invalid currency type.");
            }
        }

        public class UpdateLoanValidator : AbstractValidator<UpdateLoanDto>
        {
            public UpdateLoanValidator()
            {
                RuleFor(x => x.CounterpartyName)
                    .NotEmpty().WithMessage("Counterparty name is required.")
                    .MaximumLength(100);

                RuleFor(x => x.InterestRate)
                    .GreaterThanOrEqualTo(0).WithMessage("Interest rate must be non-negative.");

                RuleFor(x => x.RepaymentFrequency)
                    .IsInEnum().WithMessage("Invalid repayment frequency.");
            }
        }
    }
}
