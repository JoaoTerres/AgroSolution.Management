namespace AgroSolution.Identity.Domain.Entities;

public class Producer
{
    public Guid   Id           { get; private set; }
    public string Name         { get; private set; } = string.Empty;
    public string Email        { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt  { get; private set; }

    // EF Core
    protected Producer() { }

    public Producer(string name, string email, string passwordHash)
    {
        Id           = Guid.NewGuid();
        Name         = name;
        Email        = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAt    = DateTime.UtcNow;

        Validate();
    }

    private void Validate()
    {
        AssertValidation.ValidateIfEmpty(Name,  "O nome do produtor é obrigatório.");
        AssertValidation.ValidateIfEmpty(Email, "O e-mail é obrigatório.");
        AssertValidation.ValidateEmailFormat(Email, "O e-mail informado não é válido.");
        AssertValidation.ValidateIfEmpty(PasswordHash, "A senha é obrigatória.");
    }
}
