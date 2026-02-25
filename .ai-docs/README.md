# 🤖 Documentação para Agentes de IA

Esta pasta contém instruções especializadas para **agentes de IA e assistentes de código** que trabalham neste projeto.

---

## 📂 Organização

### 01-Prompt-Guards
**Objetivo:** Validações e regras que agentes devem respeitar

Contém:
- Constraints de modificação de código
- Validações obrigatórias
- Padrões proibidos
- Limites de mudança

### 02-Context
**Objetivo:** Contexto de negócio e técnico do projeto

Contém:
- Visão geral de domínio
- Objetivos de negócio
- Restrições técnicas
- Dependências críticas

### 03-Padroes-Codigo
**Objetivo:** Padrões que devem ser seguidos

Contém:
- Convenções de naming
- Estrutura de classes
- Padrões de erro
- Organização de arquivos

### 04-Fluxos
**Objetivo:** Fluxos de negócio e processos

Contém:
- Diagramas de fluxo
- Sequências de operação
- Estados e transições
- Regras de negócio

---

## 🔍 Como Usar

**Ao receber uma requisição, agentes devem:**

1. Ler `02-Context` para entender o domínio
2. Consultar `03-Padroes-Codigo` para implementar corretamente
3. Validar contra `01-Prompt-Guards` antes de executar
4. Considerar `04-Fluxos` para manter coerência

---

## ⚠️ Crítico

Estes arquivos são **instruções obrigatórias** para qualquer trabalho no codebase. Não devem ser ignorados.

---

**Última atualização:** 12/02/2026
