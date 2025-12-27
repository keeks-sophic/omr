using backend.DTOs;
using backend.Models;

namespace backend.Mappers;

public static class RobotMapper
{
    public static RobotDto ToDto(Robot robot)
    {
        return new RobotDto
        {
            Id = robot.Id,
            Name = robot.Name,
            Ip = robot.Ip,
            X = robot.X,
            Y = robot.Y,
            State = robot.State,
            Battery = robot.Battery,
            Connected = robot.Connected,
            LastActive = robot.LastActive
        };
    }

    public static Robot FromDto(RobotDto dto)
    {
        return new Robot
        {
            Id = dto.Id,
            Name = dto.Name,
            Ip = dto.Ip,
            X = dto.X,
            Y = dto.Y,
            State = dto.State,
            Battery = dto.Battery,
            Connected = dto.Connected,
            LastActive = dto.LastActive
        };
    }
}
