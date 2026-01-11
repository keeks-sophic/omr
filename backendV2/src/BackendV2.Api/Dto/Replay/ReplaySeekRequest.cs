using System;

namespace BackendV2.Api.Dto.Replay;

public class ReplaySeekRequest
{
    public DateTimeOffset SeekTime { get; set; }
}
