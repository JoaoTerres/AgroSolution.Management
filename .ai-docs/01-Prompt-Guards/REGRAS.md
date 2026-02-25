# Prompt Guards - Regras Obrigatórias para Agentes de IA

---
**Versão:** 1.0  
**Data:** 12/02/2026  
**Status:** Ativo
---

## ⚖️ Regras de Ouro

Estas regras **DEVEM** ser seguidas em toda modificação de código:

### 1. Arquitetura em Camadas
- ✅ NÃO acople camadas (Api → Core, Core → Infra)
- ✅ Sempre use interfaces para abstração
- ✅ Repositórios e Services devem estar em Core, nunca em Api

### 2. Padrão Result<T>
- ✅ Retorne `Result<T>` de casos de uso
- ✅ Nunca lance exceções em lógica de negócio (exceto validação)
- ✅ Controllers retornem `CustomResponse(result)`

### 3. Validações
- ✅ Use `AssertValidation` para validar regras de negócio
- ✅ Entidades devem validar seus invariantes
- ✅ DTOs devem ter validação no controller

### 4. Nomenclatura
- ✅ Classes de caso de uso: `{NomeAcao}` (ex: `CreateProperty`)
- ✅ Interfaces: `I{NomeAcao}` (ex: `ICreateProperty`)
- ✅ DTOs: `{Nome}Dto` (ex: `CreatePropertyDto`)
- ✅ Repositórios: `{Nome}Repository` (ex: `PropertyRepository`)

### 5. Async/Await
- ✅ Sempre use métodos async em I/O (banco, API)
- ✅ Use `Task<T>` em interfaces públicas
- ✅ Não use `.Result` ou `.Wait()`

### 6. Banco de Dados
- ✅ Mapeamentos no `ManagementDbContext`
- ✅ Migrations com prefixo sequencial
- ✅ Chaves estrangeiras configuradas fluentemente

---

## 🚫 Proibições

| O que | Por quê | Alternativa |
|------|---------|-------------|
| Exceções em lógica de negócio | Acoplamento | Use Result<T> |
| Acesso direto a DbContext fora de repos | Violação de camadas | Injete repositório |
| DTOs sem validação | Dados inconsistentes | Adicione DataAnnotations |
| Magic strings | Manutenção difícil | Use constantes/config |
| Métodos muito longos (>50 linhas) | Legibilidade | Extraia em sub-métodos |

---

## ✅ Validações Antes de Commit

- [ ] Código compila sem erros
- [ ] Não há warnings (se possível)
- [ ] Segue padrões de naming
- [ ] Respeita separação de camadas
- [ ] Usa `Result<T>` para retorno
- [ ] DTOs têm validação
- [ ] Documentação atualizada

---

## 📋 Checklist para Novas Features

Quando implementar uma nova feature, siga:

```
1. Criar entidade de domínio em Domain/Entities
2. Criar interface de repositório em Domain/Interfaces
3. Criar DTO em App/DTO
4. Criar caso de uso em App/Features
5. Implementar repositório em Infra/Repositories
6. Criar controller em Api/Controllers
7. Adicionar DI em DependencyInjectionConfig
8. Documentar em docs/
```

---

## 🔐 Segurança

- [ ] Sempre usar `[Authorize]` onde apropriado
- [ ] Validar `AppUserId` contra recursos
- [ ] Nunca retornar dados sensíveis
- [ ] Sanitizar inputs de usuário

---

## 📝 Exemplos Corretos

### ✅ Caso de Uso Correto
```csharp
public class CreateProperty : ICreateProperty
{
    private readonly IPropertyRepository _repository;
    
    public CreateProperty(IPropertyRepository repository)
        => _repository = repository;
    
    public async Task<Result<PropertyResponseDto>> ExecuteAsync(CreatePropertyDto dto)
    {
        AssertValidation.NotNull(dto);
        
        var property = new Property(dto.Name, dto.ProducerId);
        
        var result = await _repository.AddAsync(property);
        if (!result) return Result<PropertyResponseDto>.Failure("Erro ao adicionar");
        
        return Result<PropertyResponseDto>.Success(new PropertyResponseDto(property));
    }
}
```

### ❌ Evite
```csharp
// Não lance exceções
public class CreateProperty : ICreateProperty
{
    public async Task<PropertyResponseDto> ExecuteAsync(CreatePropertyDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto)); // ❌
        var property = new Property(dto.Name, dto.ProducerId);
        return await _repository.AddAsync(property);
    }
}
```

---

**Última atualização:** 12/02/2026
