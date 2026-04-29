using System;

namespace VpnLinkExtractor;

public sealed record VpnEntry(string Remarks, string VlessUri, string ConfigJson)
{
    public string Display
    {
        get
        {
            var hash = VlessUri.IndexOf('#');
            if (hash < 0) return VlessUri;
            return VlessUri[..(hash + 1)] + Uri.UnescapeDataString(VlessUri[(hash + 1)..]);
        }
    }
}
