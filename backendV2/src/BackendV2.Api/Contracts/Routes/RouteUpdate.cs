namespace BackendV2.Api.Contracts.Routes;

public class RouteUpdate
{
    public string RouteId { get; set; } = string.Empty;
    public object Segments { get; set; } = new object();
}
