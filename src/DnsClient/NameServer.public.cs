using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DnsClient
{
    public partial class NameServer
    {
        /// <summary>A public Google DNS IPv4 nameserver.</summary>
        public static readonly NameServer GooglePublicDns = new IPEndPoint(IPAddress.Parse("8.8.4.4"), DefaultPort);

        /// <summary>A second public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDns2 = new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort);

        /// <summary>A public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8844"), DefaultPort);

        /// <summary>A second public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDns2IPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), DefaultPort);

        /// <summary>A public Cloudflare DNS nameserver.</summary>
        public static readonly NameServer CloudflarePublicDns = new IPEndPoint(IPAddress.Parse("1.1.1.1"), DefaultPort);

        /// <summary>A public Cloudflare DNS nameserver.</summary>
        public static readonly NameServer CloudflarePublicDns2 = new IPEndPoint(IPAddress.Parse("1.0.0.1"), DefaultPort);

        /// <summary>A public Cloudflare DNS IPv6 nameserver.</summary>
        public static readonly NameServer CloudflarePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1111"), DefaultPort);

        /// <summary>A public Cloudflare DNS IPv6 nameserver.</summary>
        public static readonly NameServer CloudflarePublicDns2IPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1001"), DefaultPort);

        /// <summary>Default fallback nameservers.</summary>
        public static readonly IEnumerable<NameServer> DefaultFallback = new NameServer[]
        {
            GooglePublicDns, GooglePublicDns2, GooglePublicDnsIPv6, GooglePublicDns2IPv6,
            CloudflarePublicDns, CloudflarePublicDns2, CloudflarePublicDnsIPv6, CloudflarePublicDns2IPv6
        };
    }
}
