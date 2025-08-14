namespace MoneyBoard.Application.Interfaces
{
    public interface IBCryptService
    {
        string HashPassword(string password);

        bool VerifyPassword(string password, string hashedPassword);
    }
}