using System.Collections.Generic;
using System.Threading.Tasks;
using BackendV2.Api.Dto.Traffic;

namespace BackendV2.Api.Service.Traffic;

public interface ITrafficControl
{
    Task<List<RobotScheduleSummaryDto>> ComputeScheduleSummariesAsync();
    Task EmitScheduleSummaryAsync();
}

