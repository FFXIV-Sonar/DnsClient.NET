// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Windows;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class NameServerTest
    {
        [Fact]
        public void NativeDnsServerResolution()
        {
            var result = NameServer.ResolveNameServersNative();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void AnyAddressIPv4IsInvalid()
        {
            Assert.False(new NameServer(IPAddress.Any).IsValid);
        }

        [Fact]
        public void AnyAddressIPv6IsInvalid()
        {
            Assert.False(new NameServer(IPAddress.IPv6Any).IsValid);
        }

        [Fact]
        public void ValidateAnyAddress_LookupClientInit()
        {
            Assert.Throws<InvalidOperationException>(() => new LookupClient(IPAddress.Any));
            Assert.Throws<InvalidOperationException>(() => new LookupClient(IPAddress.Any, 33));
            Assert.Throws<InvalidOperationException>(() => new LookupClient(IPAddress.IPv6Any));
            Assert.Throws<InvalidOperationException>(() => new LookupClient(IPAddress.IPv6Any, 555));
        }

        [Fact]
        public void ValidateAnyAddress_LookupClientQuery()
        {
            var client = new LookupClient(NameServer.CloudflarePublicDns);

            Assert.Throws<InvalidOperationException>(() => client.QueryServer(new[] { IPAddress.Any }, "query", QueryType.A));
            Assert.Throws<InvalidOperationException>(() => client.QueryServerReverse(new[] { IPAddress.Any }, IPAddress.Loopback));
        }

        [Fact]
        public async Task ValidateAnyAddress_LookupClientQueryAsync()
        {
            var client = new LookupClient(NameServer.CloudflarePublicDns);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.QueryServerAsync(new[] { IPAddress.Any }, "query", QueryType.A));
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.QueryServerReverseAsync(new[] { IPAddress.Any }, IPAddress.Loopback));
        }

        [Fact]
        public void ValidateNameResolutionPolicyDoesntThrowNormally()
        {
            var ex = Record.Exception(() => NameResolutionPolicy.Resolve());

            Assert.Null(ex);
        }
    }
}
