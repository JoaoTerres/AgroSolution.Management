using System.ComponentModel.DataAnnotations;

namespace AgroSolution.Core.App.DTO;

public record CreatePlotDto(
    [Required(ErrorMessage = "PropertyId é obrigatório")]
    Guid PropertyId,

    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 200 caracteres")]
    string Name,

    [Required(ErrorMessage = "Tipo de cultura é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Tipo de cultura deve ter entre 2 e 100 caracteres")]
    string CropType,

    [Range(0.01, double.MaxValue, ErrorMessage = "Área deve ser maior que zero")]
    decimal Area);