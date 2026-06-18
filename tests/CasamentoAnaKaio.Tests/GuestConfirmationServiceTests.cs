using System.Reflection;
using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Application.Services;
using CasamentoAnaKaio.Domain.Entities;
using ClosedXML.Excel;

namespace CasamentoAnaKaio.Tests;

public sealed class GuestConfirmationServiceTests
{
    [Fact]
    public async Task ExportToExcelAsync_RemovesDuplicatedGuestsAndAddsConfirmedTotal()
    {
        var olderDuplicate = CreateConfirmation(
            "Francilene Alves Falcao Murta",
            "985860279",
            1,
            true,
            new DateTimeOffset(2026, 6, 18, 13, 41, 0, TimeSpan.Zero));
        var newerDuplicate = CreateConfirmation(
            "Francilene Alves Falcao Murta",
            "(98) 5860-279",
            3,
            true,
            new DateTimeOffset(2026, 6, 18, 13, 45, 0, TimeSpan.Zero));
        var notAttending = CreateConfirmation(
            "Hudson Vinicios de Souza da Cruz",
            "3191472783",
            4,
            false,
            new DateTimeOffset(2026, 6, 18, 13, 12, 0, TimeSpan.Zero));

        var service = new GuestConfirmationService(
            new FakeGuestConfirmationRepository([olderDuplicate, newerDuplicate, notAttending]),
            new FakeUnitOfWork());

        var export = await service.ExportToExcelAsync(CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(export.Content));
        var worksheet = workbook.Worksheet("Convidados");

        Assert.Equal("Francilene Alves Falcao Murta", worksheet.Cell(2, 1).GetString());
        Assert.Equal(3, worksheet.Cell(2, 3).GetValue<int>());
        Assert.Equal("IF(E2=\"Sim\",1+C2,0)", worksheet.Cell(2, 4).FormulaA1);
        Assert.Equal("Hudson Vinicios de Souza da Cruz", worksheet.Cell(3, 1).GetString());
        Assert.Equal("Total de convidados confirmados", worksheet.Cell(4, 1).GetString());
        Assert.Equal("SUM(D2:D3)", worksheet.Cell(4, 4).FormulaA1);
        Assert.True(worksheet.Cell(5, 1).IsEmpty());
    }

    private static GuestConfirmation CreateConfirmation(
        string fullName,
        string phone,
        int guestsCount,
        bool willAttend,
        DateTimeOffset createdAt)
    {
        var confirmation = new GuestConfirmation(fullName, phone, guestsCount, willAttend, null);
        typeof(GuestConfirmation)
            .GetProperty(nameof(GuestConfirmation.CreatedAt), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(confirmation, createdAt);

        return confirmation;
    }

    private sealed class FakeGuestConfirmationRepository(IReadOnlyList<GuestConfirmation> confirmations)
        : IGuestConfirmationRepository
    {
        public Task AddAsync(GuestConfirmation confirmation, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<GuestConfirmation>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(confirmations);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
