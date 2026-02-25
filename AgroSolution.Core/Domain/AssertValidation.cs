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

    /// <summary>
    /// Valida se um valor não é nulo
    /// </summary>
    public static void NotNull(object? obj, string paramName)
    {
        if (obj == null)
            throw new DomainException($"O parâmetro '{paramName}' não pode ser nulo.");
    }

    /// <summary>
    /// Valida se uma string não é nula ou vazia
    /// </summary>
    public static void NotNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"O parâmetro '{paramName}' não pode ser nulo ou vazio.");
    }

    /// <summary>
    /// Valida se um Guid não está vazio
    /// </summary>
    public static void NotEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new DomainException($"O parâmetro '{paramName}' não pode ser Guid vazio.");
    }

    /// <summary>
    /// Valida se um enum tem um valor válido
    /// </summary>
    public static void IsValidEnum<T>(T value, string paramName) where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value))
            throw new DomainException($"O valor '{value}' não é válido para o tipo {typeof(T).Name}.");
    }
}