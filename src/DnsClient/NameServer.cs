// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using DnsClient.Internal;
using DnsClient.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DnsClient
{
    /// <summary>
    /// Represents a name server instance used by <see cref="ILookupClient"/>.
    /// Also, comes with some static methods to resolve name servers from the local network configuration.
    /// </summary>
    public sealed partial class NameServer : IEquatable<NameServer>
    {
        /// <summary>Default DNS server port.</summary>
        public const int DefaultPort = 53;

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
            this.IPEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, string? dnsSuffix) : this(new IPEndPoint(endPoint, DefaultPort), dnsSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class.
        /// </summary>
        /// <param name="endPoint">The name server endpoint.</param>
        /// <param name="port">The name server port.</param>
        /// <param name="dnsSuffix">An optional DNS suffix (can be null).</param>
        /// <exception cref="ArgumentNullException">If <paramref name="endPoint"/>is <c>null</c>.</exception>
        public NameServer(IPAddress endPoint, int port, string? dnsSuffix) : this(new IPEndPoint(endPoint, port), dnsSuffix)
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
        public static implicit operator NameServer(IPEndPoint endPoint) => ToNameServer(endPoint);

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">The address.</param>
        public static implicit operator NameServer(IPAddress address) => ToNameServer(address);

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="endPoint">The endpoint.</param>
        [return: NotNullIfNotNull(nameof(endPoint))]
        public static NameServer? ToNameServer(IPEndPoint? endPoint)
        {
            if (endPoint is null) return null;
            return new NameServer(endPoint);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameServer"/> class from a <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="endPoint">The address.</param>
        [return: NotNullIfNotNull(nameof(endPoint))]
        public static NameServer? ToNameServer(IPAddress? endPoint)
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

        /// <summary>
        /// Gets a value indicating this <see cref="NameServer"/> is valid.
        /// </summary>
        public bool IsValid => !this.IPEndPoint.Address.Equals(IPAddress.Any) && !this.IPEndPoint.Address.Equals(IPAddress.IPv6Any);

        /// <summary>
        /// Convert <paramref name="addresses"/> into <see cref="NameServer"/>s.
        /// </summary>
        /// <param name="addresses">Addresses to convert.</param>
        /// <returns><paramref name="addresses"/> converted to <see cref="NameServer"/>s.</returns>
        [return: NotNullIfNotNull(nameof(addresses))]
        public static IEnumerable<NameServer>? Convert(IEnumerable<IPAddress>? addresses) => addresses?.Select(p => (NameServer)p);

        /// <summary>
        /// Convert <paramref name="addresses"/> into <see cref="NameServer"/>s.
        /// </summary>
        /// <param name="addresses">Addresses to convert.</param>
        /// <returns><paramref name="addresses"/> converted to <see cref="NameServer"/>s.</returns>
        [return: NotNullIfNotNull(nameof(addresses))]
        public static IEnumerable<NameServer>? Convert(IEnumerable<IPEndPoint>? addresses) => addresses?.Select(p => (NameServer)p);

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
    }
}
