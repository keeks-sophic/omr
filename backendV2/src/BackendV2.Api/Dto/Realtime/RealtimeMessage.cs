using System;

namespace BackendV2.Api.Dto.Realtime;

public class RealtimeMessage<T>
{
    public string Topic { get; set; } = string.Empty;
    public DateTimeOffset Ts { get; set; } = DateTimeOffset.UtcNow;
    public T Payload { get; set; } = default!;
}
