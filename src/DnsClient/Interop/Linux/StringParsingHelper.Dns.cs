// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System.Collections.Generic;
using System.IO;
using System.Net;

// TODO: Remove if fixed
// This code is from https://github.com/dotnet/corefx
// Will be removed whenever the bugs reading network information on Linux are fixed and
// I can use the Managed version.

namespace DnsClient.Linux
{
    internal static partial class StringParsingHelpers
    {
        internal static string ParseDnsSuffixFromResolvConfFile(string filePath)
        {
            var data = File.ReadAllText(filePath);
            var rcr = new RowConfigReader(data);
            return rcr.TryGetNextValue("search", out var dnsSuffix) ? dnsSuffix : string.Empty;
        }

        internal static IEnumerable<NameServer> ParseDnsAddressesFromResolvConfFile(string filePath)
        {
            // Parse /etc/resolv.conf for all of the "nameserver" entries.
            // These are the DNS servers the machine is configured to use.
            // On OSX, this file is not directly used by most processes for DNS
            // queries/routing, but it is automatically generated instead, with
            // the machine's DNS servers listed in it.
            var data = File.ReadAllText(filePath);
            var rcr = new RowConfigReader(data);
            var dnsSuffix = new RowConfigReader(data).TryGetNextValue("search", out var dnsSuffix) ? dnsSuffix : null;
            while (rcr.TryGetNextValue("nameserver", out var addressString))
            {
                if (IPAddress.TryParse(addressString, out var address))
                {
                    yield return new(address, NameServer.DefaultPort, dnsSuffix);
                }
            }
        }
    }
}
