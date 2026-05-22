using CasamentoAnaKaio.Application.Abstractions;
using CasamentoAnaKaio.Contracts.GuestConfirmations;
using CasamentoAnaKaio.Domain.Entities;
using ClosedXML.Excel;

namespace CasamentoAnaKaio.Application.Services;

public sealed class GuestConfirmationService(
    IGuestConfirmationRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<GuestConfirmationResponse> CreateAsync(
        CreateGuestConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var confirmation = new GuestConfirmation(
            request.FullName,
            request.Phone,
            request.GuestsCount,
            request.WillAttend,
            request.Notes);

        await repository.AddAsync(confirmation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(confirmation);
    }

    public async Task<IReadOnlyList<GuestConfirmationResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var confirmations = await repository.ListAsync(cancellationToken);
        return confirmations.Select(Map).ToList();
    }

    public async Task<GuestExportResult> ExportToExcelAsync(CancellationToken cancellationToken)
    {
        var confirmations = await repository.ListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Convidados");

        // Cabecalhos exigidos para a planilha administrativa.
        worksheet.Cell(1, 1).Value = "Nome completo";
        worksheet.Cell(1, 2).Value = "Telefone";
        worksheet.Cell(1, 3).Value = "Nº de acompanhantes";
        worksheet.Cell(1, 4).Value = "Vai comparecer";
        worksheet.Cell(1, 5).Value = "Observações";
        worksheet.Cell(1, 6).Value = "Data de confirmação";

        var header = worksheet.Range(1, 1, 1, 6);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEDED");

        for (var index = 0; index < confirmations.Count; index++)
        {
            var row = index + 2;
            var confirmation = confirmations[index];

            worksheet.Cell(row, 1).Value = confirmation.FullName;
            worksheet.Cell(row, 2).Value = confirmation.Phone;
            worksheet.Cell(row, 3).Value = confirmation.GuestsCount;
            worksheet.Cell(row, 4).Value = confirmation.WillAttend ? "Sim" : "Não";
            worksheet.Cell(row, 5).Value = confirmation.Notes ?? string.Empty;
            worksheet.Cell(row, 6).Value = confirmation.CreatedAt.LocalDateTime;
            worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"convidados_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return new GuestExportResult(
            stream.ToArray(),
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static GuestConfirmationResponse Map(GuestConfirmation confirmation)
    {
        return new GuestConfirmationResponse(
            confirmation.Id,
            confirmation.FullName,
            confirmation.Phone,
            confirmation.GuestsCount,
            confirmation.WillAttend,
            confirmation.Notes,
            confirmation.CreatedAt);
    }
}
