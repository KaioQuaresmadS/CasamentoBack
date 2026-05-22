namespace CasamentoAnaKaio.Contracts.GuestConfirmations;

public sealed record GuestExportResult(
    byte[] Content,
    string FileName,
    string ContentType);
