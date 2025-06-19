// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnsClient.Internal;
using DnsClient.Windows;

namespace DnsClient
{
    /// <summary>
    /// Represents a name server instance used by <see cref="ILookupClient"/>.
    /// Also, comes with some static methods to resolve name servers from the local network configuration.
    /// </summary>
    public sealed class NameServer : IEquatable<NameServer>
    {
        /// <summary>Default DNS server port.</summary>
        public const int DefaultPort = 53;

        /// <summary>A public Google DNS IPv4 nameserver.</summary>
        public static readonly NameServer GooglePublicDns = new IPEndPoint(IPAddress.Parse("8.8.4.4"), DefaultPort);

        /// <summary>A second public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDns2 = new IPEndPoint(IPAddress.Parse("8.8.8.8"), DefaultPort);

        /// <summary>A public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDnsIPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8844"), DefaultPort);

        /// <summary>A second public Google DNS IPv6 nameserver.</summary>
        public static readonly NameServer GooglePublicDns2IPv6 = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), DefaultPort);

        /// <summary>A public Cloudflare DNS nameserver.</summary>
        public static readonly NameServer Cloudflare = new IPEndPoint(IPAddress.Parse("1.1.1.1"), DefaultPort);

        /// <summary>A public Cloudflare DNS nameserver.</summary>
        public static readonly NameServer Cloudflare2 = new IPEndPoint(IPAddress.Parse("1.0.0.1"), DefaultPort);

        /// <summary>A public Cloudflare DNS IPv6 nameserver.</summary>
        public static readonly NameServer CloudflareIPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1111"), DefaultPort);

        /// <summary>A public Cloudflare DNS IPv6 nameserver.</summary>
        public static readonly NameServer Cloudflare2IPv6 = new IPEndPoint(IPAddress.Parse("2606:4700:4700::1001"), DefaultPort);

        internal const string EtcResolvConfFile = "/etc/resolv.conf";

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint) : this(new IPEndPoint(endPoint, DefaultPort))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, int port) : this(new IPEndPoint(endPoint, port))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPEndPoint endPoint)
        {
            IPEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, string dnsSuffix) : this(new IPEndPoint(endPoint, DefaultPort), dnsSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, int port, string dnsSuffix) : this(new IPEndPoint(endPoint, port), dnsSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPEndPoint endPoint, string? dnsSuffix) : this(endPoint)
        {
            this.DnsSuffix = string.IsNullOrWhiteSpace(dnsSuffix) ? null : dnsSuffix;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        public static implicit operator NameServer(IPEndPoint endPoint)
            => ToNameServer(endPoint);

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">The address.</param>
        public static implicit operator NameServer(IPAddress address)
            => ToNameServer(address);

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        public static NameServer? ToNameServer(IPEndPoint endPoint)
        {
            if (endPoint is null) return null;
            return new NameServer(endPoint);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="endPoint">The address.</param>
        public static NameServer? ToNameServer(IPAddress endPoint)
        {
            if (endPoint is null) return null;
            return new NameServer(endPoint);
        }

        /// <summary>
        /// Gets the string representation of the configured <see cref="IPAddress"/>.
        /// </summary>
        public string Address => this.IPEndPoint.Address.ToString();

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port => this.IPEndPoint.Port;

        /// <summary>
        /// Gets the address family.
        /// </summary>
        public AddressFamily AddressFamily => this.IPEndPoint.AddressFamily;

        /// <summary>
        /// Gets the size of the supported UDP payload.
        /// <para>
        /// This value might get updated by <see cref="ILookupClient"/> by reading the options records returned by a query.
        /// </para>
        /// </summary>
        /// <value>
        /// The size of the supported UDP payload.
        /// </value>
        public int? SupportedUdpPayloadSize { get; internal set; }

        internal IPEndPoint IPEndPoint { get; }

        /// <summary>
        /// Gets an optional DNS suffix which a resolver can use to append to queries or to find servers suitable for a query.
        /// </summary>
        public string? DnsSuffix { get; }

        public bool IsValid => !this.IPEndPoint.Address.Equals(IPAddress.Any) && !this.IPEndPoint.Address.Equals(IPAddress.IPv6Any);

        internal static NameServer[]? Convert(IReadOnlyCollection<IPAddress> addresses) => addresses?.Select(p => (NameServer)p).ToArray();

        internal static NameServer[]? Convert(IReadOnlyCollection<IPEndPoint> addresses) => addresses?.Select(p => (NameServer)p).ToArray();

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{this.IPEndPoint}{(this.DnsSuffix is not null ? $"|{this.DnsSuffix}" : string.Empty)}";

        /// <inheritdocs />
        public override bool Equals(object? obj) => obj is NameServer ns && this.Equals(ns);

        /// <inheritdocs />
        public bool Equals(NameServer? other) => other is not null && this.IPEndPoint == other.IPEndPoint && this.DnsSuffix == other.DnsSuffix;

        /// <inheritdocs />
        public override int GetHashCode() => this.IPEndPoint.GetHashCode();

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// <para>
        /// If <paramref name="fallbackToGooglePublicDns" /> is enabled, this method will return the Google public DNS endpoints if no
        /// local DNS server was found.
        /// </para>
        /// </summary>
        /// <param name="skipIPv6SiteLocal">If set to <c>true</c> local IPv6 sites are skipped.</param>
        /// <param name="fallbackToGooglePublicDns">If set to <c>true</c> the public Google DNS servers are returned if no other servers could be found.</param>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IEnumerable<NameServer> ResolveNameServers(bool skipIPv6SiteLocal = true, bool fallbackToGooglePublicDns = true)
        {
            var nameServers = new List<NameServer>();
            var exceptions = new List<Exception>();
            var logger = Logging.LoggerFactory?.CreateLogger("DnsClient.NameServer");

            logger?.LogDebug("Starting to resolve name servers (skipIPv6SiteLocal: {0} | fallbackToGooglePublicDns: {1})", skipIPv6SiteLocal, fallbackToGooglePublicDns);

            try
            {
                logger?.LogDebug("Resolving name servers using .NET framework.");
                nameServers.AddRange(ResolveNameServersNet());
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Resolving name servers using .NET framework failed.");
                exceptions.Add(ex);
            }

            try
            {
                logger?.LogDebug("Resolving name servers using native implementation.");
                nameServers.AddRange(ResolveNameServersNative());
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Resolving name servers using native implementation failed.");
                exceptions.Add(ex);
            }

            try
            {
                logger?.LogDebug("Resolving name servers from NRPT.");
                nameServers.AddRange(ResolveNameServersNrpt());
            }
            catch (Exception ex)
            {
                // Ignore the exception. (AG NOTE: Added to exceptions anyway)
                // Turns out this can happen in Azure Functions. See #133
                // Turns out it can cause more errors, See #162, #149
                logger?.LogWarning(ex, "Resolving name servers from NRPT failed.");
                exceptions.Add(ex);
            }

            try
            {
                logger?.LogDebug("Validating name servers.");
                nameServers.RemoveAll(ns => !ns.IsValid || (skipIPv6SiteLocal && ns.IPEndPoint.Address.IsIPv6SiteLocal));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Validating name servers failed");
                exceptions.Add(ex);
            }

            if (nameServers.Count == 0)
            {
                if (!fallbackToGooglePublicDns && exceptions.Count > 0)
                {
                    logger?.LogError("Could not resolve any name servers.");
                    if (logger is not null)
                    {
                        for (var index = 0; index < exceptions.Count; index++)
                        {
                            logger.LogError(exceptions[index], "Exception #{index}/{count}", index + 1, exceptions.Count);
                        }
                    }
                    throw new AggregateException("Could not resolve any name servers", exceptions);
                }
                else if (fallbackToGooglePublicDns)
                {
                    logger?.LogWarning("Could not resolve any NameServers, falling back to Google public servers.");
                    nameServers.AddRange([GooglePublicDns, GooglePublicDns2, GooglePublicDnsIPv6, GooglePublicDns2IPv6]);
                }
            }

            logger?.LogDebug("Resolved {0} name servers: [{1}].", nameServers.Count, string.Join(",", nameServers.AsEnumerable()));
            return nameServers;
        }

        /// <summary>
        /// Using my custom native implementation to support UWP apps and such until <see cref="NetworkInterface.GetAllNetworkInterfaces"/>
        /// gets an implementation in netstandard2.1.
        /// </summary>
        /// <remarks>
        /// DnsClient has been changed in version 1.1.0.
        /// It will not invoke this when resolving default DNS servers. It is up to the user to decide what to do based on what platform the code is running on.
        /// </remarks>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IEnumerable<NameServer> ResolveNameServersNative()
        {
            if (OperatingSystem.IsWindows())
            {
                var fixedInfo = Windows.IpHlpApi.FixedNetworkInformation.GetFixedInformation();
                return fixedInfo.DnsAddresses.Select(address => new NameServer(address, DefaultPort, fixedInfo.DomainName));
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return Linux.StringParsingHelpers.ParseDnsAddressesFromResolvConfFile(EtcResolvConfFile);
            }
            else
            {
                throw new NotSupportedException($"Operating System not recognized: {Environment.OSVersion}");
            }
        }

        /// <summary>
        /// On a Windows machine query the Name Resolution Policy table for a list of policy-defined name servers.
        /// </summary>
        /// <returns>Returns a collection of name servers from the policy table</returns>
        public static IEnumerable<NameServer> ResolveNameServersNrpt()
        {
            return NameResolutionPolicy.Resolve();
        }

        public static IEnumerable<NameServer> ResolveNameServersNet()
        {
            var adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(p => p.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(p => p.OperationalStatus is OperationalStatus.Up or OperationalStatus.Unknown);

            foreach (var adapter in adapters)
            {
                var properties = adapter?.GetIPProperties();

                // Can be null under mono for whatever reason...
                if (properties?.DnsAddresses is null) continue;

                foreach (var ip in properties.DnsAddresses)
                {
                    yield return new NameServer(ip, DefaultPort, properties.DnsSuffix);
                }
            }
        }
    }
}
