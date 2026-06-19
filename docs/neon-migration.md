# Migração PostgreSQL Render para Neon

## Connection string Neon

```text
postgresql://neondb_owner:npg_1UbNXWT7tjBc@ep-rapid-lab-ad493xa2-pooler.c-2.us-east-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require
```

## Arquivos alterados

- `src/CasamentoAnaKaio.Api/appsettings.json`: substituida a connection string antiga do Render pela URL do Neon.
- `src/CasamentoAnaKaio.Api/appsettings.Production.json`: substituida a connection string antiga do Render pela URL do Neon.
- `src/CasamentoAnaKaio.Api/appsettings.Development.json`: substituida a connection string SQL Server local pela URL do Neon.
- `src/CasamentoAnaKaio.Infrastructure/DependencyInjection.cs`: mantido `UseNpgsql` e ajustada a normalizacao de URLs `postgres://`/`postgresql://` para preservar `sslmode=require` e `channel_binding=require`.
- `src/CasamentoAnaKaio.Application/CasamentoAnaKaio.Application.csproj`: trocado `BCrypt.Net-Core` por `BCrypt.Net-Next` para remover incompatibilidade NU1701 com `net10.0` e destravar build/EF.
- `src/CasamentoAnaKaio.Infrastructure/Migrations/20260619145919_UpdateDatabaseForNeon.cs`: migration criada para registrar a verificacao Neon. Ela nao possui operacoes porque o modelo atual ja estava sincronizado com o snapshot.
- `src/CasamentoAnaKaio.Infrastructure/Migrations/20260619145919_UpdateDatabaseForNeon.Designer.cs` e `AppDbContextModelSnapshot.cs`: metadados EF da migration.
- `docs/neon-database-create.sql`: script SQL completo gerado a partir das migrations.

## Configuracao EF Core/Npgsql

O `Program.cs` chama `builder.Services.AddInfrastructure(builder.Configuration)`.
Em `AddInfrastructure`, o `AppDbContext` e registrado com:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

O pacote `Npgsql.EntityFrameworkCore.PostgreSQL` ja estava instalado em `src/CasamentoAnaKaio.Infrastructure/CasamentoAnaKaio.Infrastructure.csproj`.

## Comandos executados

```bash
dotnet tool install --global dotnet-ef --version 10.0.8
dotnet restore src/CasamentoAnaKaio.Api/CasamentoAnaKaio.Api.csproj
dotnet build src/CasamentoAnaKaio.Api/CasamentoAnaKaio.Api.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false
dotnet ef migrations add UpdateDatabaseForNeon --project src/CasamentoAnaKaio.Infrastructure/CasamentoAnaKaio.Infrastructure.csproj --startup-project src/CasamentoAnaKaio.Api/CasamentoAnaKaio.Api.csproj --no-build
dotnet ef migrations script 0 --project src/CasamentoAnaKaio.Infrastructure/CasamentoAnaKaio.Infrastructure.csproj --startup-project src/CasamentoAnaKaio.Api/CasamentoAnaKaio.Api.csproj --no-build --output docs/neon-database-create.sql
dotnet ef database update --project src/CasamentoAnaKaio.Infrastructure/CasamentoAnaKaio.Infrastructure.csproj --startup-project src/CasamentoAnaKaio.Api/CasamentoAnaKaio.Api.csproj --no-build
dotnet test tests/CasamentoAnaKaio.Tests/CasamentoAnaKaio.Tests.csproj --no-restore -m:1 /nodeReuse:false /p:UseSharedCompilation=false
```

## Resultado no Neon

`dotnet ef database update` foi aplicado com sucesso no Neon.

Tabelas confirmadas:

- `GiftContributions`
- `Gifts`
- `GuestConfirmations`
- `Payments`
- `Roles`
- `UserRoles`
- `Users`
- `__EFMigrationsHistory`

Contagens confirmadas apos a criacao da estrutura:

- `Gifts`: 4
- `Roles`: 3
- `GuestConfirmations`: 0
- `Users`: 0
- `GiftContributions`: 0
- `Payments`: 0
- `UserRoles`: 0
- `__EFMigrationsHistory`: 4

Essas contagens indicam que a estrutura foi criada e os seeds de `Gifts` e `Roles` foram aplicados. Dados reais do Render nao foram migrados porque nao foi fornecido backup `.sql` nem acesso ao banco antigo.

## Plano para migrar dados do Render para Neon

Se o banco antigo do Render ainda estiver acessivel:

1. Pausar escrita na aplicacao ou colocar a aplicacao em manutencao.
2. Gerar backup do Render:

```bash
pg_dump "<RENDER_DATABASE_URL>" --format=custom --no-owner --no-acl --file render-backup.dump
```

3. Restaurar no Neon:

```bash
pg_restore --dbname "<NEON_DATABASE_URL>" --no-owner --no-acl --clean --if-exists render-backup.dump
```

Alternativa com SQL puro:

```bash
pg_dump "<RENDER_DATABASE_URL>" --no-owner --no-acl --file render-backup.sql
psql "<NEON_DATABASE_URL>" --file render-backup.sql
```

4. Rodar `dotnet ef database update` contra o Neon para garantir que o historico de migrations esta atualizado.
5. Validar contagens por tabela, login administrativo, listagem de presentes, confirmacoes de convidados e fluxo de pagamento.
6. Atualizar variaveis de ambiente da hospedagem para usar `DATABASE_URL` ou `ConnectionStrings__DefaultConnection` apontando para o Neon.

Se o banco do Render expirou ou foi removido, as migrations recriam apenas estrutura, indices, relacionamentos e seeds definidos no modelo. Elas nao recuperam usuarios, confirmacoes, pedidos ou pagamentos antigos sem backup ou acesso temporario ao banco antigo.

## Possiveis erros de migracao

- `Resource temporarily unavailable` ou erro de DNS: ambiente sem rede externa liberada para o host Neon.
- `channel binding required`: connection string sem `channel_binding=require` ou normalizacao removendo esse parametro.
- `password authentication failed`: usuario/senha incorretos ou credencial rotacionada no Neon.
- `database does not exist`: nome do banco diferente de `neondb`.
- Erros de FK/duplicidade ao restaurar backup sobre banco ja populado: restaurar em banco limpo ou usar `--clean --if-exists` com cuidado.
- Render indisponivel: nao ha como migrar registros sem backup.

## Como validar a conexao com Neon

1. Rodar a aplicacao e conferir logs de startup. O metodo `ApplyDatabaseMigrationsAsync` deve concluir sem erro.
2. Acessar `GET /health`.
3. Confirmar no painel Neon que existem as tabelas `Gifts`, `Roles`, `Users`, `Payments`, `GiftContributions`, `GuestConfirmations`, `UserRoles` e `__EFMigrationsHistory`.
4. Conferir que `__EFMigrationsHistory` tem 4 registros.
5. Fazer uma operacao real da API, como listar presentes ou registrar uma confirmacao de convidado, e verificar o registro no Neon.
