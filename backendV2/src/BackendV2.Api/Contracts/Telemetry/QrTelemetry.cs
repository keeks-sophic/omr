using System;

namespace BackendV2.Api.Contracts.Telemetry;

public class QrTelemetry
{
    public string QrCode { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; }
}
