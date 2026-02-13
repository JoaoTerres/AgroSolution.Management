namespace AgroSolution.Core.Domain;

public static class AssertValidation
{
    public static void ValidateIfEmpty(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(message);
    }
    
    public static void ValidateMinimumLength(string value, int minimum, string message)
    {
        if (value.Length < minimum)
            throw new DomainException(message);
    }
    
    public static void ValidateEmailFormat(string email, string message)
    {
        if (!email.Contains("@") || !email.Contains("."))
            throw new DomainException(message);
    }
    
    public static void ValidateIfNull(object obj, string message)
    {
        if (obj == null) throw new DomainException(message);
    }

    public static void ValidateIfGuidEmpty(Guid guid, string message)
    {
        if (guid == Guid.Empty) throw new DomainException(message);
    }

    public static void ValidateIfLowerEqualThan(decimal value, decimal min, string message)
    {
        if (value <= min) throw new DomainException(message);
    }
}