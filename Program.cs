using System.Diagnostics;
using System.Globalization;
using RestartDisplay;
using Path = System.IO.Path;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            int id = 0;
            bool link = false;
            foreach (string arg in args)
            {
                if (arg.Equals("-l", StringComparison.OrdinalIgnoreCase) || arg.Equals("-link", StringComparison.OrdinalIgnoreCase))
                {
                    if (link) throw new ArgumentException("Specify the shortcut option only once.");
                    link = true;
                }
                else if (id == 0 && int.TryParse(arg, out int parsed) && parsed > 0) id = parsed;
                else throw new ArgumentException("Use RestartDisplay.exe [positive monitor ID] [-l | -link].");
            }
            if (link && id == 0) throw new ArgumentException("Shortcut creation requires a positive monitor ID.");
            int timeout = ReadTimeout();
            if (link)
            {
                string exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the executable.");
                string destination = Path.Combine(AppContext.BaseDirectory, $"Restart {id}.lnk");
                Shortcut.Create(exe, id, destination);
                Console.WriteLine($"Created \"{destination}\" -> \"{exe}\" {id}");
                return 0;
            }
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: RestartDisplay.exe <monitor ID>       Restart a monitor.");
                Console.WriteLine("       RestartDisplay.exe <monitor ID> -l    Create Restart #.lnk (-link also works).");
                Console.WriteLine("timeout.txt beside the executable sets the disconnect time in seconds (default: 5).");
                Console.WriteLine($"Current timeout: {timeout} seconds. IDs below belong to this app, not Windows Settings.");
                Console.WriteLine("Re-list after changing cables or docks.\n");
            }
            Snapshot snapshot = Config.Query();
            if (id == 0)
            {
                Console.WriteLine($"{"ID",-4} {"Name",-28} {"Windows device",-16} {"Active",-7} {"Primary",-8} {"Resolution",-15} Position");
                foreach (Display d in Config.List(snapshot))
                    Console.WriteLine($"{d.MonitorId,-4} {d.Name,-28} {d.WindowsDevice,-16} {d.Active,-7} {d.Primary,-8} {d.Resolution,-15} {d.Position}");
                return 0;
            }
            Cycle cycle = Config.Prepare(snapshot, id);
            using var cancelled = new ManualResetEventSlim();
            ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancelled.Set(); };
            Console.CancelKeyPress += handler;
            try
            {
                try
                {
                    Console.WriteLine($"Disconnecting monitor {id} ...");
                    Config.Disconnect(cycle);
                    var timer = Stopwatch.StartNew();
                    int last = -1;
                    while (timer.Elapsed.TotalSeconds < timeout && !cancelled.IsSet)
                    {
                        int remaining = (int)Math.Ceiling(timeout - timer.Elapsed.TotalSeconds);
                        if (remaining != last) Console.WriteLine($"Reconnecting in {remaining} seconds ...");
                        last = remaining;
                        cancelled.Wait(100);
                    }
                }
                finally
                {
                    Console.WriteLine($"Reconnecting monitor {id} ...");
                    Config.Reconnect(cycle);
                }
            }
            finally { Console.CancelKeyPress -= handler; }
            Console.WriteLine("Done. Display is extended.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
            return 1;
        }
    }

    private static int ReadTimeout()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "timeout.txt");
        try
        {
            string value = File.ReadAllText(file).Trim();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) && seconds >= 0)
            {
                Console.WriteLine($"Successfully read timeout.txt: {seconds} seconds.");
                return seconds;
            }
            Console.WriteLine("Invalid timeout.txt; expected a nonnegative integer. Using 5 seconds.");
        }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Cannot read timeout.txt ({ex.Message}). Using 5 seconds.");
        }
        return 5;
    }
}
