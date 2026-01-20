using System.Collections.Generic;

namespace BackendV2.Api.Infrastructure.Security;

public enum BackendFeature
{
    DashboardView,
    RobotStatusView,
    MapView,
    MapEdit,
    TrafficView,
    TasksView,
    TasksControl,
    MissionRun,
    MissionLibrary,
    SimulationControl,
    ReplayView,
    ReplayControl,
    TeachSessions,
    ConfigManagement,
    OpsMonitoring,
    OpsAdmin,
    UserManagement
}

public static class RoleFeatureAccess
{
    private static readonly IReadOnlyDictionary<string, BackendFeature[]> FeaturesByRole = new Dictionary<string, BackendFeature[]>
    {
        {
            "Viewer",
            new[]
            {
                BackendFeature.DashboardView,
                BackendFeature.RobotStatusView,
                BackendFeature.MapView,
                BackendFeature.TrafficView,
                BackendFeature.TasksView,
                BackendFeature.ReplayView
            }
        },
        {
            "Operator",
            new[]
            {
                BackendFeature.DashboardView,
                BackendFeature.RobotStatusView,
                BackendFeature.MapView,
                BackendFeature.TrafficView,
                BackendFeature.TasksView,
                BackendFeature.TasksControl,
                BackendFeature.MissionRun,
                BackendFeature.ReplayView,
                BackendFeature.ReplayControl,
                BackendFeature.TeachSessions
            }
        },
        {
            "Planner",
            new[]
            {
                BackendFeature.DashboardView,
                BackendFeature.RobotStatusView,
                BackendFeature.MapView,
                BackendFeature.MapEdit,
                BackendFeature.TrafficView,
                BackendFeature.TasksView,
                BackendFeature.MissionRun,
                BackendFeature.MissionLibrary,
                BackendFeature.SimulationControl,
                BackendFeature.ReplayView
            }
        },
        {
            "Admin",
            new[]
            {
                BackendFeature.DashboardView,
                BackendFeature.RobotStatusView,
                BackendFeature.MapView,
                BackendFeature.MapEdit,
                BackendFeature.TrafficView,
                BackendFeature.TasksView,
                BackendFeature.TasksControl,
                BackendFeature.MissionRun,
                BackendFeature.MissionLibrary,
                BackendFeature.SimulationControl,
                BackendFeature.ReplayView,
                BackendFeature.ReplayControl,
                BackendFeature.TeachSessions,
                BackendFeature.ConfigManagement,
                BackendFeature.OpsMonitoring,
                BackendFeature.OpsAdmin,
                BackendFeature.UserManagement
            }
        }
    };

    public static IReadOnlyDictionary<string, BackendFeature[]> Matrix => FeaturesByRole;
}

