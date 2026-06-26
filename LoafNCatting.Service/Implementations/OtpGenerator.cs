using System.Security.Cryptography;
using LoafNCatting.Service.Interfaces;

namespace LoafNCatting.Service.Implementations;

public class OtpGenerator : IOtpGenerator
{
    public string GenerateNumericCode(int length = 6)
    {
        if (length is < 4 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "OTP length must be between 4 and 10 digits.");
        }

        Span<char> digits = stackalloc char[length];
        for (var index = 0; index < digits.Length; index++)
        {
            digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(digits);
    }
}
