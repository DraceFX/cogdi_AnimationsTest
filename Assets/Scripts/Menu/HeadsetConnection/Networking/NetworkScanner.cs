using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class NetworkScanner
{
    public async Task<List<string>> ScanForPortAsync(int port, int timeoutMs, int maxConcurrency, CancellationToken token)
    {
        var result = new List<string>();
        var localIp = NetworkUtil.GetPreferredLocalIPv4(out var mask);
        if (localIp == null) return result;

        var addresses = NetworkUtil.GetSubnetAddresses(localIp, mask);
        Debug.Log($"Сканируем {addresses.Count} адресов (локалка {localIp}, маска {mask})...");

        using var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = addresses.Select(async ip =>
        {
            try
            {
                await semaphore.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (await IsPortOpenAsync(ip, port, timeoutMs, token))
                {
                    lock (result) { result.Add(ip); }
                    Debug.Log($"Порт {port} открыт на {ip}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return result;
    }

    private static async Task<bool> IsPortOpenAsync(string ip, int port, int timeoutMs, CancellationToken token)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs, token));
            if (completed != connectTask) return false;
            await connectTask;
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}