namespace BackendV3.Modules.Maps.Dto.Requests;

public sealed class SetRestOptionsRequest
{
    public bool IsRestPath { get; set; }
    public int? RestCapacity { get; set; }
    public string? RestDwellPolicy { get; set; }
}

