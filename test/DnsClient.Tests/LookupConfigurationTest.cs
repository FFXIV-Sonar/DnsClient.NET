// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class LookupConfigurationTest
    {
        static LookupConfigurationTest()
        {
            Tracing.Source.Switch.Level = System.Diagnostics.SourceLevels.All;
        }

        /*
        [Fact]
        public void DnsQuerySettings_Equals_Other()
        {
            var opt = new DnsQueryOptions();
            var opt2 = new DnsQueryOptions();

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        [Fact]
        public void DnsQueryAndServerSettings_Equals_Other()
        {
            var opt = new DnsQueryAndServerOptions();
            var opt2 = new DnsQueryAndServerOptions();

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }

        [Fact]
        public void LookupClientSettings_Equals_Other()
        {
            var opt = new LookupClientOptions();
            var opt2 = new LookupClientOptions();

            // typed overload
            Assert.True(opt.Equals(opt2));

            // object overload
            Assert.True(opt.Equals((object)opt2));
        }
        */

        [Fact]
        public void DnsQueryAndServerSettings_ChangeServers()
        {
            var settings = new DnsQueryAndServerOptions(NameServer.CloudflarePublicDns);

            Assert.Single(settings.NameServers);
            Assert.Contains(NameServer.CloudflarePublicDns, settings.NameServers);
        }

        [Fact]
        public void DnsQueryAndServerSettings_NullServers()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => new DnsQueryAndServerOptions((NameServer[])null));
        }

        public static TheoryData<LookupClientOptions> AllNonDefaultConfigurations
        {
            get => new TheoryData<LookupClientOptions>(
                new LookupClientOptions { AutoResolveNameServers = false, ContinueOnDnsError = false },
                new LookupClientOptions { AutoResolveNameServers = false, EnableAuditTrail = true },
                new LookupClientOptions { AutoResolveNameServers = false, ExtendedDnsBufferSize = 2222 },
                new LookupClientOptions { AutoResolveNameServers = false, MaximumCacheTimeout = TimeSpan.FromSeconds(5) },
                new LookupClientOptions { AutoResolveNameServers = false, MinimumCacheTimeout = TimeSpan.FromSeconds(5) },
                new LookupClientOptions { AutoResolveNameServers = false, Recursion = false },
                new LookupClientOptions { AutoResolveNameServers = false, RequestDnsSecRecords = true },
                new LookupClientOptions { AutoResolveNameServers = false, Retries = 3 },
                new LookupClientOptions { AutoResolveNameServers = false, ThrowDnsErrors = true },
                new LookupClientOptions { AutoResolveNameServers = false, Timeout = TimeSpan.FromSeconds(1) },
                new LookupClientOptions { AutoResolveNameServers = false, UseCache = false },
                new LookupClientOptions { AutoResolveNameServers = false, UseRandomNameServer = false },
                new LookupClientOptions { AutoResolveNameServers = false, UseTcpFallback = false },
                new LookupClientOptions { AutoResolveNameServers = false, UseTcpOnly = true },
                new LookupClientOptions(NameServer.CloudflarePublicDns2),
                new LookupClientOptions(IPAddress.Loopback),
                new LookupClientOptions(new IPEndPoint(IPAddress.Loopback, 1111)),
                new LookupClientOptions(NameServer.CloudflarePublicDnsIPv6));
        }

        [Theory]
        [MemberData(nameof(AllNonDefaultConfigurations))]
        public void LookupClientSettings_NotEqual(LookupClientOptions otherOptions)
        {
            var opt = new LookupClientOptions() { AutoResolveNameServers = false };
            var opt2 = otherOptions;

            Assert.NotStrictEqual(opt, opt2);

            // typed overload
            Assert.False(opt.Equals(opt2));

            // object overload
            Assert.False(opt.Equals((object)opt2));
        }

        [Fact]
        public void LookupClientOptions_Defaults()
        {
            var options = new LookupClientOptions();

            Assert.Empty(options.NameServers);
            Assert.True(options.AutoResolveNameServers);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.Null(options.MaximumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(2, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.ContinueOnEmptyResponse);
            Assert.True(options.UseRandomNameServer);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
            Assert.False(options.RequestDnsSecRecords);
            Assert.False(options.CacheFailedResults);
            Assert.Equal(options.FailedResultsCacheDuration, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void LookupClientOptions_DefaultsNoResolve()
        {
            var options = new LookupClientOptions() { AutoResolveNameServers = false };

            Assert.Empty(options.NameServers);
            Assert.False(options.AutoResolveNameServers);
            Assert.True(options.UseCache);
            Assert.False(options.EnableAuditTrail);
            Assert.Null(options.MinimumCacheTimeout);
            Assert.Null(options.MaximumCacheTimeout);
            Assert.True(options.Recursion);
            Assert.False(options.ThrowDnsErrors);
            Assert.Equal(2, options.Retries);
            Assert.Equal(options.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(options.UseTcpFallback);
            Assert.False(options.UseTcpOnly);
            Assert.True(options.ContinueOnDnsError);
            Assert.True(options.ContinueOnEmptyResponse);
            Assert.True(options.UseRandomNameServer);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
            Assert.False(options.RequestDnsSecRecords);
            Assert.False(options.CacheFailedResults);
            Assert.Equal(options.FailedResultsCacheDuration, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void LookupClient_SettingsValid()
        {
            var defaultOptions = new LookupClientOptions();

            var options = new LookupClientOptions
            {
                AutoResolveNameServers = !defaultOptions.AutoResolveNameServers,
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                ContinueOnEmptyResponse = !defaultOptions.ContinueOnEmptyResponse,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                MinimumCacheTimeout = TimeSpan.FromMinutes(1),
                MaximumCacheTimeout = TimeSpan.FromMinutes(42),
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly,
                ExtendedDnsBufferSize = 1234,
                RequestDnsSecRecords = true,
                CacheFailedResults = true,
                FailedResultsCacheDuration = TimeSpan.FromSeconds(10)
            };

            var client = new LookupClient(options);

            // Not resolving or setting any servers => should be empty.
            Assert.Empty(client.NameServers);
            Assert.Equal(!defaultOptions.ContinueOnDnsError, client.Options.ContinueOnDnsError);
            Assert.Equal(!defaultOptions.ContinueOnEmptyResponse, client.Options.ContinueOnEmptyResponse);
            Assert.Equal(!defaultOptions.EnableAuditTrail, client.Options.EnableAuditTrail);
            Assert.Equal(TimeSpan.FromMinutes(1), client.Options.MinimumCacheTimeout);
            Assert.Equal(TimeSpan.FromMinutes(42), client.Options.MaximumCacheTimeout);
            Assert.Equal(!defaultOptions.Recursion, client.Options.Recursion);
            Assert.Equal(defaultOptions.Retries * 2, client.Options.Retries);
            Assert.Equal(!defaultOptions.ThrowDnsErrors, client.Options.ThrowDnsErrors);
            Assert.Equal(TimeSpan.FromMinutes(10), client.Options.Timeout);
            Assert.Equal(!defaultOptions.UseCache, client.Options.UseCache);
            Assert.Equal(!defaultOptions.UseRandomNameServer, client.Options.UseRandomNameServer);
            Assert.Equal(!defaultOptions.UseTcpFallback, client.Options.UseTcpFallback);
            Assert.Equal(!defaultOptions.UseTcpOnly, client.Options.UseTcpOnly);
            Assert.Equal(1234, client.Options.ExtendedDnsBufferSize);
            Assert.Equal(!defaultOptions.RequestDnsSecRecords, client.Options.RequestDnsSecRecords);
            Assert.Equal(!defaultOptions.CacheFailedResults, client.Options.CacheFailedResults);
            Assert.Equal(TimeSpan.FromSeconds(10), client.Options.FailedResultsCacheDuration);

            Assert.Equal(options, client.Options);
        }

        [Fact]
        public void QueryOptions_EdnsDisabled_WithSmallBuffer()
        {
            var options = new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize,
                RequestDnsSecRecords = false
            };
            Assert.False(options.UseExtendedDns);
        }

        [Fact]
        public void QueryOptions_Edns_SmallerBufferFallback()
        {
            var options = new DnsQueryOptions()
            {
                // Anything less then minimum falls back to minimum.
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize - 1,
                RequestDnsSecRecords = false
            };
            Assert.False(options.UseExtendedDns);
            Assert.Equal(DnsQueryOptions.MinimumBufferSize, options.ExtendedDnsBufferSize);
        }

        [Fact]
        public void QueryOptions_Edns_LargerBufferFallback()
        {
            var options = new DnsQueryOptions()
            {
                // Anything more then max falls back to max.
                ExtendedDnsBufferSize = DnsQueryOptions.MaximumBufferSize + 1,
                RequestDnsSecRecords = false
            };

            Assert.True(options.UseExtendedDns);
            Assert.Equal(DnsQueryOptions.MaximumBufferSize, options.ExtendedDnsBufferSize);
        }

        [Fact]
        public void QueryOptions_EdnsEnabled_ByLargeBuffer()
        {
            var options = new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize + 1,
                RequestDnsSecRecords = false
            };

            Assert.True(options.UseExtendedDns);
        }

        [Fact]
        public void QueryOptions_EdnsEnabled_ByRequestDnsSec()
        {
            var options = new DnsQueryOptions()
            {
                ExtendedDnsBufferSize = DnsQueryOptions.MinimumBufferSize,
                RequestDnsSecRecords = true
            };

            Assert.True(options.UseExtendedDns);
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { Timeout = TimeSpan.FromMilliseconds(0) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { Timeout = TimeSpan.FromMilliseconds(-23) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidTimeout3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { Timeout = TimeSpan.FromDays(25) };
            });
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_ZeroIgnored()
        {
            var options = new LookupClientOptions()
            {
                MinimumCacheTimeout = TimeSpan.Zero
            };
            Assert.Null(options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_AcceptsInfinite()
        {
            var options = new LookupClientOptions()
            {
                MinimumCacheTimeout = Timeout.InfiniteTimeSpan
            };
            Assert.Equal(Timeout.InfiniteTimeSpan, options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MinimumCacheTimeout_AcceptsNull()
        {
            var options = new LookupClientOptions()
            {
                MinimumCacheTimeout = null
            };
            Assert.Null(options.MinimumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_InvalidMinimumCacheTimeout1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { MinimumCacheTimeout = TimeSpan.FromMilliseconds(-23) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidMinimumCacheTimeout2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { MinimumCacheTimeout = TimeSpan.FromDays(25) };
            });
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_ZeroIgnored()
        {
            var options = new LookupClientOptions()
            {
                MaximumCacheTimeout = TimeSpan.Zero
            };
            Assert.Null(options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_AcceptsInfinite()
        {
            var options = new LookupClientOptions()
            {
                MaximumCacheTimeout = Timeout.InfiniteTimeSpan
            };
            Assert.Equal(Timeout.InfiniteTimeSpan, options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_MaximumCacheTimeout_AcceptsNull()
        {
            var options = new LookupClientOptions()
            {
                MaximumCacheTimeout = null
            };
            Assert.Null(options.MaximumCacheTimeout);
        }

        [Fact]
        public void LookupClientOptions_InvalidMaximumCacheTimeout1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { MaximumCacheTimeout = TimeSpan.FromMilliseconds(-23) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidMaximumCacheTimeout2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { MaximumCacheTimeout = TimeSpan.FromDays(25) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidCacheFailureDuration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { FailedResultsCacheDuration = TimeSpan.FromMilliseconds(0) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidCacheFailureDuration2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { FailedResultsCacheDuration = TimeSpan.FromMilliseconds(-23) };
            });
        }

        [Fact]
        public void LookupClientOptions_InvalidCacheFailureDuration3()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                return new LookupClientOptions() { FailedResultsCacheDuration = TimeSpan.FromDays(25) };
            });
        }

        public static TheoryData<TestMatrixItem> All
        {
            get
            {
                // question doesn't matter
                var question = new DnsQuestion("something.com", QueryType.A);

                return new(
                    // standard
                    new TestMatrixItem("Query(q)", (client) => client.Query(question)),
                    new TestMatrixItem("Query(n,t,c)", (client) => client.Query(question.QueryName, question.QuestionType, question.QuestionClass)),
                    new TestMatrixItem("QueryAsync(q)", (client) => client.QueryAsync(question).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryAsync(n,t,c)", (client) => client.QueryAsync(question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryReverse(ip)", (client) => client.QueryReverse(IPAddress.Any)),
                    new TestMatrixItem("QueryReverseAsync(ip)", (client) => client.QueryReverseAsync(IPAddress.Any).GetAwaiter().GetResult()),

                    // by server
                    new TestMatrixItem("QueryServer(s,n,t,c)", (client, servers) => client.QueryServer(servers.ToList(), question.QueryName, question.QuestionType, question.QuestionClass)),
                    new TestMatrixItem("QueryServer(s2,n,t,c)", (client, servers) => client.QueryServer(ToIPAddresses(servers).ToList(), question.QueryName, question.QuestionType, question.QuestionClass)),
                    new TestMatrixItem("QueryServer(s3,n,t,c)", (client, servers) => client.QueryServer(ToIPEndpoints(servers).ToList(), question.QueryName, question.QuestionType, question.QuestionClass)),
                    new TestMatrixItem("QueryServer(s,q)", (client, servers) => client.QueryServer(servers.ToList(), question)),
                    new TestMatrixItem("QueryServerAsync(s,n,t,c)", (client, servers) => client.QueryServerAsync(servers.ToList(), question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerAsync(s2,n,t,c)", (client, servers) => client.QueryServerAsync(ToIPAddresses(servers).ToList(), question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerAsync(s3,n,t,c)", (client, servers) => client.QueryServerAsync(ToIPEndpoints(servers).ToList(), question.QueryName, question.QuestionType, question.QuestionClass).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerAsync(s,q)", (client, servers) => client.QueryServerAsync(servers.ToList(), question).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerReverse(s,ip)", (client, servers) => client.QueryServerReverse(servers.ToList(), IPAddress.Any)),
                    new TestMatrixItem("QueryServerReverse(s2,ip)", (client, servers) => client.QueryServerReverse(ToIPAddresses(servers).ToList(), IPAddress.Any)),
                    new TestMatrixItem("QueryServerReverse(s3,ip)", (client, servers) => client.QueryServerReverse(ToIPEndpoints(servers).ToList(), IPAddress.Any)),
                    new TestMatrixItem("QueryServerReverseAsync(s,ip)", (client, servers) => client.QueryServerReverseAsync(servers.ToList(), IPAddress.Any).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerReverseAsync(s2,ip)", (client, servers) => client.QueryServerReverseAsync(ToIPAddresses(servers).ToList(), IPAddress.Any).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryServerReverseAsync(s3,ip)", (client, servers) => client.QueryServerReverseAsync(ToIPEndpoints(servers).ToList(), IPAddress.Any).GetAwaiter().GetResult()),

                    // with servers and options
                    new TestMatrixItem("Query(q,s,o)", (client, options, servers) => client.QueryServer(servers.ToList(), question, options)),
                    new TestMatrixItem("QueryAsync(q,s,o)", (client, options, servers) => client.QueryServerAsync(servers.ToList(), question, options).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryReverse(ip,s,o)", (client, options, servers) => client.QueryServerReverse(servers.ToList(), IPAddress.Any, options)),
                    new TestMatrixItem("QueryReverseAsync(ip,s,o)", (client, options, servers) => client.QueryServerReverseAsync(servers.ToList(), IPAddress.Any, options).GetAwaiter().GetResult()),

                    // with query options
                    new TestMatrixItem("Query(q,o)", (client, options) => client.Query(question, options)),
                    new TestMatrixItem("QueryAsync(q,o)", (client, options) => client.QueryAsync(question, options).GetAwaiter().GetResult()),
                    new TestMatrixItem("QueryReverse(ip,o)", (client, options) => client.QueryReverse(IPAddress.Any, options)),
                    new TestMatrixItem("QueryReverseAsync(ip,o)", (client, options) => client.QueryReverseAsync(IPAddress.Any, options).GetAwaiter().GetResult()));

                IReadOnlyCollection<IPAddress> ToIPAddresses(IReadOnlyCollection<NameServer> nameServers)
                {
                    return nameServers?.Select(p => p.IPEndPoint.Address).ToArray();
                }

                IReadOnlyCollection<IPEndPoint> ToIPEndpoints(IReadOnlyCollection<NameServer> nameServers)
                {
                    return nameServers?.Select(p => p.IPEndPoint).ToArray();
                }
            }
        }

        public static TheoryData<TestMatrixItem> AllWithoutServerQueries
            => new(All.OfType<TestMatrixItem>().Where(a => !a.UsesServers));

        public static TheoryData<TestMatrixItem> AllWithServers
            => new(All.OfType<TestMatrixItem>().Where(a => a.UsesServers));

        public static TheoryData<TestMatrixItem> AllWithQueryOptions
            => new(All.OfType<TestMatrixItem>().Where(a => a.UsesQueryOptions));

        public static TheoryData<TestMatrixItem> AllWithoutQueryOptionsOrServerQueries
            => new(All.OfType<TestMatrixItem>().Where(a => (a.UsesQueryOptions || a.UsesServers)));

        [Theory]
        [MemberData(nameof(AllWithoutServerQueries))]
        public void ConfigMatrix_NoServersConfiguredThrows(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions() { AutoResolveNameServers = false };
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersQueriesExpectServers(TestMatrixItem test)
        {
            var unresolvedOptions = new LookupClientOptions() { AutoResolveNameServers = false };
            Assert.Throws<ArgumentOutOfRangeException>("servers", () => test.Invoke(lookupClientOptions: unresolvedOptions, useServers: Array.Empty<NameServer>()));
        }

        [Theory]
        [MemberData(nameof(AllWithServers))]
        public void ConfigMatrix_ServersCannotBeNull(TestMatrixItem test)
        {
            var options = new LookupClientOptions { AutoResolveNameServers = true };
            Assert.Throws<ArgumentNullException>("servers", () => test.InvokeNoDefaults(lookupClientOptions: options, useOptions: options, useServers: null));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_ValidSettingsResponse(TestMatrixItem test)
        {
            // Changing some random settings to a different value then defaults to verify proper setting usage.
            var unresolvedOptions = new LookupClientOptions(NameServer.GooglePublicDns)
            {
                EnableAuditTrail = true,
                Recursion = false,
                ThrowDnsErrors = true
            };

            var queryOptions = new DnsQueryAndServerOptions(NameServer.GooglePublicDns2)
            {
                ContinueOnEmptyResponse = false,
                ContinueOnDnsError = false
            };
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };

            var result = test.Invoke(lookupClientOptions: unresolvedOptions, useOptions: queryOptions, useServers: servers);

            Assert.Null(result.TestClient.TcpHandler.LastRequest);
            Assert.NotNull(result.TestClient.UdpHandler.LastRequest);
            Assert.StrictEqual(unresolvedOptions, result.TestClient.Client.Options);

            if (test.UsesQueryOptions && test.UsesServers)
            {
                Assert.Equal(servers[0], new NameServer(result.TestClient.UdpHandler.LastServer));
                Assert.Equal(servers[0], result.Response.NameServer);
                Assert.Equal(queryOptions, result.Response.Settings);
            }
            else if (test.UsesQueryOptions)
            {
                Assert.Equal(queryOptions.NameServers.First(), new NameServer(result.TestClient.UdpHandler.LastServer));
                Assert.Equal(queryOptions.NameServers.First(), result.Response.NameServer);
                Assert.Equal(queryOptions, result.Response.Settings);
            }
            else if (test.UsesServers)
            {
                Assert.Equal(servers[0], new NameServer(result.TestClient.UdpHandler.LastServer));
                Assert.Equal(servers[0], result.Response.NameServer);
                // by server overrules settings, but doesn't change the servers collection in settings..
                Assert.Equal(unresolvedOptions, result.Response.Settings);
            }
            else
            {
                Assert.Equal(unresolvedOptions.NameServers.First(), new NameServer(result.TestClient.UdpHandler.LastServer));
                Assert.Equal(unresolvedOptions.NameServers.First(), result.Response.NameServer);
                Assert.Equal(unresolvedOptions, result.Response.Settings);
            }
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverride(TestMatrixItem test)
        {
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };
            var defaultOptions = new LookupClientOptions() { AutoResolveNameServers = false };
            var queryOptions = new DnsQueryAndServerOptions(NameServer.GooglePublicDns2IPv6)
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions, useServers: servers);

            Assert.Null(result.TestClient.UdpHandler.LastRequest);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            Assert.Equal(defaultOptions, result.TestClient.Client.Options);
            Assert.Equal(queryOptions, result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyOverrideWithServerFallback(TestMatrixItem test)
        {
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6);
            var queryOptions = new DnsQueryAndServerOptions()
            {
                ContinueOnDnsError = !defaultOptions.ContinueOnDnsError,
                EnableAuditTrail = !defaultOptions.EnableAuditTrail,
                Recursion = !defaultOptions.Recursion,
                Retries = defaultOptions.Retries * 2,
                ThrowDnsErrors = !defaultOptions.ThrowDnsErrors,
                Timeout = TimeSpan.FromMinutes(10),
                UseCache = !defaultOptions.UseCache,
                UseRandomNameServer = !defaultOptions.UseRandomNameServer,
                UseTcpFallback = !defaultOptions.UseTcpFallback,
                UseTcpOnly = !defaultOptions.UseTcpOnly,
                RequestDnsSecRecords = true,
                ExtendedDnsBufferSize = 3333
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions, useServers: servers);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.TcpHandler.LastRequest.Question);
            Assert.Null(result.TestClient.Client.Cache.Get(cacheKey));

            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.TestClient.TcpHandler.LastServer);
            Assert.Equal(NameServer.GooglePublicDns2IPv6, result.Response.NameServer);
            // make sure we don't alter the original object
            Assert.Empty(queryOptions.NameServers);
            Assert.Equal(queryOptions, result.Response.Settings);
        }

        [Theory]
        [MemberData(nameof(AllWithQueryOptions))]
        public void ConfigMatrix_VerifyCacheUsed(TestMatrixItem test)
        {
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false
            };
            var queryOptions = new DnsQueryAndServerOptions()
            {
                UseCache = true
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions, useServers: servers);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question);
            Assert.NotNull(result.TestClient.Client.Cache.Get(cacheKey));
        }

        [Theory]
        [MemberData(nameof(All))]
        public void ConfigMatrix_VerifyCacheNotUsed(TestMatrixItem test)
        {
            var servers = new NameServer[] { NameServer.GooglePublicDns2IPv6 };
            var defaultOptions = new LookupClientOptions(NameServer.GooglePublicDns2IPv6)
            {
                UseCache = false,
                MinimumCacheTimeout = TimeSpan.FromMilliseconds(1000)
            };
            var queryOptions = new DnsQueryAndServerOptions()
            {
                UseCache = false
            };

            var result = test.Invoke(lookupClientOptions: defaultOptions, useOptions: queryOptions, useServers: servers);

            // verify that override settings also control cache
            var cacheKey = ResponseCache.GetCacheKey(result.TestClient.UdpHandler.LastRequest.Question);
            Assert.Null(result.TestClient.Client.Cache.Get(cacheKey));
        }

        public class TestMatrixItem
        {
            public bool UsesServers { get; }

            public Func<ILookupClient, IDnsQueryResponse> ResolverSimple { get; }

            public Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> ResolverServers { get; }

            public string Name { get; }

            public Func<ILookupClient, DnsQueryAndServerOptions, IDnsQueryResponse> ResolverQueryOptions { get; }

            public Func<ILookupClient, DnsQueryAndServerOptions, IReadOnlyCollection<NameServer>, IDnsQueryResponse> ResolverServersAndOptions { get; }

            public bool UsesQueryOptions { get; }

            private TestMatrixItem(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            public TestMatrixItem(string name, Func<ILookupClient, DnsQueryAndServerOptions, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverQueryOptions = resolver;
                UsesQueryOptions = true;
                UsesServers = false;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IReadOnlyCollection<NameServer>, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverServers = resolver;
                UsesQueryOptions = false;
                UsesServers = true;
            }

            public TestMatrixItem(string name, Func<ILookupClient, DnsQueryAndServerOptions, IReadOnlyCollection<NameServer>, IDnsQueryResponse> resolver)
               : this(name)
            {
                ResolverServersAndOptions = resolver;
                UsesQueryOptions = true;
                UsesServers = true;
            }

            public TestMatrixItem(string name, Func<ILookupClient, IDnsQueryResponse> resolver)
                : this(name)
            {
                ResolverSimple = resolver;
                UsesQueryOptions = false;
                UsesServers = false;
            }

            public TestResponse Invoke(LookupClientOptions lookupClientOptions = null, DnsQueryAndServerOptions useOptions = null, IReadOnlyCollection<NameServer> useServers = null)
            {
                var testClient = new TestClient(lookupClientOptions);
                var servers = useServers ?? new NameServer[] { IPAddress.Loopback };
                var queryOptions = useOptions ?? new DnsQueryAndServerOptions();

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, servers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, queryOptions);
                }
                else if (ResolverServersAndOptions != null)
                {
                    response = ResolverServersAndOptions(testClient.Client, queryOptions, useServers);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public TestResponse InvokeNoDefaults(LookupClientOptions lookupClientOptions, DnsQueryAndServerOptions useOptions, IReadOnlyCollection<NameServer> useServers)
            {
                var testClient = new TestClient(lookupClientOptions);

                IDnsQueryResponse response = null;
                if (ResolverServers != null)
                {
                    response = ResolverServers(testClient.Client, useServers);
                }
                else if (ResolverQueryOptions != null)
                {
                    response = ResolverQueryOptions(testClient.Client, useOptions);
                }
                else if (ResolverServersAndOptions != null)
                {
                    response = ResolverServersAndOptions(testClient.Client, useOptions, useServers);
                }
                else
                {
                    response = ResolverSimple(testClient.Client);
                }

                return new TestResponse(testClient, response);
            }

            public override string ToString()
            {
                return $"{Name} => s:{UsesServers} q:{UsesQueryOptions}";
            }
        }

        public class TestResponse
        {
            public TestResponse(TestClient testClient, IDnsQueryResponse response)
            {
                TestClient = testClient;
                Response = response;
            }

            public TestClient TestClient { get; }

            public IDnsQueryResponse Response { get; }
        }

        public class TestClient
        {
            public TestClient(LookupClientOptions options)
            {
                UdpHandler = new ConfigurationTrackingMessageHandler();
                TcpHandler = new ConfigurationTrackingMessageHandler(DnsMessageHandleType.TCP);
                Client = new LookupClient(options, UdpHandler, TcpHandler);
            }

            internal ConfigurationTrackingMessageHandler UdpHandler { get; }

            internal ConfigurationTrackingMessageHandler TcpHandler { get; }

            public LookupClient Client { get; }
        }

        internal class ConfigurationTrackingMessageHandler : DnsMessageHandler
        {
            // raw bytes from mcnet.com
            private static readonly byte[] s_zoneData = new byte[]
            {
               95, 207, 129, 128, 0, 1, 0, 11, 0, 0, 0, 1, 6, 103, 111, 111, 103, 108, 101, 3, 99, 111, 109, 0, 0, 255, 0, 1, 192, 12, 0, 1, 0, 1, 0, 0, 1, 8, 0, 4, 172, 217, 17, 238, 192, 12, 0, 28, 0, 1, 0, 0, 0, 71, 0, 16, 42, 0, 20, 80, 64, 22, 8, 13, 0, 0, 0, 0, 0, 0, 32, 14, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 17, 0, 50, 4, 97, 108, 116, 52, 5, 97, 115, 112, 109, 120, 1, 108, 192, 12, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 4, 0, 10, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 30, 4, 97, 108, 116, 50, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 20, 4, 97, 108, 116, 49, 192, 91, 192, 12, 0, 15, 0, 1, 0, 0, 2, 30, 0, 9, 0, 40, 4, 97, 108, 116, 51, 192, 91, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 51, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 50, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 52, 192, 12, 192, 12, 0, 2, 0, 1, 0, 4, 31, 116, 0, 6, 3, 110, 115, 49, 192, 12, 0, 0, 41, 16, 0, 0, 0, 0, 0, 0, 0
            };

            public override DnsMessageHandleType Type { get; }

            public IReadOnlyList<DnsQueryAndServerOptions> UsedSettings { get; }

            public IPEndPoint LastServer { get; private set; }

            public DnsRequestMessage LastRequest { get; private set; }

            public ConfigurationTrackingMessageHandler(DnsMessageHandleType type = DnsMessageHandleType.UDP)
            {
                Type = type;
            }

            public override DnsResponseMessage Query(
                IPEndPoint server,
                DnsRequestMessage request,
                TimeSpan timeout)
            {
                LastServer = server;
                LastRequest = request;

                using var writer = new DnsDatagramWriter(new ArraySegment<byte>(s_zoneData.ToArray()));
                writer.Index = 0;
                writer.WriteInt16NetworkOrder((short)request.Header.Id);
                writer.Index = s_zoneData.Length;

                var response = GetResponseMessage(writer.Data);

                if (response.Header.Id != request.Header.Id)
                {
                    throw new Exception();
                }

                return response;
            }

            public override Task<DnsResponseMessage> QueryAsync(
                IPEndPoint server,
                DnsRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastServer = server;
                LastRequest = request;
                // no need to run async here as we don't do any IO
                return Task.FromResult(Query(server, request, Timeout.InfiniteTimeSpan));
            }
        }
    }
}
