using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

class Info
{
    // Global Processor Usage
    static PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

    // Global Network Stats
    static PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");
    static string instance = pcg.GetInstanceNames()[0];
    static PerformanceCounter pcsent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
    static PerformanceCounter pcreceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
    // Networking Maximums for the status bards
    static int netSendMax = 1;
    static int netRecMax = 1;
    static double netSendTotal = 0;
    static double netRecTotal = 0;

    static int prevCursorBottom = 0;

    public static void Main()
    {
        Console.CursorVisible = false;
        while (true)
        {
            // Make sure the console meets minimum size requirements
            Console.SetWindowSize(Math.Max(128, Console.WindowWidth), Math.Max(36, Console.WindowHeight));

            // If the number of rows has changed, clear the console to prevent visual artifacts
            if (prevCursorBottom != Console.CursorTop)
            {
                prevCursorBottom = Console.CursorTop;
                Console.Clear();
            }
            // Otherwise just redraw the screen
            else
            {
                Console.SetCursorPosition(0, 0);
            }

            Console.WriteLine(GetHeaderString("Processor"));
            Console.WriteLine(GetCpuUsage());
            Console.WriteLine(GetHeaderString("Memory"));
            Console.WriteLine(GetMemoryUsage());
            Console.WriteLine(GetHeaderString("Drives"));
            Console.WriteLine(GetDriveInfo());
            Console.WriteLine(GetHeaderString("Network"));
            Console.WriteLine(GetNetInfo());
            System.Threading.Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Get the storage usage info of all the drives in the machine
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Get CPU usage, process, and thread counts
    /// </summary>
    /// <returns></returns>
    public static string GetCpuUsage()
    {
        var cpuUsage = Math.Round(cpuCounter.NextValue(), 1);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{DrawProgressBar(Convert.ToInt32(cpuUsage))}  Usage: {cpuUsage}%     ");
        sb.AppendLine($"{DrawProgressBar(-1)}  Processes: {Process.GetProcesses().Length}     ");
        sb.AppendLine($"{DrawProgressBar(-1)}  Threads: {Process.GetProcesses().Sum(p => p.Threads.Count)}     ");

        return sb.ToString();
    }

    /// <summary>
    /// Get total and used memory
    /// </summary>
    /// <returns></returns>
    public static string GetMemoryUsage()
    {
        StringBuilder sb = new StringBuilder();

        ManagementObjectSearcher wmiObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

        var memoryValues = wmiObject.Get().Cast<ManagementObject>().Select(mo => new
        {
            FreePhysicalMemory = Double.Parse(mo["FreePhysicalMemory"].ToString()) * 1024,
            TotalVisibleMemorySize = Double.Parse(mo["TotalVisibleMemorySize"].ToString()) * 1024
        }).FirstOrDefault();

        sb.AppendLine($"{DrawProgressBar(-1)}  Total Memory: {BytesToDetailedString(memoryValues.TotalVisibleMemorySize)}     ");
        sb.Append($"{DrawProgressBar((int)Math.Round(((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100, 2))}  ");
        sb.Append($"In Use: {BytesToDetailedString(memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory)}");
        sb.Append($" ({Math.Round(((memoryValues.TotalVisibleMemorySize - memoryValues.FreePhysicalMemory) / memoryValues.TotalVisibleMemorySize) * 100, 2)}%)     ");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Get upload/download speeds
    /// </summary>
    /// <returns></returns>
    public static string GetNetInfo()
    {
        StringBuilder sb = new StringBuilder();
        int netRec = (int)pcreceived.NextValue();
        int netSent = (int) pcsent.NextValue();

        netRecMax = Math.Max(netRec, netRecMax);
        netSendMax = Math.Max(netSent, netSendMax);

        netRecTotal += (double)netRec;
        netSendTotal += (double)netSent;

        sb.AppendLine($"{DrawProgressBar((netRec * 100)/netRecMax)}  Download Speed: {BytesToString(netRec)}/s ({BytesToString(netRecTotal)} total)    ");
        sb.AppendLine($"{DrawProgressBar((netSent * 100) / netSendMax)}  Upload Speed: {BytesToString(netSent)}/s ({BytesToString(netSendTotal)} total)    ");

        return sb.ToString();

    }

    /// <summary>
    /// Take a number of bytes and return it with the approprite unit and one decimal point
    /// </summary>
    /// <param name="bytes">The number of bytes</param>
    /// <returns></returns>
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

    /// <summary>
    /// Take a number of bytes and return it with the approprite unit and two decimal points
    /// </summary>
    /// <param name="bytes">The number of bytes</param>
    /// <returns></returns>
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

    /// <summary>
    /// Gets a "#" enclosed header 
    /// </summary>
    /// <param name="HeaderName">The string to be enclosed by #</param>
    /// <returns>A 3 line box of # with the HeaderName inside</returns>
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

    /// <summary>
    /// Draw a progress bar given a percent complete and a total length
    /// </summary>
    /// <param name="percent">The percent completion of the task</param>
    /// <param name="length">The number of characters inside the progress bar</param>
    /// <returns>A one-line ASCII progress bar</returns>
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