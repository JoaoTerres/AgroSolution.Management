namespace AgroSolution.Core.Domain.Entities;

public class Plot
{
    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Name { get; private set; }
    public string CropType { get; private set; }
    public decimal AreaInHectares { get; private set; }

    protected Plot() { }

    public Plot(Guid propertyId, string name, string cropType, decimal area)
    {
        Id = Guid.NewGuid();
        PropertyId = propertyId;
        Name = name;
        CropType = cropType;
        AreaInHectares = area;

        Validate(); 
    }

    public void Validate()
    {
        AssertValidation.ValidateIfGuidEmpty(PropertyId, "O talhão deve pertencer a uma propriedade.");
        AssertValidation.ValidateIfEmpty(Name, "O nome do talhão é obrigatório.");
        AssertValidation.ValidateIfEmpty(CropType, "A cultura (soja, milho, etc) deve ser informada.");
        AssertValidation.ValidateIfLowerEqualThan(AreaInHectares, 0, "A área deve ser maior que zero.");
    }
}