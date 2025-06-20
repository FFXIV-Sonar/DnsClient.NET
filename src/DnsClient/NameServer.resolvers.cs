using DnsClient.Internal;
using DnsClient.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DnsClient
{
    public partial class NameServer
    {
        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// <para>
        /// Will fallback to <paramref name="fallback" /> name servers if no local DNS server was found.
        /// </para>
        /// </summary>
        /// <param name="skipIPv6SiteLocal">If set to <c>true</c> local IPv6 sites are skipped.</param>
        /// <param name="fallback">Fallback DNS Servers.</param>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IEnumerable<NameServer> ResolveNameServers(bool skipIPv6SiteLocal = true, params NameServer[] fallback)
        {
            var nameServers = new List<NameServer>();
            var exceptions = new List<Exception>();
            var logger = Logging.LoggerFactory?.CreateLogger("DnsClient.NameServer");

            fallback ??= [];

            if (logger is not null)
            {
                var meta = new List<string>() { $"skipIPv6SiteLocal: {skipIPv6SiteLocal}" };
                if (fallback.Length > 0) meta.Add($"fallback: [{string.Join(", ", fallback.AsEnumerable())}]");
                logger.LogDebug("Starting to resolve name servers ({meta})", string.Join(" | ", meta));
            }

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

            var failed = nameServers.Count is 0;
            if (failed)
            {
                if (fallback.Length > 0)
                {
                    logger?.LogWarning("Could not resolve any NameServers, falling back to Google public servers.");
                    nameServers.AddRange(fallback);
                }
                else
                {
                    logger?.LogError("Could not resolve any name servers.");
                }

                if (logger is not null)
                {
                    var logLevel = fallback.Length is 0 ? LogLevel.Error : LogLevel.Warning;
                    for (var index = 0; index < exceptions.Count; index++)
                    {
                        logger.Log(logLevel, exceptions[index], "Exception #{0}/{1}", index + 1, exceptions.Count);
                    }
                }
            }

            if (nameServers.Count is 0) throw new AggregateException("Could not resolve any name servers", exceptions);
            logger?.LogDebug("Resolved {0} name servers: [{1}].", nameServers.Count, string.Join(", ", nameServers.AsEnumerable()));
            return nameServers;
        }

        /// <summary>
        /// Gets a list of name servers by iterating over the available network interfaces.
        /// <para>
        /// If <paramref name="fallbackToPublicDns" /> is enabled, this method will return the Google public DNS endpoints if no
        /// local DNS server was found.
        /// </para>
        /// </summary>
        /// <param name="skipIPv6SiteLocal">If set to <c>true</c> local IPv6 sites are skipped.</param>
        /// <param name="fallbackToPublicDns">If set to <c>true</c> the public Google DNS servers are returned if no other servers could be found.</param>
        /// <returns>
        /// The list of name servers.
        /// </returns>
        public static IEnumerable<NameServer> ResolveNameServers(bool skipIPv6SiteLocal = true, bool fallbackToPublicDns = true)
        {
            return ResolveNameServers(skipIPv6SiteLocal, fallbackToPublicDns ? (NameServer[])DefaultFallback : []);
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
