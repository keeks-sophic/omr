namespace BackendV3.Endpoints;

public static class ApiRoutes
{
    public const string ApiV1 = "/api/v1";

    public static class Auth
    {
        public const string Base = ApiV1 + "/auth";
        public const string Login = Base + "/login";
        public const string Register = Base + "/register";
        public const string Logout = Base + "/logout";
        public const string Me = Base + "/me";
    }

    public static class AdminUsers
    {
        public const string Base = ApiV1 + "/admin/users";
        public const string ById = Base + "/{userId:guid}";
        public const string Disable = ById + "/disable";
        public const string Roles = ById + "/roles";
    }

    public static class Maps
    {
        public const string Base = ApiV1 + "/maps";
        public const string MapById = Base + "/{mapId:guid}";
        public const string Draft = MapById + "/draft";

        public const string Versions = MapById + "/versions";
        public const string VersionById = Versions + "/{mapVersionId:guid}";
        public const string Clone = VersionById + "/clone";
        public const string Publish = VersionById + "/publish";
        public const string Activate = VersionById + "/activate";
        public const string Snapshot = VersionById + "/snapshot";

        public const string Nodes = VersionById + "/nodes";
        public const string NodeById = Nodes + "/{nodeId:guid}";
        public const string NodeMaintenance = NodeById + "/maintenance";

        public const string Paths = VersionById + "/paths";
        public const string PathById = Paths + "/{pathId:guid}";
        public const string PathMaintenance = PathById + "/maintenance";
        public const string PathRest = PathById + "/rest";

        public const string Points = VersionById + "/points";
        public const string PointById = Points + "/{pointId:guid}";

        public const string Qrs = VersionById + "/qrs";
        public const string QrById = Qrs + "/{qrId:guid}";
    }

    public static class Robots
    {
        public const string Base = ApiV1 + "/robots";
        public const string ById = Base + "/{robotId}";
        public const string Identity = ById + "/identity";
        public const string Capability = ById + "/capability";
        public const string SettingsReported = ById + "/settings/reported";
        public const string SettingsDesired = ById + "/settings/desired";
        public const string Commands = ById + "/commands";
    }
}
