namespace BackendV2.Api.Contracts.Commands;

public class HoistCommand
{
    public string Mode { get; set; } = string.Empty;
    public double Value { get; set; }
}
