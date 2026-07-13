namespace LoafNCatting.Service.Validation;

internal static class PhoneNumberValidator
{
    public static bool IsValid(string value) =>
        value.Length is >= 10 and <= 11 && value.All(char.IsDigit);
}
