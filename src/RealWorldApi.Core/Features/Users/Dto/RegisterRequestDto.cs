using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RealWorldApi.Infrastructure.Data;

namespace RealWorldApi.Core.Features.Users.Dto;

public class RegisterRequestDto
{
    public RegisterDetails User { get; set; } = null!;
}

public record RegisterDetails(
    string Username, 
    string Password, 
    string Email
);

public class RegisterRequestValidator: AbstractValidator<RegisterRequestDto>
{
    private readonly AppDbContext _db;
    public RegisterRequestValidator(AppDbContext db, ILogger<Program> logger)
    {
        _db = db;
        RuleFor(x => x.User)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("User is required.");
        RuleFor(x => x.User.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Username is required.")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(20).WithMessage("Username must be at most 20 characters long.")
            .MustAsync(BeUniqueUsername).WithMessage("Username is already taken.");
        RuleFor(x => x.User.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(100).WithMessage("Password must be at most 100 characters long.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$").WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit.");
        RuleFor(x => x.User.Email).NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
    }

    private async Task<bool> BeUniqueUsername(string? username, CancellationToken token)
    {
        if (username is null) return false;
        return await _db.Users
            .SingleOrDefaultAsync(u => u.Username == username, token) is null;
    }
}