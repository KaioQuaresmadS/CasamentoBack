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
        var uniqueConfirmations = confirmations
            .GroupBy(GetDuplicateKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(confirmation => confirmation.CreatedAt).First())
            .OrderByDescending(confirmation => confirmation.CreatedAt)
            .ToList();

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

        for (var index = 0; index < uniqueConfirmations.Count; index++)
        {
            var row = index + 2;
            var confirmation = uniqueConfirmations[index];

            worksheet.Cell(row, 1).Value = confirmation.FullName;
            worksheet.Cell(row, 2).Value = confirmation.Phone;
            worksheet.Cell(row, 3).Value = confirmation.GuestsCount;
            worksheet.Cell(row, 4).Value = confirmation.WillAttend ? "Sim" : "Não";
            worksheet.Cell(row, 5).Value = confirmation.Notes ?? string.Empty;
            worksheet.Cell(row, 6).Value = confirmation.CreatedAt.LocalDateTime;
            worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        }

        var totalRow = uniqueConfirmations.Count + 2;
        worksheet.Cell(totalRow, 1).Value = "Total de convidados confirmados";
        worksheet.Range(totalRow, 1, totalRow, 2).Merge();
        worksheet.Cell(totalRow, 3).FormulaA1 = uniqueConfirmations.Count == 0
            ? "0"
            : $"SUMIF(D2:D{totalRow - 1},\"Sim\",C2:C{totalRow - 1})";

        var totalRange = worksheet.Range(totalRow, 1, totalRow, 6);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2F0D9");

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

    private static string GetDuplicateKey(GuestConfirmation confirmation)
    {
        var phoneDigits = new string(confirmation.Phone.Where(char.IsDigit).ToArray());

        if (!string.IsNullOrWhiteSpace(phoneDigits))
        {
            return $"phone:{phoneDigits}";
        }

        return $"name:{confirmation.FullName.Trim()}";
    }
}
