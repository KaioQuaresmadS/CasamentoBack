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

## Deploy no Fly.io com banco Neon

O `fly.toml` usa Docker, expoe a API na porta `8080` e verifica saude em `/health`.
Antes do deploy, confira se o nome em `app = "casamentoanakaio-backend"` e o mesmo app criado no Fly.

Configure os segredos no Fly, mantendo a URL do Neon fora do repositorio:

```bash
fly secrets set DATABASE_URL='postgresql://usuario:senha@host/neondb?sslmode=require&channel_binding=require'
fly secrets set Jwt__Secret='uma-chave-forte-com-pelo-menos-32-caracteres'
fly secrets set MERCADOPAGO_ACCESS_TOKEN='...'
fly secrets set MERCADOPAGO_PUBLIC_KEY='...'
fly secrets set MERCADOPAGO_WEBHOOK_SECRET='...'
fly secrets set FRONTEND_URL='https://casamento-ana-kaio.netlify.app'
fly secrets set BACKEND_URL='https://casamentoanakaio-backend.fly.dev'
```

Depois:

```bash
fly deploy
fly status
fly logs
```

Se o app no Fly tiver outro nome, atualize tambem `BACKEND_URL`. Se o frontend mudar de dominio, adicione esse novo dominio em `Cors:AllowedOrigins` ou configure segredos como `Cors__AllowedOrigins__0`.
