using MoneyBoard.Application.DTOs;

namespace MoneyBoard.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
}
