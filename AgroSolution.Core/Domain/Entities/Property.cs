namespace AgroSolution.Core.Domain.Entities;

public class Property
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Location { get; private set; }
    public Guid ProducerId { get; private set; }
    
    private readonly List<Plot> _plots = new();
    public IReadOnlyCollection<Plot> Plots => _plots.AsReadOnly();

    protected Property() { }

    public Property(string name, string location, Guid producerId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Location = location;
        ProducerId = producerId;

        Validate(); 
    }

    public void Validate()
    {
        AssertValidation.ValidateIfEmpty(Name, "O nome da propriedade é obrigatório.");
        AssertValidation.ValidateIfEmpty(Location, "A localização é obrigatória.");
        AssertValidation.ValidateIfGuidEmpty(ProducerId, "A propriedade deve estar vinculada a um produtor.");
    }

    public void AddPlot(string name, string cropType, decimal area)
    {
        var plot = new Plot(this.Id, name, cropType, area);
        _plots.Add(plot);
    }
}