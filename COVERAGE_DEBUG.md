# SonarCloud Gateway Coverage Investigation

## Status Atual
- ✅ Testes: 77 passando (100%)
- ✅ Cobertura local: 100% (314/314 linhas)
- ✅ Arquivo coverage.opencover.xml sendo gerado
- ❌ SonarCloud não está detectando a cobertura

## Arquivos Gerados
```
TestResults/{guid}/coverage.opencover.xml
```

## Configurações Verificadas

### 1. coverlet.runsettings ✅
```xml
- Format: opencover
- Exclude: [*.Tests]*
- IncludeTestAssembly: false
```

### 2. Workflow ci-pull-request.yml
```yaml
/d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"
--collect:"XPlat Code Coverage"
--settings coverlet.runsettings
```

### 3. Estrutura do Projeto
```
fiap-x-api-gateway.sln
├── src/Gateway.csproj (código fonte)
└── tests/Gateway.Tests.csproj (testes)
```

## Problemas Identificados

### ❌ PROBLEMA 1: Teste não especifica solution/projeto
**Atual:**
```bash
dotnet test --configuration Release --no-build
```

**Deveria ser:**
```bash
dotnet test fiap-x-api-gateway.sln --configuration Release --no-build
```

### ❌ PROBLEMA 2: Falta especificar o projeto de teste
O workflow pode estar tentando testar o projeto errado.

### ❌ PROBLEMA 3: Paths podem estar relativos ao workspace raiz
SonarScanner pode não estar encontrando os arquivos porque está procurando no path errado.

## Soluções Propostas

### Opção A: Usar absolute paths
```yaml
/d:sonar.cs.opencover.reportsPaths="TestResults/**/coverage.opencover.xml"
```

### Opção B: Adicionar verbose logging
```yaml
dotnet sonarscanner begin ... /d:sonar.verbose=true
```

### Opção C: Especificar test project explicitamente
```yaml
dotnet test tests/Gateway.Tests.csproj
```

### Opção D: Adicionar sonar.coverageReportPaths
Alguns projetos .NET precisam de:
```yaml
/d:sonar.coverageReportPaths="TestResults/**/coverage.opencover.xml"
```

### Opção E: Verificar se o arquivo realmente existe no runner
```yaml
- name: Verify Coverage Files
  run: |
    echo "=== Looking for coverage files ==="
    find . -name "coverage.opencover.xml" -type f
    echo "=== Coverage file content check ==="
    ls -lh TestResults/*/coverage.opencover.xml
```

## Próximos Passos

1. **INVESTIGAR**: Adicionar step de debug no workflow para listar arquivos
2. **CORRIGIR**: Especificar solution file no dotnet test
3. **VALIDAR**: Adicionar verbose logging no SonarScanner
4. **TESTAR**: Rodar e verificar logs do GitHub Actions
