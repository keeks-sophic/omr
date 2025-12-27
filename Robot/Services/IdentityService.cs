using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Robot.Services;

public class IdentityService
{
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(ILogger<IdentityService> logger)
    {
        _logger = logger;
    }
 
 public Task RegisterAsync(string name, string? preferredInterface = null, string? ipOverride = null)
    {
        var ip = GetIpAddress(preferredInterface, ipOverride);

        _logger.LogInformation("Registering robot. Name: {Name}, IP: {IP}", name, ip);

        // TODO: Publish registration to backend (NATS)
        return Task.CompletedTask;
    }
 
    public string? GetIpAddress(string? preferredInterface = null, string? ipOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredInterface))
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up);

            foreach (var nic in nics)
            {
                var match = nic.Name.Contains(preferredInterface, StringComparison.OrdinalIgnoreCase)
                            || nic.Description.Contains(preferredInterface, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;

                var ipProps = nic.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .Select(u => u.Address)
                    .FirstOrDefault(a =>
                        a.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(a) &&
                        !a.ToString().StartsWith("169.254."));
                if (ipv4 != null) return ipv4.ToString();

                var ipv6 = ipProps.UnicastAddresses
                    .Select(u => u.Address)
                    .FirstOrDefault(a =>
                        a.AddressFamily == AddressFamily.InterNetworkV6 &&
                        !IPAddress.IsLoopback(a) &&
                        !a.ToString().StartsWith("fe80:", StringComparison.OrdinalIgnoreCase));
                if (ipv6 != null) return ipv6.ToString();
            }
        }

        if (!string.IsNullOrWhiteSpace(ipOverride))
            return ipOverride;

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var addr in host.AddressList)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                return addr.ToString();
            }
        }
        return host.AddressList.FirstOrDefault()?.ToString();
    }
}

