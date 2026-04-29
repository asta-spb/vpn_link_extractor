using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace VpnLinkExtractor;

public sealed class AboutForm : Form
{
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkTitleBar.Apply(Handle);
    }

    public AboutForm()
    {
        Text = "About";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        Width = 600;
        Height = 400;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Sans;
        Padding = new Padding(28, 24, 28, 24);

        var title = new Label
        {
            Text = "VPN Link Extractor",
            Font = Theme.Title,
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(28, 24),
        };
        Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Extract VLESS links and XRay configs from a Happ-style subscription URL.",
            Font = Theme.Sans,
            ForeColor = Theme.TextMuted,
            AutoSize = false,
            Size = new Size(544, 20),
            Location = new Point(28, 56),
        };
        Controls.Add(subtitle);

        var version = new Label
        {
            Text = GetVersionString(),
            Font = Theme.Sans,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Location = new Point(28, 96),
        };
        Controls.Add(version);

        var divider = new Panel
        {
            BackColor = Theme.Border,
            Height = 1,
            Location = new Point(28, 128),
            Width = 544,
        };
        Controls.Add(divider);

        AddLinkRow("Project on GitHub",
            "github.com/asta-spb/vpn_link_extractor",
            "https://github.com/asta-spb/vpn_link_extractor",
            148);

        AddLinkRow("Community",
            "t.me/nastya_chtoto_delaet",
            "https://t.me/nastya_chtoto_delaet",
            196);

        AddLinkRow("Author",
            "t.me/anastasia98",
            "https://t.me/anastasia98",
            244);

        var close = new FlatBtn { Text = "Close", Width = 96 };
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.Location = new Point(ClientSize.Width - close.Width - 28, ClientSize.Height - close.Height - 24);
        close.Click += (_, _) => Close();
        Controls.Add(close);

        AcceptButton = close;
        CancelButton = close;
    }

    private void AddLinkRow(string label, string display, string url, int y)
    {
        var lbl = new Label
        {
            Text = label,
            ForeColor = Theme.TextMuted,
            Font = Theme.Sans,
            AutoSize = true,
            Location = new Point(28, y),
        };
        Controls.Add(lbl);

        var link = new LinkLabel
        {
            Text = display,
            Font = Theme.Sans,
            AutoSize = true,
            Location = new Point(28, y + 18),
            LinkColor = Theme.LinkColor,
            ActiveLinkColor = Theme.AccentHover,
            VisitedLinkColor = Theme.LinkColor,
            LinkBehavior = LinkBehavior.HoverUnderline,
            BackColor = Theme.Background,
        };
        link.LinkClicked += (_, _) => OpenUrl(url);
        Controls.Add(link);
    }

    private static void OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return;
        if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps) return;
        try
        {
            Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // ignore — best effort
        }
    }

    private static string GetVersionString()
    {
        var asm = Assembly.GetExecutingAssembly();
        var v = asm.GetName().Version;
        var verStr = v != null ? v.ToString(3) : "1.0.0";

        try
        {
            var path = asm.Location;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var built = File.GetLastWriteTime(path);
                return $"Version {verStr}  ·  build {built:yyyyMMdd.HHmm}";
            }
        }
        catch
        {
            // fall through
        }
        return $"Version {verStr}";
    }
}
