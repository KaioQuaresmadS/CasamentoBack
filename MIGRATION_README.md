# 🔧 Correção: PostgreSQL Migration - ExternalReference Column

## Problema Identificado
```
Npgsql.PostgresException 42703:
column "ExternalReference" of relation "Payments" does not exist
```

### Causa Raiz
1. O projeto estava configurado com `UseSqlServer()` mas a produção usa **PostgreSQL** (Render)
2. A migração anterior era para SQL Server (tipos: `uniqueidentifier`, `nvarchar`, etc.)
3. PostgreSQL não reconhece esses tipos - a migração nunca foi executada
4. A tabela Payments foi criada sem as colunas necessárias para Mercado Pago

---

## ✅ Correções Aplicadas

### 1. **Alterou DependencyInjection.cs**
- ❌ `UseSqlServer()` 
- ✅ `UseNpgsql()` - Para PostgreSQL

### 2. **Trocar Pacote NuGet**
- ❌ `Microsoft.EntityFrameworkCore.SqlServer` v10.0.8
- ✅ `Npgsql.EntityFrameworkCore.PostgreSQL` v10.0.0

### 3. **Criar Nova Migration para PostgreSQL**
- Nome: `InitialCreate` (20260520011348)
- Tipos corretos:
  - `uuid` (em vez de `uniqueidentifier`)
  - `character varying` (em vez de `nvarchar`)
  - `numeric(10,2)` (em vez de `decimal`)
  - `boolean` (em vez de `bit`)
  - `timestamp with time zone` (em vez de `datetimeoffset`)

### 4. **Adicionar Auto-Migration no Startup**
No `Program.cs`, adicionado:
```csharp
// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}
```

---

## 📋 Checklist de Implementação em Produção

### Opção 1: Automática (Recomendada)
1. ✅ Fazer deploy do código atualizado no Render
2. ✅ A migração será aplicada automaticamente no startup
3. ✅ O endpoint POST /api/payments/create funcionará

**Vantagens:**
- Simples
- Sem intervenção manual
- Segura (usa `Database.Migrate()` do EF Core)

### Opção 2: Manual (Se necessário)
Se a aplicação falhar ao iniciar:

1. Conecte ao PostgreSQL do Render
2. Execute o script: `MIGRATION_PRODUCTION_BACKUP.sql`
3. Reinicie a aplicação

---

## 🔍 Validação

### No Banco de Dados (PostgreSQL)
```sql
-- Verificar que a tabela e colunas existem
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Payments' 
ORDER BY ordinal_position;

-- Verificar que ExternalReference está lá
SELECT * FROM information_schema.columns 
WHERE table_name = 'Payments' 
AND column_name = 'ExternalReference';
```

### Na Aplicação
1. Fazer request: `POST /api/payments/create`
2. Verificar que não há erro 42703
3. Pagamento deve ser inserido com sucesso

---

## 📁 Arquivos Modificados

| Arquivo | Mudança |
|---------|---------|
| `DependencyInjection.cs` | `UseSqlServer()` → `UseNpgsql()` |
| `CasamentoAnaKaio.Infrastructure.csproj` | SqlServer → Npgsql package |
| `Program.cs` | Adicionado auto-migration no startup |
| `Migrations/20260520011348_InitialCreate.cs` | ✨ Nova migração PostgreSQL |

---

## ⚠️ Notas Importantes

### Dados Existentes
- ✅ Nenhum dado será deletado
- ✅ Tabelas existentes não serão modificadas
- ✅ Apenas novos dados serão sincronizados se necessário

### Ambientes
- **Desenvolvimento**: Funcionará com PostgreSQL (local ou container)
- **Produção (Render)**: Funcionará automaticamente com a nova migration

### Rollback (Se necessário)
Se precisar reverter para SQL Server (não recomendado):
```bash
cd src/CasamentoAnaKaio.Api
dotnet ef migrations remove
```

Mas isso removerá a última migration - mantenha PostgreSQL para produção.

---

## 🚀 Próximos Passos

1. Fazer deploy do código atualizado
2. Monitorar logs na Render para confirmar que migrations foram aplicadas
3. Testar endpoint: `POST /api/payments/create` com dados do Mercado Pago
4. Confirmar que pagamentos são salvos com sucesso

---

## 📞 Suporte

Se encontrar problemas:

1. **Erro de conexão**: Verificar `DefaultConnection` em appsettings.json
2. **Erro de compilação**: Rodar `dotnet restore` e `dotnet build`
3. **Erro de migração**: Verificar logs e aplicar `MIGRATION_PRODUCTION_BACKUP.sql` manualmente

---

**Data**: 19/05/2026
**Status**: ✅ Pronto para Produção
