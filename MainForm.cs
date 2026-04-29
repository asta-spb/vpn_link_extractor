using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VpnLinkExtractor;

public sealed class MainForm : Form
{
    private readonly ComboBox _urlCombo;
    private readonly CheckBox _rememberCheckbox;
    private readonly FlatBtn _fetchButton;
    private readonly FlatBtn _aboutButton;
    private readonly FlatBtn _copyLinksButton;
    private readonly FlatBtn _saveLinksButton;
    private readonly FlatBtn _saveConfigsButton;
    private readonly FlatBtn _toggleConfigButton;
    private readonly FlatBtn _copyConfigButton;
    private readonly Label _serversHeader;
    private readonly ListBox _resultList;
    private readonly TextBox _configBox;
    private readonly Label _statusLabel;
    private readonly SplitContainer _split;
    private readonly AppSettings _settings;
    private CancellationTokenSource? _cts;
    private const string ShowJsonText = "Show JSON Config";
    private const string HideJsonText = "Hide JSON Config";

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        DarkTitleBar.Apply(Handle);
    }

    public MainForm()
    {
        _settings = AppSettings.Load();

        try
        {
            using var s = typeof(MainForm).Assembly.GetManifestResourceStream("VpnLinkExtractor.app.ico");
            if (s != null) Icon = new System.Drawing.Icon(s);
        }
        catch { /* ignore — fall back to default */ }

        Text = "VPN Link Extractor";
        MinimumSize = new Size(960, 640);
        Width = 1180;
        Height = 760;
        BackColor = Theme.Background;
        ForeColor = Theme.Text;
        Font = Theme.Sans;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.Background,
            Padding = new Padding(20, 16, 20, 16),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 91));   // url card
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // status
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // split
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));    // bottom spacer
        Controls.Add(root);

        // ── Header bar ────────────────────────────────────────────
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(0, 4, 0, 0),
        };
        titleStack.Controls.Add(new Label
        {
            Text = "VPN Link Extractor",
            Font = Theme.Title,
            ForeColor = Theme.Text,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
        });
        titleStack.Controls.Add(new Label
        {
            Text = "Extract VLESS links from Happ subscription URLs",
            Font = Theme.Sans,
            ForeColor = Theme.TextMuted,
            AutoSize = true,
            Margin = new Padding(0),
        });
        header.Controls.Add(titleStack, 0, 0);

        _aboutButton = new FlatBtn { Text = "About", Width = 96, Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(0, 12, 0, 0) };
        _aboutButton.Click += (_, _) => new AboutForm().ShowDialog(this);
        header.Controls.Add(_aboutButton, 1, 0);
        root.Controls.Add(header, 0, 0);

        // ── URL card ──────────────────────────────────────────────
        var urlCard = MakeCard();

        var urlLabel = new Label
        {
            Text = "Subscription URL",
            Font = Theme.Sans,
            ForeColor = Theme.TextMuted,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 22,
        };

        var urlInputRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Surface,
        };
        urlInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        urlInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        urlInputRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        urlInputRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var urlBoxWrap = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 43,
            BackColor = Theme.Border,
            Padding = new Padding(1),
            Margin = new Padding(0, 0, 8, 0),
        };
        var urlBoxInner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceAlt,
            Padding = new Padding(8, 0, 4, 0),
            ColumnCount = 1,
            RowCount = 1,
        };
        urlBoxInner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        urlBoxInner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _urlCombo = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.Text,
            Font = new Font("Segoe UI", 10F),
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
            MaxDropDownItems = 10,
            Margin = new Padding(0),
        };
        _urlCombo.DrawItem += UrlCombo_DrawItem;
        _urlCombo.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FetchClicked(); }
        };
        urlBoxInner.Controls.Add(_urlCombo, 0, 0);
        urlBoxWrap.Controls.Add(urlBoxInner);

        _rememberCheckbox = new CheckBox
        {
            Text = "Remember URLs",
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 32,
            ForeColor = Theme.Text,
            BackColor = Theme.Surface,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 8, 0),
        };
        _rememberCheckbox.FlatAppearance.BorderSize = 0;
        _rememberCheckbox.FlatAppearance.CheckedBackColor = Theme.Accent;
        _rememberCheckbox.FlatAppearance.MouseOverBackColor = Theme.SurfaceAlt;

        _fetchButton = new FlatBtn
        {
            Text = "Fetch",
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 32,
            Margin = new Padding(0),
        };
        _fetchButton.ApplyPrimary();
        _fetchButton.Click += (_, _) => FetchClicked();

        urlInputRow.Controls.Add(urlBoxWrap, 0, 0);
        urlInputRow.Controls.Add(_rememberCheckbox, 1, 0);
        urlInputRow.Controls.Add(_fetchButton, 2, 0);

        urlCard.Controls.Add(urlInputRow);
        urlCard.Controls.Add(urlLabel);
        root.Controls.Add(urlCard, 0, 1);

        // ── Status row ────────────────────────────────────────────
        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextMuted,
            Font = Theme.Sans,
            Text = "Ready. Paste a Happ-style subscription URL and press Fetch.",
            Padding = new Padding(2, 0, 0, 0),
        };
        root.Controls.Add(_statusLabel, 0, 2);

        // ── Split: list + config ──────────────────────────────────
        _split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BackColor = Theme.Background,
            SplitterWidth = 8,
        };
        root.Controls.Add(_split, 0, 3);

        // -- list panel
        var listCard = MakeCard();
        var listGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
        };
        listGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        listGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        listGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _serversHeader = new Label
        {
            Text = "Servers",
            Font = Theme.SansBold,
            ForeColor = Theme.Text,
            AutoSize = false,
            Dock = DockStyle.Fill,
        };
        listGrid.Controls.Add(_serversHeader, 0, 0);

        _resultList = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.Text,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            SelectionMode = SelectionMode.MultiExtended,
            DisplayMember = nameof(VpnEntry.Display),
        };
        _resultList.SelectedIndexChanged += (_, _) => UpdateConfigView();
        listGrid.Controls.Add(_resultList, 0, 1);

        var listActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 8, 0, 4),
        };
        _copyLinksButton = new FlatBtn { Text = "Copy links", Width = 110 };
        _copyLinksButton.Click += (_, _) => CopyLinks();
        _saveLinksButton = new FlatBtn { Text = "Save links…", Width = 130 };
        _saveLinksButton.Click += (_, _) => SaveLinks();
        _saveConfigsButton = new FlatBtn { Text = "Save configs…", Width = 140 };
        _saveConfigsButton.Click += (_, _) => SaveConfigs();
        _toggleConfigButton = new FlatBtn { Text = ShowJsonText, Width = 160 };
        _toggleConfigButton.Click += (_, _) => ToggleJsonPanel();
        listActions.Controls.AddRange(new Control[] { _copyLinksButton, _saveLinksButton, _saveConfigsButton, _toggleConfigButton });
        listGrid.Controls.Add(listActions, 0, 2);

        listCard.Controls.Add(listGrid);
        _split.Panel1.Controls.Add(listCard);

        // -- config panel
        var configCard = MakeCard();
        var configGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
        };
        configGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        configGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        configGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        configGrid.Controls.Add(new Label
        {
            Text = "Config (JSON)",
            Font = Theme.SansBold,
            ForeColor = Theme.Text,
            AutoSize = false,
            Dock = DockStyle.Fill,
        }, 0, 0);

        _configBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Dock = DockStyle.Fill,
            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.Text,
            Font = Theme.Mono,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
        };
        configGrid.Controls.Add(_configBox, 0, 1);

        var configActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 8, 0, 4),
        };
        _copyConfigButton = new FlatBtn { Text = "Copy config", Width = 120 };
        _copyConfigButton.Click += (_, _) => CopySelectedConfig();
        configActions.Controls.Add(_copyConfigButton);
        configGrid.Controls.Add(configActions, 0, 2);

        configCard.Controls.Add(configGrid);
        _split.Panel2.Controls.Add(configCard);

        // ── Apply persisted state ────────────────────────────────
        ApplyGeometry(_settings.Window);
        _rememberCheckbox.Checked = _settings.RememberUrls;
        RefreshRecentUrls();
        if (_settings.RecentUrls.Count > 0) _urlCombo.Text = _settings.RecentUrls[0];

        FormClosing += (_, _) => SaveSettings();

        Shown += (_, _) =>
        {
            try
            {
                _split.Panel1MinSize = 160;
                _split.Panel2MinSize = 160;
                if (_split.Height >= 320) _split.SplitterDistance = (int)(_split.Height * 0.55);
                _split.Panel2Collapsed = true;
            }
            catch { }
        };
    }

    private void UrlCombo_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected ? Theme.Accent : Theme.SurfaceAlt);
        e.Graphics.FillRectangle(bg, e.Bounds);
        var item = _urlCombo.Items[e.Index]?.ToString() ?? "";
        var textBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, item, _urlCombo.Font, textBounds, Theme.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void RefreshRecentUrls()
    {
        var current = _urlCombo.Text;
        _urlCombo.BeginUpdate();
        _urlCombo.Items.Clear();
        foreach (var u in _settings.RecentUrls) _urlCombo.Items.Add(u);
        _urlCombo.EndUpdate();
        _urlCombo.Text = current;
    }

    private void ApplyGeometry(WindowGeometry g)
    {
        if (g.Width is int w && w >= MinimumSize.Width && w <= 8192) Width = w;
        if (g.Height is int h && h >= MinimumSize.Height && h <= 8192) Height = h;

        if (g.X is int x && g.Y is int y)
        {
            // Require most of the title bar to sit inside a working area so the window stays grabbable.
            var titleBar = new Rectangle(x + 80, y, Math.Max(40, Width - 160), 30);
            if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(titleBar)))
            {
                StartPosition = FormStartPosition.Manual;
                Location = new Point(x, y);
            }
        }

        if (g.Maximized) WindowState = FormWindowState.Maximized;
    }

    private void SaveSettings()
    {
        var g = new WindowGeometry();
        if (WindowState == FormWindowState.Maximized)
        {
            g.Maximized = true;
            var rb = RestoreBounds;
            g.Width = rb.Width;
            g.Height = rb.Height;
            g.X = rb.X;
            g.Y = rb.Y;
        }
        else
        {
            g.Width = Width;
            g.Height = Height;
            g.X = Location.X;
            g.Y = Location.Y;
        }
        _settings.Window = g;
        _settings.RememberUrls = _rememberCheckbox.Checked;
        _settings.Save();
    }

    private void ToggleJsonPanel()
    {
        _split.Panel2Collapsed = !_split.Panel2Collapsed;
        _toggleConfigButton.Text = _split.Panel2Collapsed ? ShowJsonText : HideJsonText;
        if (!_split.Panel2Collapsed) UpdateConfigView();
    }

    private static Panel MakeCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 0, 0, 12),
        };
        card.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var pen = new Pen(Theme.Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return card;
    }

    // ── Actions ───────────────────────────────────────────────────
    private void FetchClicked()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = FetchAsync(_urlCombo.Text.Trim(), _cts.Token);
    }

    private const int MaxUrlLength = 4096;

    private async Task FetchAsync(string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) { SetStatus("Enter a subscription URL.", Theme.Error); return; }
        if (url.Length > MaxUrlLength) { SetStatus($"URL too long (max {MaxUrlLength}).", Theme.Error); return; }
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            SetStatus("URL must start with http:// or https://", Theme.Error);
            return;
        }

        _fetchButton.Enabled = false;
        SetStatus("Fetching…", Theme.TextMuted);
        _resultList.BeginUpdate();
        _resultList.Items.Clear();
        _resultList.EndUpdate();
        _configBox.Text = "";
        UpdateServersHeader();

        try
        {
            var entries = await SubscriptionFetcher.FetchAsync(url, ct).ConfigureAwait(true);
            if (entries.Count == 0) { SetStatus("No VLESS entries found in the response.", Theme.Error); return; }

            _resultList.BeginUpdate();
            foreach (var e in entries) _resultList.Items.Add(e);
            _resultList.EndUpdate();
            _resultList.SelectedIndex = 0;
            UpdateServersHeader();
            SetStatus($"Loaded {entries.Count} entries.", Theme.Success);

            if (_rememberCheckbox.Checked)
            {
                _settings.AddRecent(url);
                RefreshRecentUrls();
                _urlCombo.Text = url;
            }
        }
        catch (OperationCanceledException) { SetStatus("Cancelled.", Theme.TextMuted); }
        catch (Exception ex) { SetStatus("Error: " + ex.Message, Theme.Error); }
        finally { _fetchButton.Enabled = true; }
    }

    private void UpdateServersHeader()
    {
        var n = _resultList.Items.Count;
        _serversHeader.Text = n == 0 ? "Servers" : $"Servers ({n})";
    }

    private void UpdateConfigView()
    {
        if (_resultList.SelectedItem is VpnEntry e)
        {
            _configBox.Text = ($"# {e.Remarks}{Environment.NewLine}{Environment.NewLine}{e.ConfigJson}").Replace("\n", Environment.NewLine);
        }
        else
        {
            _configBox.Text = "";
        }
    }

    private List<VpnEntry> GetTargetEntries(out bool wasSelection)
    {
        var selected = _resultList.SelectedItems.Cast<VpnEntry>().ToList();
        if (selected.Count > 0) { wasSelection = true; return selected; }
        wasSelection = false;
        return _resultList.Items.Cast<VpnEntry>().ToList();
    }

    private void CopyLinks()
    {
        var entries = GetTargetEntries(out var wasSelection);
        if (entries.Count == 0) { SetStatus("Nothing to copy.", Theme.Error); return; }
        var text = string.Join(Environment.NewLine, entries.Select(e => e.VlessUri));
        if (!TrySetClipboard(text)) return;
        SetStatus($"Copied {entries.Count} link(s){(wasSelection ? " (selected)" : "")}.", Theme.Success);
    }

    private bool TrySetClipboard(string text)
    {
        try { Clipboard.SetText(text); return true; }
        catch (Exception ex) { SetStatus("Clipboard error: " + ex.Message, Theme.Error); return false; }
    }

    private void SaveLinks()
    {
        var entries = GetTargetEntries(out var wasSelection);
        if (entries.Count == 0) { SetStatus("Nothing to save.", Theme.Error); return; }

        using var dlg = new SaveFileDialog
        {
            Title = "Save VLESS links",
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = wasSelection ? "vless-selected.txt" : "vless-links.txt",
            DefaultExt = "txt",
            AddExtension = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var text = string.Join(Environment.NewLine, entries.Select(e => e.VlessUri));
            File.WriteAllText(dlg.FileName, text + Environment.NewLine, new UTF8Encoding(false));
            SetStatus($"Saved {entries.Count} link(s) to {Path.GetFileName(dlg.FileName)}.", Theme.Success);
        }
        catch (Exception ex) { SetStatus("Save failed: " + ex.Message, Theme.Error); }
    }

    private void SaveConfigs()
    {
        var entries = GetTargetEntries(out var wasSelection);
        if (entries.Count == 0) { SetStatus("Nothing to save.", Theme.Error); return; }

        using var dlg = new SaveFileDialog
        {
            Title = "Save XRay configs",
            Filter = "JSON file (*.json)|*.json|All files (*.*)|*.*",
            FileName = wasSelection ? "configs-selected.json" : "configs.json",
            DefaultExt = "json",
            AddExtension = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (var i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.AppendLine();
                sb.Append(IndentJson(entries[i].ConfigJson, 2));
            }
            sb.AppendLine();
            sb.Append(']');
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(false));
            SetStatus($"Saved {entries.Count} config(s) to {Path.GetFileName(dlg.FileName)}.", Theme.Success);
        }
        catch (Exception ex) { SetStatus("Save failed: " + ex.Message, Theme.Error); }
    }

    private static string IndentJson(string json, int spaces)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return SubscriptionFetcher.PrettyPrint(doc.RootElement);
        }
        catch
        {
            return json;
        }
    }

    private void CopySelectedConfig()
    {
        if (_resultList.SelectedItem is VpnEntry e)
        {
            if (TrySetClipboard(e.ConfigJson))
                SetStatus("Copied selected config JSON.", Theme.Success);
        }
        else
        {
            SetStatus("Select an entry first.", Theme.Error);
        }
    }

    private void SetStatus(string s, Color color)
    {
        _statusLabel.Text = s;
        _statusLabel.ForeColor = color;
    }
}
