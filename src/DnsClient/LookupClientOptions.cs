using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    /// <summary>
    /// The options used to configure defaults in <see cref="LookupClient"/> and to optionally use specific settings per query.
    /// </summary>
    public class LookupClientOptions : DnsQueryAndServerOptions
    {
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;

        // max is 24 days
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);

        private TimeSpan? _minimumCacheTimeout;
        private TimeSpan? _maximumCacheTimeout;

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/> with default settings.
        /// </summary>
        public LookupClientOptions() : base()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="LookupClientOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public LookupClientOptions(params NameServer[] nameServers) : base(nameServers)
        {
            this.AutoResolveNameServers = false;
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the name server collection should be automatically resolved.
        /// Default is <c>True</c>.
        /// </summary>
        /// <remarks>
        /// If name servers are configured manually via the constructor, this flag is set to false.
        /// If you want both, your manually configured servers and auto resolved name servers,
        /// you can use both (ctor or) <see cref="DnsQueryAndServerOptions.NameServers"/> and <see cref="AutoResolveNameServers"/> set to <c>True</c>.
        /// </remarks>
        public bool AutoResolveNameServers { get; init; } = true;

        /// <summary>
        /// Gets or sets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is lower than this minimum value.
        /// Default is <c>Null</c>.
        /// <para>
        /// This is useful in case the server returns records with zero TTL.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>,
        /// or the value is set to <c>Null</c> or <see cref="TimeSpan.Zero"/>.
        /// The maximum value is 24 days or <see cref="Timeout.Infinite"/> (choose a wise setting).
        /// </remarks>
        public TimeSpan? MinimumCacheTimeout
        {
            get => this._minimumCacheTimeout;
            init
            {
                if (value.HasValue && (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == TimeSpan.Zero)
                {
                    this._minimumCacheTimeout = null;
                }
                else
                {
                    this._minimumCacheTimeout = value;
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="TimeSpan"/> which can override the TTL of a resource record in case the
        /// TTL of the record is higher than this maximum value.
        /// Default is <c>Null</c>.
        /// </summary>
        /// <remarks>
        /// This setting gets ignored in case <see cref="DnsQueryOptions.UseCache"/> is set to <c>False</c>,
        /// or the value is set to <c>Null</c>, <see cref="Timeout.Infinite"/> or <see cref="TimeSpan.Zero"/>.
        /// The maximum value is 24 days (which shouldn't be used).
        /// </remarks>
        public TimeSpan? MaximumCacheTimeout
        {
            get => this._maximumCacheTimeout;
            init
            {
                if (value.HasValue && (value < TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == TimeSpan.Zero)
                {
                    this._maximumCacheTimeout = null;
                }
                else
                {
                    this._maximumCacheTimeout = value;
                }
            }
        }
    }
}
