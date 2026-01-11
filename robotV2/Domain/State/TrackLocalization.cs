using System;

namespace Robot.Domain.State;

public class TrackLocalization
{
    public string? LastQrCode { get; set; }
    public DateTimeOffset? LastQrTime { get; set; }
    public object? TrackPosition { get; set; }
}

