using FluentValidation;
using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Validators
{
    public class RepaymentValidator
    {
        public class CreateRepaymentValidator : AbstractValidator<CreateRepaymentRequestDto>
        {
            public CreateRepaymentValidator()
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0).WithMessage("Amount must be greater than 0.")
                    .LessThanOrEqualTo(10000000).WithMessage("Amount cannot exceed 10,000,000."); // Reasonable upper limit

                RuleFor(x => x.RepaymentDate)
                    .NotEmpty().WithMessage("Repayment date is required.")
                    .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Repayment date cannot be in the future.")
                    .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-10)).WithMessage("Repayment date cannot be more than 10 years in the past.");

                RuleFor(x => x.Notes)
                    .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
                    .When(x => !string.IsNullOrWhiteSpace(x.Notes))
                    .MinimumLength(2).WithMessage("Notes must be at least 2 characters if provided.");
            }
        }

        public class UpdateRepaymentValidator : AbstractValidator<UpdateRepaymentRequestDto>
        {
            public UpdateRepaymentValidator()
            {
                RuleFor(x => x.Amount)
                    .GreaterThan(0).WithMessage("Amount must be greater than 0.")
                    .LessThanOrEqualTo(10000000).WithMessage("Amount cannot exceed 10,000,000."); // Reasonable upper limit

                RuleFor(x => x.RepaymentDate)
                    .NotEmpty().WithMessage("Repayment date is required.")
                    .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Repayment date cannot be in the future.")
                    .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-10)).WithMessage("Repayment date cannot be more than 10 years in the past.");

                RuleFor(x => x.Notes)
                    .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
                    .When(x => !string.IsNullOrWhiteSpace(x.Notes))
                    .MinimumLength(2).WithMessage("Notes must be at least 2 characters if provided.");
            }
        }
    }
}
