# Contexto do Projeto - Para Agentes de IA

---
**Versão:** 1.0  
**Data:** 12/02/2026  
**Status:** Ativo
---

## 🎯 Visão Geral de Negócio

### O que é AgroSolution.Management?
Plataforma SaaS para **gestão de propriedades agrícolas** com foco em organização hierárquica:

```
Produtor (User)
  └── Propriedades (Property)
       └── Talhões/Parcelas (Plots)
            └── Dados/Operações
```

### Problema que Resolve
Pequenos e médios produtores rurais não têm ferramentas simples para organizar suas propriedades e parcelas de terra.

### Objetivos de Negócio
1. 📍 Centralizar informações de propriedades
2. 📊 Permitir rastreamento de parcelas (preparo para analytics)
3. 🔐 Controle de acesso por produtor
4. 📈 Base para monetização futura

---

## 👥 Entidades de Negócio

### Producer (Usuário)
```
- Id: Guid
- Name: string
- Email: string (único, autenticação)
- PhoneNumber: string?
- CreatedAt: DateTime
```

**Regras:**
- Um produtor pode ter múltiplas propriedades
- Acesso restrito aos seus próprios dados
- Email é identificador único

### Property (Propriedade)
```
- Id: Guid
- ProducerId: Guid (FK)
- Name: string
- City: string
- State: string
- TotalArea: decimal (hectares)
- CreatedAt: DateTime
```

**Regras:**
- Cada propriedade pertence a um produtor
- Nome deve ser único por produtor
- Área total > 0
- Localização é obrigatória

### Plot (Talhão/Parcela)
```
- Id: Guid
- PropertyId: Guid (FK)
- Name: string
- Area: decimal (hectares)
- Crop: string?
- CreatedAt: DateTime
```

**Regras:**
- Cada talhão pertence a uma propriedade
- Área < Área da propriedade pai
- Nome deve ser único por propriedade
- Preparação para rastreamento futuro

---

## 🔐 Segurança

### Autenticação
- **Tipo:** JWT Bearer
- **Issuer:** AgroSolution
- **Claims Inclusos:**
  - `sub`: Id do usuário
  - `email`: Email do usuário
  - `iat`: Data de emissão
  - `exp`: Expiração

### Autorização
- Todos endpoints requerem `[Authorize]`
- Validar que usuário é proprietário do recurso
- Implementar soft-delete para auditoria

### Dados Sensíveis
- Nunca retornar senhas (óbvio)
- Retornar apenas dados do usuário autenticado
- Logs não devem conter dados pessoais

---

## 🏗️ Estrutura Técnica

### Arquitetura
```
API (Apresentação)
├── Controllers
├── Middlewares
├── Config
└── Program.cs

CORE (Lógica de Negócio)
├── Domain (Entidades, Interfaces, Validações)
├── App (Casos de Uso, DTOs, Resultados)
└── Infra (Repositórios, EF Core, Migrações)
```

### Padrões Implementados
1. **DDD** (Domain-Driven Design)
2. **Repository Pattern** (Abstração de dados)
3. **Result Pattern** (Tratamento de erros)
4. **Dependency Injection** (IoC)
5. **SOLID Principles**

### Fluxo de Requisição
```
1. HTTP Request → Controller
2. Controller valida DTO
3. Controller chama Caso de Uso
4. Caso de Uso valida regras de negócio
5. Caso de Uso usa Repositório
6. Repositório acessa Banco de Dados
7. Resultado retorna pela cadeia
8. Controller mapeia para CustomResponse
9. HTTP Response
```

---

## 📦 Dependências Críticas

| Pacote | Versão | Uso |
|--------|--------|-----|
| EntityFrameworkCore | 9.0.x | ORM |
| EntityFrameworkCore.PostgreSQL | 9.0.x | Driver DB |
| AspNetCore.Authentication.JwtBearer | 9.0.x | Autenticação |
| Swashbuckle.AspNetCore | 6.x | Swagger/OpenAPI |

---

## 🗄️ Banco de Dados

### Banco
- **Tipo:** PostgreSQL 14+
- **Nome Recomendado:** `agrosolution_management`

### Tabelas Principais
```sql
-- Producers (Users)
-- Properties
-- Plots
-- AuditLogs (futuro)
```

### Migrations
- Usar Entity Framework Core Migrations
- Prefixo: `YYYY_MM_DD_HHMM_Description`
- Sempre reversível

---

## 📚 Convenções de Código

### Naming
- **Namespaces:** `AgroSolution.{Layer}.{Feature}`
- **Classes:** `PascalCase`
- **Métodos:** `PascalCase`
- **Variáveis privadas:** `_camelCase`
- **Variáveis locais:** `camelCase`
- **Constantes:** `UPPER_SNAKE_CASE`

### Exemplo
```csharp
namespace AgroSolution.Core.App.Features.CreateProperty;

public class CreateProperty : ICreateProperty
{
    private readonly IPropertyRepository _repository;
    
    public async Task<Result<PropertyResponseDto>> ExecuteAsync(CreatePropertyDto dto)
    {
        // Implementação
    }
}
```

---

## 🚀 Roadmap de Desenvolvimento

### Fase 1 (Atual)
- [x] Setup inicial
- [x] Entidades de domínio
- [ ] API básica completa
- [ ] Autenticação JWT

### Fase 2 (Próxima)
- [ ] Validações completas
- [ ] Testes unitários
- [ ] Testes de integração
- [ ] CI/CD básico

### Fase 3 (Futura)
- [ ] Frontend web
- [ ] Analytics dashboard
- [ ] Mobile app
- [ ] Integrações externas

---

## 📞 Contactos e Referências

| Tipo | Informação |
|------|-----------|
| Repo | GitHub: AgroBusinessSolution/AgroSolution.Management |
| Docs | /docs |
| Issues | GitHub Issues |
| Discussions | GitHub Discussions |

---

**Última atualização:** 12/02/2026

---

## ❓ Dúvidas Frequentes Técnicas

**P: Por que Result<T> e não exceções?**  
R: Separação de conceitos. Exceptions = erros técnicos. Result = tratamento de negócio.

**P: Como validar que usuário é dono do recurso?**  
R: Comparar `AppUserId` com `ProducerId` do recurso antes de modificar.

**P: Posso usar stored procedures?**  
R: Preferir LINQ to Entities. SP apenas para queries muito complexas.

**P: Como estruturar queries complexas?**  
R: Criar métodos específicos no repositório com nomes descritivos.
