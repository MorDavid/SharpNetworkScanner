using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;
using NetTools;

class SharpNetworkScanner
{
    static void PrintHelp()
    {
        Console.WriteLine("Usage: SharpNetworkScan.exe [-t|--target <target>] [-f|--file <file>] [-p|--port <port>] " +
                          "[-th|--threads <threads>] [-to|--timeout <timeout>] [-o|--output <outputFile>] " +
                          "[-h|--help]");
        Console.WriteLine("Options:");
        Console.WriteLine("  -t, --target      Specify a target.");
        Console.WriteLine("  -f, --file        Specify a file containing targets.");
        Console.WriteLine("  -p, --port        Specify port(s). Use comma-separated values or range (e.g., 80,8080 or 80-100).");
        Console.WriteLine("  -th, --threads    Specify the number of threads.");
        Console.WriteLine("  -to, --timeout    Specify the timeout value in milliseconds.");
        Console.WriteLine("  -o, --output      Specify the output file.");
        Console.WriteLine("  -h, --help        Display this help message.");
    }
    static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    static async Task WriteToFileAsync(string outputFile, string text)
    {
        if (outputFile != "")
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                using (StreamWriter writer = File.AppendText(outputFile))
                {
                    writer.WriteLine($"{text}");
                }
            }
            catch (IOException ex)
            {
                // Handle the IOException (log, display a message, etc.)
                Console.WriteLine($"Error writing to file {outputFile}: {ex.Message}");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
    static async Task Main(string[] args)
    {
        // Create a Stopwatch instance
        Stopwatch stopwatch = new Stopwatch();

        // Start the stopwatch
        stopwatch.Start();
        List<string> targets = new List<string>();
        List<int> ports = new List<int> { 20, 21, 22, 23, 53, 80, 88, 110, 123, 135, 136, 137, 138, 139, 143, 161, 162, 389, 443, 445, 636, 993, 995, 1433, 1434, 2049, 3306, 3389, 5985, 5986 }; // Default ports
        int numThreads = 1; // Default number of threads
        int timeout = 10000; // Default timeout in milliseconds
        string outputFile = ""; // Default output file

        Console.WriteLine(@"
░█▀▀░█░█░█▀█░█▀▄░█▀█  ░█▀█░█▀▀░▀█▀░█░█░█▀█░█▀▄░█░█  ░█▀▀░█▀▀░█▀█░█▀█░█▀█░█▀▀░█▀▄
░▀▀█░█▀█░█▀█░█▀▄░█▀▀  ░█░█░█▀▀░░█░░█▄█░█░█░█▀▄░█▀▄  ░▀▀█░█░░░█▀█░█░█░█░█░█▀▀░█▀▄
░▀▀▀░▀░▀░▀░▀░▀░▀░▀    ░▀░▀░▀▀▀░░▀░░▀░▀░▀▀▀░▀░▀░▀░▀  ░▀▀▀░▀▀▀░▀░▀░▀░▀░▀░▀░▀▀▀░▀░▀
                                                                    By Mor David
");
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    PrintHelp();
                    return;
                    break;
                case "-t":
                case "--target":
                    string target = args[++i];
                    targets.Add(target);
                    break;

                case "-f":
                case "--file":
                    string file = args[++i];
                    try
                    {
                        targets.AddRange(File.ReadAllLines(file));
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"Error reading targets from file: {file}");
                        return;
                    }
                    break;

                case "-p":
                case "--port":
                    ports = new List<int> {};
                    string[] portSpecifiers = args[++i].Split(',');
                    foreach (var specifier in portSpecifiers)
                    {
                        if (specifier.Contains('-'))
                        {
                            // Range specifier (e.g., 80-100)
                            string[] range = specifier.Split('-');
                            if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                            {
                                ports.AddRange(Enumerable.Range(start, end - start + 1));
                            }
                            else
                            {
                                Console.WriteLine($"Invalid port range: {specifier}");
                                return;
                            }
                        }
                        else if (int.TryParse(specifier, out int port))
                        {
                            // Single port specifier
                            ports.Add(port);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid port specifier: {specifier}");
                            return;
                        }
                    }
                    break;

                case "-th":
                case "--threads":
                    if (int.TryParse(args[++i], out int threads) && threads > 0)
                    {
                        numThreads = threads;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid number of threads. Using default value.");
                    }
                    break;

                case "-to":
                case "--timeout":
                    if (int.TryParse(args[++i], out int timeoutValue) && timeoutValue > 0)
                    {
                        timeout = timeoutValue;
                    }
                    else
                    {
                        Console.WriteLine($"Invalid timeout value. Using default value.");
                    }
                    break;

                case "-o":
                case "--output":
                    outputFile = args[++i];
                    break;

                default:
                    Console.WriteLine($"Invalid argument: {arg}");
                    return;
            }
        }

        List<Task> tasks = new List<Task>();

        // Write header to CSV file
        await WriteToFileAsync(outputFile, "Target,OpenPorts");
        

        foreach (var targetEntry in targets.Distinct())
        {
            tasks.Add(Task.Run(() => ScanTargets(targetEntry, ports.Distinct(), timeout, outputFile, numThreads)));
        }

        await Task.WhenAll(tasks);
        // Stop the stopwatch
        stopwatch.Stop();

        // Get the elapsed time in seconds
        TimeSpan elapsedTime = stopwatch.Elapsed;
        Console.WriteLine($"Execution Time: {elapsedTime.TotalSeconds} seconds");
        if (outputFile != "")
        {
            Console.WriteLine("[+] Done ... Saved in " + outputFile);
        }
        else
        {
            Console.WriteLine("[+] Done");
        }
    }

    static async Task ScanTargets(string target, IEnumerable<int> ports, int timeout, string outputFile, int numThreads)
    {
        if (IPAddressRange.TryParse(target, out _))
        {
            await ScanSubnet(target, ports, timeout, outputFile, numThreads);
        }
        else
        {
            await ScanPorts(target, ports, timeout, outputFile, numThreads);
        }
    }

    static async Task ScanPorts(string target, IEnumerable<int> ports, int timeout, string outputFile, int numThreads)
    {
        List<string> openPorts = new List<string>();

        // Generate a random list of ports for each host
        List<int> randomPorts = ports.OrderBy(p => Guid.NewGuid()).ToList();

        // Use a semaphore to control the number of concurrent tasks
        SemaphoreSlim portSemaphore = new SemaphoreSlim(numThreads, numThreads);

        var tasks = randomPorts.Select(port => Task.Run(async () =>
        {
            // Wait for semaphore before starting a new task
            await portSemaphore.WaitAsync();

            try
            {
                if (IsPortOpen(target, port, timeout))
                {
                    openPorts.Add(port.ToString());
                    Console.WriteLine($"{target}:{port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MD Error: ");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                // Release semaphore when the task is done
                portSemaphore.Release();
            }
        }));

        await Task.WhenAll(tasks);

        // Write results to CSV file 
        string t_ports = "";
        foreach (string port in openPorts)
        {
            if (port.Contains(":"))
            {
                t_ports = port.Split(':')[1] + "," + t_ports;
            }
            else
            {
                t_ports = port + "," + t_ports;
            }
        }
        if (t_ports != "")
        {
            await WriteToFileAsync(outputFile, $"\"{target}\",\"{t_ports}\"");
        }
    }

    static async Task ScanSubnet(string subnet, IEnumerable<int> ports, int timeout, string outputFile, int numThreads)
    {
        List<string> openPorts = new List<string>();

        Console.WriteLine($"[+] Scanning {subnet}...");

        // Generate a list of IP addresses in the specified subnet
        List<IPAddress> ipAddresses = GetIpAddressesInSubnet(subnet);

        // Use a semaphore to control the number of concurrent tasks
        SemaphoreSlim ipSemaphore = new SemaphoreSlim(numThreads, numThreads);

        var tasks = ipAddresses.Select(ip => Task.Run(async () =>
        {
            await ScanPorts(ip.ToString(), ports, timeout, outputFile, numThreads);
        }));

        await Task.WhenAll(tasks);
    }

    static List<IPAddress> GetIpAddressesInSubnet(string subnet)
    {
        List<IPAddress> ipAddresses = new List<IPAddress>();

        IPAddressRange tempIPAddressRange = IPAddressRange.Parse(subnet);
        foreach (var onceip in tempIPAddressRange)
        {
            ipAddresses.Add(onceip);
        }

        return ipAddresses;
    }

    static bool IsPortOpen(string host, int port, int timeout)
    {
        try
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                var task = tcpClient.ConnectAsync(host, port);
                if (Task.WaitAny(new Task[] { task }, timeout) == -1)
                {
                    // Timeout occurred
                    return false;
                }

                return tcpClient.Connected;
            }
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
