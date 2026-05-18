namespace RealWorldApi.Core.Features.Users.Dto;

public record UserResponseDto(
    string Email,
    string Username,
    string Token,
    string? Bio,
    string? Image
);