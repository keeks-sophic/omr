namespace BackendV2.Api.Contracts.Commands;

public class ModeCommand
{
    public string Mode { get; set; } = string.Empty;
    public bool? TeachEnabled { get; set; }
    public string? TeachSessionId { get; set; }
}
