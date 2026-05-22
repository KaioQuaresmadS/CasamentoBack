using CasamentoAnaKaio.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Controllers;

[ApiController]
[Route("api/guests")]
public sealed class GuestsController(GuestConfirmationService service) : ControllerBase
{
    [HttpGet("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var export = await service.ExportToExcelAsync(cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
