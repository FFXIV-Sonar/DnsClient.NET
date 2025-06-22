// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using DnsClient.Protocol;

namespace DnsClient
{
    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQueryOptions
    {
        /// <summary>
        /// The minimum payload size. Anything equal or less than that will default back to this value and might disable EDNS.
        /// </summary>
        public const int MinimumBufferSize = 512;

        /// <summary>
        /// The maximum reasonable payload size.
        /// </summary>
        public const int MaximumBufferSize = 4096;

        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private TimeSpan _timeout = s_defaultTimeout;
        private int _ednsBufferSize = MaximumBufferSize;
        private TimeSpan _failedResultsCacheDuration = s_defaultTimeout;

        /// <summary>
        /// Gets or sets a flag indicating whether each <see cref="IDnsQueryResponse"/> will contain a full documentation of the response(s).
        /// Default is <c>False</c>.
        /// </summary>
        /// <seealso cref="IDnsQueryResponse.AuditTrail"/>
        public bool EnableAuditTrail { get; init; }

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should use response caching or not.
        /// The cache duration is calculated by the resource record of the response. Usually, the lowest TTL is used.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// In case the DNS Server returns records with a TTL of zero. The response cannot be cached.
        /// </remarks>
        public bool UseCache { get; init; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether DNS queries should instruct the DNS server to do recursive lookups, or not.
        /// Default is <c>True</c>.
        /// </summary>
        /// <value>The flag indicating if recursion should be used or not.</value>
        public bool Recursion { get; init; } = true;

        /// <summary>
        /// Gets or sets the number of tries to get a response from one name server before trying the next one.
        /// Only transient errors, like network or connection errors will be retried.
        /// Default is <c>2</c> which will be three tries total.
        /// <para>
        /// If all configured <see cref="DnsQueryAndServerOptions.NameServers"/> error out after retries, an exception will be thrown at the end.
        /// </para>
        /// </summary>
        /// <value>The number of retries.</value>
        public int Retries { get; init; } = 2;

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> should throw a <see cref="DnsResponseException"/>
        /// in case the query result has a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>False</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to <c>False</c>, the query will return a result with an <see cref="IDnsQueryResponse.ErrorMessage"/>
        /// which contains more information.
        /// </para>
        /// <para>
        /// If set to <c>True</c>, any query method of <see cref="IDnsQuery"/> will throw an <see cref="DnsResponseException"/> if
        /// the response header indicates an error.
        /// </para>
        /// <para>
        /// If both, <see cref="ContinueOnDnsError"/> and <see cref="ThrowDnsErrors"/> are set to <c>True</c>,
        /// <see cref="ILookupClient"/> will continue to query all configured <see cref="DnsQueryAndServerOptions.NameServers"/>.
        /// If none of the servers yield a valid response, a <see cref="DnsResponseException"/> will be thrown
        /// with the error of the last response.
        /// </para>
        /// </remarks>
        /// <seealso cref="DnsResponseCode"/>
        /// <seealso cref="ContinueOnDnsError"/>
        public bool ThrowDnsErrors { get; init; }

        /// <summary>
        /// Gets or sets a flag indicating whether the <see cref="ILookupClient"/> can cycle through all
        /// configured <see cref="DnsQueryAndServerOptions.NameServers"/> on each consecutive request, basically using a random server, or not.
        /// Default is <c>True</c>.
        /// If only one <see cref="NameServer"/> is configured, this setting is not used.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <c>False</c>, configured endpoint will be used in random order.
        /// If <c>True</c>, the order will be preserved.
        /// </para>
        /// <para>
        /// Even if <see cref="UseRandomNameServer"/> is set to <c>True</c>, the endpoint might still get
        /// disabled and might not being used for some time if it errors out, e.g. no connection can be established.
        /// </para>
        /// </remarks>
        public bool UseRandomNameServer { get; init; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerOptions.NameServers"/> in case the response of the last query
        /// returned a <see cref="DnsResponseCode"/> other than <see cref="DnsResponseCode.NoError"/>.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If <c>True</c>, lookup client will continue until a server returns a valid result, or,
        /// if no <see cref="DnsQueryAndServerOptions.NameServers"/> yield a valid result, the last response with the error will be returned.
        /// In case no server yields a valid result and <see cref="ThrowDnsErrors"/> is also enabled, an exception
        /// will be thrown containing the error of the last response.
        /// <para>
        /// If  <c>True</c> and <see cref="ThrowDnsErrors"/> is enabled, the exception will be thrown on first encounter without trying any other servers.
        /// </para>
        /// </remarks>
        /// <seealso cref="ThrowDnsErrors"/>
        public bool ContinueOnDnsError { get; init; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether to query the next configured <see cref="DnsQueryAndServerOptions.NameServers"/>
        /// if the response does not have an error <see cref="DnsResponseCode"/> but the query was not answered by the response.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// The query is answered if there is at least one <see cref="DnsResourceRecord"/> in the answers section
        /// matching the <see cref="DnsQuestion"/>'s <see cref="QueryType"/>.
        /// <para>
        /// If there are zero answers in the response, the query is not answered, independent of the <see cref="QueryType"/>.
        /// If there are answers in the response, the <see cref="QueryType"/> is used to find a matching record,
        /// query types <see cref="QueryType.ANY"/> and <see cref="QueryType.AXFR"/> will be ignored by this check.
        /// </para>
        /// </remarks>
        public bool ContinueOnEmptyResponse { get; init; } = true;

        /// <summary>
        /// Gets or sets the request timeout in milliseconds. <see cref="Timeout"/> is used for limiting the connection and request time for one operation.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// If <see cref="Timeout.InfiniteTimeSpan"/> (or -1) is used, no timeout will be applied.
        /// Default is 5 seconds.
        /// </summary>
        /// <remarks>
        /// If a very short timeout is configured, queries will more likely result in <see cref="TimeoutException"/>s.
        /// <para>
        /// Important to note, <see cref="TimeoutException"/>s will be retried, if <see cref="Retries"/> are not disabled (set to <c>0</c>).
        /// This should help in case one or more configured DNS servers are not reachable or under load for example.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout
        {
            get { return this._timeout; }
            init
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                this._timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether TCP should be used in case a UDP response is truncated.
        /// Default is <c>True</c>.
        /// <para>
        /// If <c>False</c>, truncated results will potentially yield no or incomplete answers.
        /// </para>
        /// </summary>
        public bool UseTcpFallback { get; init; } = true;

        /// <summary>
        /// Gets or sets a flag indicating whether UDP should not be used at all.
        /// Default is <c>False</c>.
        /// <para>
        /// Enable this only if UDP cannot be used because of your firewall rules for example.
        /// Also, zone transfers (see <see cref="QueryType.AXFR"/>) must use TCP only.
        /// </para>
        /// </summary>
        public bool UseTcpOnly { get; init; }

        /// <summary>
        /// Gets a flag indicating whether EDNS is enabled based on the values
        /// of <see cref="ExtendedDnsBufferSize"/> and <see cref="RequestDnsSecRecords"/>.
        /// </summary>
        public bool UseExtendedDns => this.ExtendedDnsBufferSize > MinimumBufferSize || this.RequestDnsSecRecords;

        /// <summary>
        /// Gets or sets the maximum buffer used for UDP requests.
        /// Defaults to <c>4096</c>.
        /// <para>
        /// If this value is less or equal to <c>512</c> bytes, EDNS might be disabled.
        /// </para>
        /// </summary>
        public int ExtendedDnsBufferSize
        {
            get => this._ednsBufferSize;
            init => this._ednsBufferSize = Math.Clamp(value, MinimumBufferSize, MaximumBufferSize);
        }

        /// <summary>
        /// Gets or sets a flag indicating whether EDNS should be enabled and the <c>DO</c> flag should be set.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool RequestDnsSecRecords { get; init; }

        /// <summary>
        /// Gets or sets a flag indicating whether the DNS failures are being cached. The purpose of caching 
        /// failures is to reduce repeated lookup attempts within a short space of time.
        /// Defaults to <c>False</c>.
        /// </summary>
        public bool CacheFailedResults { get; init; }

        /// <summary>
        /// Gets or sets the duration to cache failed lookups. Does not apply if failed lookups are not being cached.
        /// Defaults to <c>5 seconds</c>.
        /// </summary>
        public TimeSpan FailedResultsCacheDuration
        {
            get { return this._failedResultsCacheDuration; }
            init
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                this._failedResultsCacheDuration = value;
            }
        }
    }
}
