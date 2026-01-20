using System;
using System.Collections.Generic;
using System.Linq;
 
namespace BackendV2.Api.Model.Auth;
 
[Flags]
public enum RoleType
{
    Viewer = 1,
    Operator = 2,
    Planner = 4,
    Admin = 8
}
 
public static class RoleUtils
{
    private static readonly Dictionary<string, RoleType> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Viewer"] = RoleType.Viewer,
        ["Operator"] = RoleType.Operator,
        ["Planner"] = RoleType.Planner,
        ["Admin"] = RoleType.Admin
    };
 
    public static RoleType FromName(string name) => _map.TryGetValue(name, out var r) ? r : RoleType.Viewer;
    public static string ToName(RoleType r) => _map.FirstOrDefault(kv => kv.Value == r).Key ?? "Viewer";
    public static RoleType Aggregate(IEnumerable<string> names) => names.Aggregate((RoleType)0, (acc, n) => acc | FromName(n));
    public static string[] ToNames(RoleType roles)
    {
        var list = new List<string>();
        foreach (var kv in _map)
        {
            if (roles.HasFlag(kv.Value)) list.Add(kv.Key);
        }
        return list.ToArray();
    }
}
