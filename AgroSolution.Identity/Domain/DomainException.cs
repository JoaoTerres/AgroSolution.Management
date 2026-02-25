namespace AgroSolution.Identity.Domain;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
