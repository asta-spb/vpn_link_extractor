using System;
using System.Drawing;
using System.Windows.Forms;

namespace VpnLinkExtractor;

public sealed class FlatBtn : Button
{
    private bool _primary;

    protected override bool ShowFocusCues => false;

    public FlatBtn()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 1;
        Cursor = Cursors.Hand;
        Font = Theme.Sans;
        Height = 32;
        Margin = new Padding(0, 0, 8, 0);
        Padding = new Padding(14, 0, 14, 0);
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleCenter;
        UseCompatibleTextRendering = false;
        TabStop = true;
        ApplySecondary();
    }

    public void ApplyPrimary()
    {
        _primary = true;
        BackColor = Theme.Accent;
        ForeColor = Color.White;
        FlatAppearance.BorderColor = Theme.Accent;
        FlatAppearance.MouseOverBackColor = Theme.AccentHover;
        FlatAppearance.MouseDownBackColor = Theme.AccentHover;
    }

    public void ApplySecondary()
    {
        _primary = false;
        BackColor = Theme.Surface;
        ForeColor = Theme.Text;
        FlatAppearance.BorderColor = Theme.Border;
        FlatAppearance.MouseOverBackColor = Theme.SurfaceAlt;
        FlatAppearance.MouseDownBackColor = Theme.SurfaceAlt;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (!Enabled)
        {
            ForeColor = Theme.TextMuted;
        }
        else if (_primary)
        {
            ForeColor = Color.White;
        }
        else
        {
            ForeColor = Theme.Text;
        }
    }
}
