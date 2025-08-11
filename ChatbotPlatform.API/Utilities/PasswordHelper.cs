using BCrypt.Net;

namespace ChatbotPlatform.API.Utilities
{
    public class PasswordHelper
    {
        internal static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        internal static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}