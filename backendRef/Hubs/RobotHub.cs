using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using backend.Services;
using backend.Options;
using backend.DTOs;
using Microsoft.Extensions.Logging;

namespace backend.Hubs;

public class RobotHub : Hub
{
    private readonly NatsService _nats;
    private readonly IOptions<NatsOptions> _opts;
    private readonly ILogger<RobotHub> _logger;

    public RobotHub(NatsService nats, IOptions<NatsOptions> opts, ILogger<RobotHub> logger)
    {
        _nats = nats;
        _opts = opts;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }

    public async Task SendCommand(RobotCommandDto cmd)
    {
        var subject = _opts.Value.CommandSubject;
        var payload = new
        {
            ip = cmd.Ip,
            command = cmd.Command,
            data = cmd.Data,
            ts = DateTime.UtcNow
        };
        await _nats.PublishJsonAsync(subject, payload);
        _logger.LogInformation("Command forwarded to NATS: {Ip} {Command}", cmd.Ip, cmd.Command);
        await Clients.Caller.SendAsync("commandAck", new { ip = cmd.Ip, command = cmd.Command });
    }
}
