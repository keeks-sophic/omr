namespace BackendV2.Api.Contracts.Commands;

public class TelescopeCommand
{
    public string Mode { get; set; } = string.Empty;
    public double Value { get; set; }
}
