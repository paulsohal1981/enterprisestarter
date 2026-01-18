namespace OrgManagement.Application.Common.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateRandomPassword(int length = 16);
}
