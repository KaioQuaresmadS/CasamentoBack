# Casamento Ana e Kaio Backend

API ASP.NET Core para lista de presentes, RSVP, autenticacao administrativa JWT e pagamentos Mercado Pago.

## Render

Configure o servico como Web Service com:

```text
Build Command: dotnet restore && dotnet build CasamentoAnaKaio.Backend.slnx -c Release --no-restore
Start Command: dotnet run --project src/CasamentoAnaKaio.Api -c Release --no-build
```

O Render define a variavel `PORT`; a API ja esta configurada para escutar `0.0.0.0:$PORT`.

## Variaveis De Ambiente

Use nomes com `__`, que e o padrao do ASP.NET Core para sobrescrever `appsettings`:

```text
ASPNETCORE_ENVIRONMENT=Production
Jwt__Secret=<chave forte com pelo menos 32 caracteres>
Jwt__Issuer=CasamentoAnaKaio
Jwt__Audience=CasamentoAnaKaioClient
ConnectionStrings__DefaultConnection=<connection string Postgres>
MERCADOPAGO_ACCESS_TOKEN=<access token Mercado Pago>
MERCADOPAGO_PUBLIC_KEY=<public key Mercado Pago>
MERCADOPAGO_WEBHOOK_SECRET=<secret do webhook>
MERCADOPAGO_ENVIRONMENT=sandbox
FRONTEND_URL=https://seu-front-end.com
BACKEND_URL=https://casamentoback-iuya.onrender.com
AdminSeed__Email=<email admin>
AdminSeed__Name=<nome admin>
AdminSeed__Password=<senha forte>
Cors__AllowedOrigins__0=https://seu-front-end.com
```

Em producao, use `MERCADOPAGO_ENVIRONMENT=production` e credenciais de producao.

## Banco

O projeto esta configurado com Entity Framework Core para PostgreSQL via Npgsql. Antes do primeiro uso apos deploy, aplique as migrations:

```powershell
dotnet ef database update --project src/CasamentoAnaKaio.Infrastructure --startup-project src/CasamentoAnaKaio.Api
```

No Render, configure `ConnectionStrings__DefaultConnection` com a connection string externa do Postgres.

## Mercado Pago

Fluxo atual:

- `POST /api/payments/create` cria uma preferencia Checkout Pro com `external_reference`.
- `GET /api/payments/{id}/status` consulta status interno e, quando ha `payment_id`, valida contra a API do Mercado Pago.
- `POST /api/payments/webhook/mercadopago` recebe notificacoes, valida assinatura quando `MERCADOPAGO_WEBHOOK_SECRET` estiver configurado, consulta a API do Mercado Pago e so entao atualiza o banco.

Configure no painel Mercado Pago a URL de webhook:

```text
https://seu-backend.com/api/payments/webhook/mercadopago
```

Para testar localmente, exponha a API com ngrok ou localtunnel e use a URL publica no painel Mercado Pago. Use usuarios e cartoes de teste do Mercado Pago em sandbox.

## Health Check

```text
GET /health
```

## OpenAPI

```text
GET /openapi/v1.json
```
