using OdectyStat1.Business;
using OdectyStat1.Contracts;

namespace OdectyStat1.DataLayer;

internal class DiagnosticsRecorder : IDiagnosticsRecorder
{
    private readonly DiagDbContext context;

    public DiagnosticsRecorder(DiagDbContext context)
    {
        this.context = context;
    }

    public async Task RecordRecognitionAsync(FileDiagnostic diagnostic, CancellationToken ct = default)
    {
        context.FileDiagnostics.Add(diagnostic);
        await context.SaveChangesAsync(ct);
    }

    public async Task RecordTransferAsync(TransferDiagnostic diagnostic, CancellationToken ct = default)
    {
        context.TransferDiagnostics.Add(diagnostic);
        await context.SaveChangesAsync(ct);
    }
}
