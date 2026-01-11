namespace BackendV2.Api.Dto.Ops;

public class OpsJetStreamDto
{
    public bool ConsumersHealthy { get; set; }
    public long Lag { get; set; }
}

