using OdectyStat1.Business;

namespace OdectyStat1.Contracts;

public interface IDiagnosticsRecorder
{
    Task RecordRecognitionAsync(FileDiagnostic diagnostic, CancellationToken ct = default);
    Task RecordTransferAsync(TransferDiagnostic diagnostic, CancellationToken ct = default);
    Task RecordDeviceAsync(DeviceDiagnostic diagnostic, CancellationToken ct = default);
    Task RecordConfigAsync(CameraConfig config, CancellationToken ct = default);
}
