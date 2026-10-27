using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkUtil
{
    public static string GetPreferredLocalIPv4(out IPAddress mask)
    {
        mask = null;
        var candidates = new List<(string ip, IPAddress mask, int priority)>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ppp) continue;

            var props = ni.GetIPProperties();
            bool hasGateway = props.GatewayAddresses.Any(g => g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));

            foreach (var addr in props.UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                var m = addr.IPv4Mask;
                if (m == null || m.ToString() == "0.0.0.0") continue;

                var ipStr = addr.Address.ToString();
                int priority = 0;
                if (ipStr.StartsWith("192.168.")) priority += 100;
                else if (ipStr.StartsWith("10.")) priority += 80;
                else if (Is172Private(ipStr)) priority += 80;
                else if (ipStr.StartsWith("169.254.")) priority -= 100;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) priority += 50;
                else if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) priority += 40;
                if (hasGateway) priority += 30;

                candidates.Add((ipStr, m, priority));
            }
        }

        if (candidates.Count == 0) return null;

        var best = candidates.OrderByDescending(c => c.priority).First();
        mask = best.mask;
        return best.ip;
    }

    public static List<string> GetSubnetAddresses(string localIp, IPAddress mask)
    {
        var addresses = new List<string>();
        var ipBytes = IPAddress.Parse(localIp).GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        var networkBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
        }

        int hostBits = 0;
        for (int i = 0; i < 4; i++)
        {
            byte b = maskBytes[i];
            while ((b & 1) == 0 && hostBits < 32)
            {
                hostBits++; b >>= 1;
            }
        }

        if (hostBits > 8)
        {
            hostBits = 8;
        }

        int hostCount = Math.Min((1 << hostBits) - 2, 254);
        if (hostCount <= 0)
        {
            hostCount = 254;
        }

        for (int i = 1; i <= hostCount; i++)
        {
            int last = networkBytes[3] + i;
            if (last > 255) break;

            addresses.Add($"{networkBytes[0]}.{networkBytes[1]}.{networkBytes[2]}.{last}");
        }
        return addresses;
    }

    private static bool Is172Private(string ip)
    {
        if (!ip.StartsWith("172.")) return false;

        var p = ip.Split('.');
        return p.Length >= 2 && int.TryParse(p[1], out int second) && second >= 16 && second <= 31;
    }
}