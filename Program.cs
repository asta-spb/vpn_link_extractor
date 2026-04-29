using System;
using System.IO;
using System.Windows.Forms;

namespace VpnLinkExtractor;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--test")
        {
            var body = File.ReadAllText(args[1]);
            var entries = SubscriptionFetcher.Parse(body);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine($"count={entries.Count}");
            foreach (var e in entries) Console.WriteLine(e.VlessUri);
            return 0;
        }
        if (args.Length >= 2 && args[0] == "--dump-config")
        {
            var body = File.ReadAllText(args[1]);
            var entries = SubscriptionFetcher.Parse(body);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var idx = args.Length >= 3 && int.TryParse(args[2], out var i) ? i : 1;
            if (idx >= 0 && idx < entries.Count) Console.WriteLine(entries[idx].ConfigJson);
            return 0;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
