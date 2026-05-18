using FluentValidation;

namespace RealWorldApi.Core.Features.Users.Dto;

public class LoginRequestDto
{
    public LoginDetails User { get; set; } = null!;
}

public record LoginDetails(
    string Email,
    string Password
);

public class LoginRequestValidator: AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.User)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("User is required.");
        RuleFor(x => x.User.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");
        RuleFor(x => x.User.Password)
            .NotEmpty().WithMessage("Password is required.");       
    }
}