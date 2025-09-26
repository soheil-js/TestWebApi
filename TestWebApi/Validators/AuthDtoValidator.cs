using FluentValidation;
using TestWebApi.Dtos;

namespace TestWebApi.Validators
{
    public class AuthDtoValidator : AbstractValidator<UserDto>
    {
        public AuthDtoValidator()
        {
            RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be 3–50 characters");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        }
    }
}
