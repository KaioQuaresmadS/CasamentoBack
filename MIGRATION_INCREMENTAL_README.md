# 📋 Migration Incremental: AddMercadoPagoPaymentColumns

## 🎯 Objetivo

Adicionar colunas faltantes à tabela `Payments` no PostgreSQL de produção (Render) para suportar integração completa com Mercado Pago.

## ❌ Erro Atual

```
Npgsql.PostgresException 42703:
column "ExternalReference" of relation "Payments" does not exist
```

## ✅ O que será adicionado

Esta migration **incrementalmente** adiciona as seguintes colunas à tabela `Payments`:

| Coluna | Tipo | Tamanho | Propósito |
|--------|------|---------|----------|
| **MercadoPagoPaymentId** | character varying | 120 | ID do pagamento no Mercado Pago |
| **PreferenceId** | character varying | 120 | ID da preferência de checkout |
| **InitPoint** | character varying | 1000 | URL do checkout em produção |
| **SandboxInitPoint** | character varying | 1000 | URL do checkout em sandbox |
| **ExternalReference** | character varying | 160 | **← Coluna do erro 42703** |
| **PaymentMethod** | character varying | 40 | Método de pagamento (pix, credit_card, etc) |
| **PayerName** | character varying | 160 | Nome do pagador |
| **PayerEmail** | character varying | 256 | Email do pagador |
| **Status** | character varying | 40 | Status do pagamento |
| **PixQrCode** | character varying | 4000 | QR Code PIX |
| **PixCopyPaste** | character varying | 4000 | Código PIX copia-cola |
| **UpdatedAt** | timestamp | - | Última atualização |

## 📁 Arquivos da Migration

1. **[20260519120000_AddMercadoPagoPaymentColumns.cs](src/CasamentoAnaKaio.Infrastructure/Persistence/Migrations/20260519120000_AddMercadoPagoPaymentColumns.cs)**
   - Código C# da migration (Up/Down)
   - Usado pelo EF Core para aplicação automática

2. **[20260519120000_AddMercadoPagoPaymentColumns.Designer.cs](src/CasamentoAnaKaio.Infrastructure/Persistence/Migrations/20260519120000_AddMercadoPagoPaymentColumns.Designer.cs)**
   - Metadados gerados automaticamente

3. **[MIGRATION_AddMercadoPagoPaymentColumns.sql](MIGRATION_AddMercadoPagoPaymentColumns.sql)**
   - Script SQL puro para PostgreSQL
   - Alternativa manual/segura para aplicação em produção

## 🚀 Como Aplicar em Produção

### Opção 1: Automática (RECOMENDADA) ⭐

Quando você faz deploy do código com a nova migration:

```csharp
// Program.cs - executa automaticamente na inicialização
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // ← Aplica todas as migrations pendentes
}
```

**Passos:**
1. Fazer deploy do código atualizado no Render
2. A migration será aplicada automaticamente no startup
3. Verificar logs para confirmar sucesso
4. Testar endpoint POST /api/payments/create

**Vantagens:**
- ✅ Simples e automática
- ✅ Segura (usa API do EF Core)
- ✅ Compatível com múltiplas migrations futuras

### Opção 2: Manual com Script SQL

Se a aplicação falhar ou você preferir controle manual:

**Passos:**
1. Conectar ao PostgreSQL via psql ou DBeaver:
   ```bash
   psql -h dpg-d8668egjs32c73b0qlig-a.oregon-postgres.render.com \
        -U casamento_api_user \
        -d casamento_api
   ```

2. Executar o script SQL:
   ```sql
   \i MIGRATION_AddMercadoPagoPaymentColumns.sql
   ```

   Ou copiar e colar o conteúdo do arquivo no seu client SQL.

3. Verificar que as colunas foram adicionadas:
   ```sql
   SELECT column_name, data_type 
   FROM information_schema.columns 
   WHERE table_name = 'Payments' 
   ORDER BY ordinal_position;
   ```

## ⚠️ Importantes

### Segurança
- ✅ **NÃO deleta dados** - apenas adiciona colunas
- ✅ **NÃO recria tabela** - não perde dados existentes
- ✅ **NÃO apaga migrations antigas** - mantém histórico
- ✅ **Idempotente** - pode ser executada múltiplas vezes com segurança

### Tipos de Dados

Todos os tipos são **compatíveis com PostgreSQL**:
- `uuid` ← Guid em .NET
- `character varying(n)` ← string em .NET
- `numeric(10,2)` ← decimal em .NET
- `timestamp with time zone` ← DateTimeOffset em .NET

### Índices

A migration cria 3 índices para performance:
- **Unique Index** em `ExternalReference` (garante unicidade)
- **Regular Index** em `MercadoPagoPaymentId` (aceleração de queries)
- **Regular Index** em `GiftContributionId` (aceleração de joins)

## 🔍 Validação

### Após aplicação, verificar:

```sql
-- 1. Verificar que as colunas existem
SELECT COUNT(*) as total_colunas 
FROM information_schema.columns 
WHERE table_name = 'Payments';
-- Esperado: 19 colunas

-- 2. Verificar coluna ExternalReference especificamente
SELECT * FROM information_schema.columns 
WHERE table_name = 'Payments' 
AND column_name = 'ExternalReference';
-- Esperado: type character varying(160), not null

-- 3. Verificar índices
SELECT * FROM pg_indexes 
WHERE tablename = 'Payments' 
ORDER BY indexname;
-- Esperado: 3 índices criados

-- 4. Verificar histórico de migrations
SELECT * FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%AddMercadoPago%';
-- Esperado: 1 linha com migration registrada
```

## 🧪 Teste em Desenvolvimento Primeiro

**Recomendado:**

1. Clonar código localmente:
   ```bash
   git clone https://github.com/KaioQuaresmadS/CasamentoBack.git
   ```

2. Testar localmente com PostgreSQL:
   ```bash
   cd src/CasamentoAnaKaio.Api
   dotnet ef database update
   ```

3. Verificar que a migration foi aplicada e as colunas existem

4. Testar endpoints com dados reais do Mercado Pago

5. Só depois fazer deploy para produção no Render

## 📞 Se Algo Der Errado

### Erro: "Column already exists"
- Normal se a migration foi aplicada anteriormente
- O script SQL tem proteção `IF NOT EXISTS`
- Continua seguro executar novamente

### Erro: "Migration not found"
- Verificar que os arquivos de migration estão no lugar correto:
  - `src/CasamentoAnaKaio.Infrastructure/Persistence/Migrations/`

### Erro: "Migration apply failed"
- Verificar permissões do usuário PostgreSQL (casamento_api_user)
- Verificar espaço em disco disponível
- Verificar que a tabela Payments existe

### Rollback (se necessário)
1. Usar `git revert` para reverter o commit
2. Executar `dotnet ef migrations remove` (remove última migration)
3. Fazer deploy da versão anterior
4. Ou executar a seção ROLLBACK do script SQL

## 📊 Resultado Esperado

Após aplicação bem-sucedida:

✅ Endpoint `POST /api/payments/create` funcionará sem erro 42703
✅ Pagamentos serão salvos com todos os dados do Mercado Pago
✅ Queries ficarão mais rápidas com os novos índices
✅ Sistema pronto para produção

---

**Status**: ✅ Pronto para Deploy
**Data**: 19/05/2026
**Versão da Migration**: 20260519120000
