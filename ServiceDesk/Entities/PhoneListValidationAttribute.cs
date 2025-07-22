using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

public class PhoneListValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        var list = value as List<string>;
        if (list == null) return true;

        var phoneRegex = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");

        foreach (var phone in list)
        {
            if (!string.IsNullOrWhiteSpace(phone) && !phoneRegex.IsMatch(phone))
            {
                return false;
            }
        }

        return true;
    }
}