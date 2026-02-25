using System.ComponentModel.DataAnnotations;

namespace AgroSolution.Core.App.DTO;

public record CreatePropertyDto(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 200 caracteres")]
    string Name,

    [Required(ErrorMessage = "Localização é obrigatória")]
    [StringLength(500, MinimumLength = 2, ErrorMessage = "Localização deve ter entre 2 e 500 caracteres")]
    string Location);