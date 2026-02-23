**Resumo**
- **Objetivo:** Mapear as regras de negócio relacionadas aos fluxos IoT, propriedades e parcelas (plots) para revisão com stakeholders.

**Mapeamento Regras de Negócio**
- **Receber dados IoT:** endpoint de ingestão, validação e persistência.
  - Código: [AgroSolution.Api/Controllers/IoTDataController.cs](AgroSolution.Api/Controllers/IoTDataController.cs)
  - DTO/Feature/Validação: [AgroSolution.Core/App/DTO/ReceiveIoTDataDto.cs](AgroSolution.Core/App/DTO/ReceiveIoTDataDto.cs), [AgroSolution.Core/App/Features/ReceiveIoTData/ReceiveIoTData.cs](AgroSolution.Core/App/Features/ReceiveIoTData/ReceiveIoTData.cs), [AgroSolution.Core/App/Validation/IoTDeviceValidator.cs](AgroSolution.Core/App/Validation/IoTDeviceValidator.cs)
  - Status: Concluído (implementação + testes unitários presentes)

- **Registrar/validar dispositivo IoT:** verificação de dispositivo e fonte de dados.
  - Código: [AgroSolution.Core/Interfaces/IDeviceRepository.cs](AgroSolution.Core/Interfaces/IDeviceRepository.cs), [AgroSolution.Core/Infra/Repositories/InMemoryDeviceRepository.cs](AgroSolution.Core/Infra/Repositories/InMemoryDeviceRepository.cs)
  - Status: Implementado em memória (provisório) — falta repositório persistente

- **Criar propriedade:** criação de propriedade com validações de domínio.
  - Código: [AgroSolution.Api/Controllers/PropertyController.cs](AgroSolution.Api/Controllers/PropertyController.cs), [AgroSolution.Core/App/Features/CreateProperty](AgroSolution.Core/App/Features/CreateProperty)
  - DTO: [AgroSolution.Core/App/DTO/CreatePropertyDto.cs](AgroSolution.Core/App/DTO/CreatePropertyDto.cs)
  - Status: Concluído

- **Adicionar parcela (plot):** assinalar parcela à propriedade.
  - Código: [AgroSolution.Api/Controllers/PlotController.cs](AgroSolution.Api/Controllers/PlotController.cs), [AgroSolution.Core/App/Features/AddPlot](AgroSolution.Core/App/Features/AddPlot)
  - DTO: [AgroSolution.Core/App/DTO/CreatePlotDto.cs](AgroSolution.Core/App/DTO/CreatePlotDto.cs)
  - Status: Concluído

- **Consultar propriedades:** listagem e leitura de dados de propriedade.
  - Código: [AgroSolution.Core/App/Features/GetProperties](AgroSolution.Core/App/Features/GetProperties), [AgroSolution.Api/Controllers/PropertyController.cs](AgroSolution.Api/Controllers/PropertyController.cs)
  - Status: Concluído

**Percentual Concluído**
- **Estimativa:** 80% — justificativa: endpoints, DTOs, validações e testes unitários básicos estão implementados; faltam repositório persistente para dispositivos, testes de integração/E2E e documentação final detalhada.

**Próximos Passos (planejamento curto prazo)**
- **1 — Revisão com stakeholders:** validar este mapeamento e identificar gaps funcionais.
- **2 — Repositório persistente:** implementar `DeviceRepository` com persistência (DB) e migrar o uso do InMemory.
- **3 — Testes de integração/E2E:** criar cenários de ingestão IoT e fluxo completo (device -> persistência -> leitura).
- **4 — Documentação:** completar Swagger/API docs em [docs/04-API/IOT-API.md](docs/04-API/IOT-API.md) e guias rápidos.
- **5 — Release:** planear deployment incremental após validação e testes.

**Artefatos & Referências**
- Arquivo gerado: [.ai-docs/BusinessRulesMapping.md](.ai-docs/BusinessRulesMapping.md)
- Principais arquivos de referência: 
  - [AgroSolution.Api/Controllers/IoTDataController.cs](AgroSolution.Api/Controllers/IoTDataController.cs)
  - [AgroSolution.Core/App/DTO/ReceiveIoTDataDto.cs](AgroSolution.Core/App/DTO/ReceiveIoTDataDto.cs)
  - [AgroSolution.Core/App/Validation/IoTDeviceValidator.cs](AgroSolution.Core/App/Validation/IoTDeviceValidator.cs)
  - [AgroSolution.Core/Infra/Repositories/InMemoryDeviceRepository.cs](AgroSolution.Core/Infra/Repositories/InMemoryDeviceRepository.cs)

--
_Gerado automaticamente para revisão; se quiser que eu aumente o nível de detalhe por regra (ex.: pré-condições, pós-condições, exceções e cenários de teste), diga qual regra quer aprofundar._
