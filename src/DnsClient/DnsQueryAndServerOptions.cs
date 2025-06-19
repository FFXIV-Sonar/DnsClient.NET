using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsClient
{
    /// <summary>
    /// The options used to override the defaults of <see cref="LookupClient"/> per query.
    /// </summary>
    public class DnsQueryAndServerOptions : DnsQueryOptions
    {
        internal readonly NameServer[] _nameServers;

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/> without name servers.
        /// If no nameservers are configured, a query will fallback to the nameservers already configured on the <see cref="LookupClient"/> instance.
        /// </summary>
        public DnsQueryAndServerOptions()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsQueryAndServerOptions"/>.
        /// </summary>
        /// <param name="nameServers">A collection of name servers.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="nameServers"/> is null.</exception>
        public DnsQueryAndServerOptions(params NameServer[] nameServers) : base()
        {
            this._nameServers = [.. nameServers];
        }

        /// <summary>
        /// Gets a list of name servers which should be used to query.
        /// </summary>
        public IEnumerable<NameServer> NameServers
        {
            get => this._nameServers;
            init => this._nameServers = [.. value];
        }

        [SuppressMessage("Security", "CA5394", Justification = "Not needed.")]
        internal NameServer[] ShuffleNameServers()
        {
            if (this.UseRandomNameServer && this._nameServers.Length > 1)
            {
                return [.. this._nameServers.OrderBy(ns => Random.Shared.Next())];
            }
            return this._nameServers;
        }
    }
}
