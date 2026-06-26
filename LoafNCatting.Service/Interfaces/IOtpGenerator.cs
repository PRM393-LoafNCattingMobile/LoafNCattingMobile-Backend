namespace LoafNCatting.Service.Interfaces;

public interface IOtpGenerator
{
    string GenerateNumericCode(int length = 6);
}
