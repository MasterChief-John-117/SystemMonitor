using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

class Info
{
    static PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    static PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");
    static string instance = pcg.GetInstanceNames()[0];
    static PerformanceCounter pcsent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
    static PerformanceCounter pcreceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

    static int netSendMax = 1;
    static int netRecMax = 1;

    public static void Main()
    {
        while (true)
        {
            Console.SetWindowSize(Math.Max(128, Console.WindowWidth), Math.Max(36, Console.WindowHeight));
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(GetHeaderString("Processor"));
            Console.WriteLine(GetCpuUsage());
            Console.WriteLine(GetHeaderString("Memory"));
            Console.WriteLine(GetMemoryUsage());
            Console.WriteLine(GetHeaderString("Drives"));
            Console.WriteLine(GetDriveInfo());

            Console.WriteLine(GetHeaderString("Network"));
            GetNetInfo();
            System.Threading.Thread.Sleep(1000);
        }
    }

    public static string GetDriveInfo()
    {
        StringBuilder sb = new StringBuilder();
        DriveInfo[] drives = DriveInfo.GetDrives();
        foreach (DriveInfo drive in drives)
        {
            sb.Append(DrawProgressBar(Convert.ToInt32(100 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize)) + "  ");

            sb.AppendLine($"{drive.Name}{drive.VolumeLabel}: {BytesToString(drive.AvailableFreeSpace)} of {BytesToString(drive.TotalSize)} free ({BytesToString(drive.TotalSize - drive.TotalFreeSpace)} used)     ");
        }

        return sb.ToString();
    }

    public static string GetCpuUsage()
    {
        var cpuUsage = Math.Round(cpuCounter.NextValue(), 1);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{DrawProgressBar(Convert.ToInt32(cpuUsage))}  Usage: {cpuUsage}%  ");
        sb.AppendLine($"{DrawProgressBar(-1)}  Processes: {Process.GetProcesses().Length}");
        sb.AppendLine($"{DrawProgressBar(-1)}  Threads: {Process.GetProcesses().Sum(p => p.Threads.Count)}");

        return sb.ToString();
    }

    public static string GetMemoryUsage()
    {
        StringBuilder sb = new StringBuilder();

        ManagementObjectSearcher wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

        var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
        {
            FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()) * 1024,
            TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString()) * 1024
        }).FirstOrDefault();

        sb.AppendLine($"{DrawProgressBar(-1)}  Total Memory: {BytesToDetailedString(memoryValues.TotalVisibleMemorySize)}");
        sb.Append($"{DrawProgressBar((int)Math.Round(((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100, 2))}  ");
        sb.Append($"In Use: {BytesToDetailedString(memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory)}");
        sb.Append($" ({Math.Round(((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100, 2)}%)    ");
        sb.AppendLine();

        return sb.ToString();
    }

    public static void GetNetInfo()
    {
        int netRec = (int)pcreceived.NextValue();
        int netSent = (int) pcsent.NextValue();

        netRecMax = Math.Max(netRec, netRecMax);
        netSendMax = Math.Max(netSent, netSendMax);

        Console.WriteLine($"{DrawProgressBar((netRec * 100)/netRecMax)}  Download Speed: {BytesToString(netRec)}/s   ");
        Console.WriteLine($"{DrawProgressBar((netSent * 100) / netSendMax)}  Upload Speed: {BytesToString(netSent)}/s   ");

    }

    public static string BytesToString(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes = bytes / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        string result = String.Format("{0:0.#}{1}", bytes, sizes[order]);

        return result;
    }

    public static string BytesToDetailedString(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes = bytes / 1024;
        }

        // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
        // show a single decimal place, and no space.
        string result = String.Format("{0:0.##}{1}", bytes, sizes[order]);

        return result;
    }

    public static string GetHeaderString(string HeaderName)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('#', HeaderName.Length + 10);
        sb.AppendLine();
        sb.Append('#', 2);
        sb.Append(' ', 3);
        sb.Append(HeaderName);
        sb.Append(' ', 3);
        sb.Append('#', 2);
        sb.AppendLine();
        sb.Append('#', HeaderName.Length + 10);
        return sb.ToString();
    }

    public static string DrawProgressBar(int percent, int length = 50)
    {
        char emptyChar = '-';
        char filledChar = '|';
        if (percent == -1)
        {
            int fillerLen = (length - "Not Available".Length) / 2;
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(emptyChar, fillerLen);
            sb.Append("Not Available");
            sb.Append(emptyChar, length - (fillerLen + "Not Available".Length));
            sb.Append(']');
            return sb.ToString();
        }
        else
        {
            int filledCount = (length * percent) / 100;
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(filledChar, filledCount);
            sb.Append(emptyChar, length - filledCount);
            sb.Append(']');
            return sb.ToString();
        }
    }
}