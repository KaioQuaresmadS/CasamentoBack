# Casamento Ana e Kaio Backend

## Mercado Pago

O fluxo publico de pagamento usa Mercado Pago Checkout Pro para Pix, boleto e cartao. O sistema cria a preferencia no backend, abre o checkout no frontend e confirma pagamento apenas depois do webhook consultar a API real do Mercado Pago.

Variaveis de ambiente aceitas:

```text
MERCADOPAGO_ACCESS_TOKEN=<access token sandbox ou producao>
MERCADOPAGO_PUBLIC_KEY=<public key sandbox ou producao>
MERCADOPAGO_WEBHOOK_SECRET=<secret de webhook>
MERCADOPAGO_ENVIRONMENT=sandbox
FRONTEND_URL=http://localhost:4200
BACKEND_URL=https://sua-api-publica
```

No ASP.NET Core tambem e possivel usar:

```text
MercadoPago__AccessToken=<access token>
MercadoPago__PublicKey=<public key>
MercadoPago__WebhookSecret=<secret>
MercadoPago__Environment=sandbox
MercadoPago__FrontendUrl=http://localhost:4200
MercadoPago__BackendUrl=https://sua-api-publica
```

Configure no painel Mercado Pago o webhook:

```text
POST {BACKEND_URL}/api/payments/webhook/mercadopago
```

Para testar localmente, exponha a API com ngrok ou localtunnel e use essa URL publica em `BACKEND_URL` e no painel do Mercado Pago. Use credenciais e usuarios/cartoes de teste do Mercado Pago em sandbox. Para producao, troque `MERCADOPAGO_ENVIRONMENT=production`, use credenciais de producao e atualize `FRONTEND_URL`/`BACKEND_URL`.

Depois de atualizar o banco:

```powershell
dotnet ef database update --project src/CasamentoAnaKaio.Infrastructure --startup-project src/CasamentoAnaKaio.Api
```
