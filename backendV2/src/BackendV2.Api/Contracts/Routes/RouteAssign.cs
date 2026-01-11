using System.Collections.Generic;

namespace BackendV2.Api.Contracts.Routes;

public class RouteAssign
{
    public string RouteId { get; set; } = string.Empty;
    public string MapVersionId { get; set; } = string.Empty;
    public object Segments { get; set; } = new object();
}
