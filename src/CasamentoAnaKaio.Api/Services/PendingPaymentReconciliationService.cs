using CasamentoAnaKaio.Application.Services;
using Serilog;

namespace CasamentoAnaKaio.Api.Services;

public sealed class PendingPaymentReconciliationService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();
                var reconciledCount = await paymentService.ReconcilePendingMercadoPagoPaymentsAsync(100, stoppingToken);

                if (reconciledCount > 0)
                {
                    Log.Information("Reconciliação de pagamentos pendentes concluída. PaymentsChecked={PaymentsChecked}", reconciledCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Falha na reconciliação periódica de pagamentos pendentes.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
