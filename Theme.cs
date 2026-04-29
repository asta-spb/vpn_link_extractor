using System.Drawing;

namespace VpnLinkExtractor;

public static class Theme
{
    public static readonly Color Background   = Color.FromArgb(0x16, 0x1B, 0x23);
    public static readonly Color Surface      = Color.FromArgb(0x1F, 0x26, 0x31);
    public static readonly Color SurfaceAlt   = Color.FromArgb(0x28, 0x30, 0x3D);
    public static readonly Color Border       = Color.FromArgb(0x37, 0x41, 0x51);
    public static readonly Color Text         = Color.FromArgb(0xE5, 0xE7, 0xEB);
    public static readonly Color TextMuted    = Color.FromArgb(0x9C, 0xA3, 0xAF);
    public static readonly Color Accent       = Color.FromArgb(0x7C, 0x3A, 0xED);
    public static readonly Color AccentHover  = Color.FromArgb(0x8B, 0x5C, 0xF6);
    public static readonly Color Success      = Color.FromArgb(0x22, 0xC5, 0x5E);
    public static readonly Color Error        = Color.FromArgb(0xEF, 0x44, 0x44);
    public static readonly Color LinkColor    = Color.FromArgb(0x60, 0xA5, 0xFA);

    public static readonly Font Sans     = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font SansBold = new("Segoe UI Semibold", 10.5F, FontStyle.Regular);
    public static readonly Font Title    = new("Segoe UI Semibold", 13F, FontStyle.Regular);
    public static readonly Font Mono     = new("Cascadia Mono", 9F, FontStyle.Regular);
}
