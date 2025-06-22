// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnsClient.Protocol.Options;

namespace DnsClient
{
    internal class LookupClientAudit
    {
        private const int PrintOffset = -32;
        private readonly StringBuilder _auditWriter = new StringBuilder();
        private Stopwatch _swatch;

        public DnsQueryOptions Settings { get; }

        public LookupClientAudit(DnsQueryOptions settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void AuditCachedItem(IDnsQueryResponse response)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            StartTimer();
            _auditWriter.AppendLine($"; (cached result)");
            AuditResponseHeader(response.Header);

            AuditOptPseudo();

            var record = response.Additionals.OfRecordType(Protocol.ResourceRecordType.OPT).FirstOrDefault();
            if (record != null && record is OptRecord optRecord)
            {
                AuditEdnsOpt(optRecord.UdpSize, optRecord.Version, optRecord.IsDnsSecOk, optRecord.ResponseCodeEx);
            }

            AuditEnd(response, response.NameServer);
        }

        public void StartTimer()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _swatch = Stopwatch.StartNew();
            _swatch.Restart();
        }

        public void AuditResolveServers(int count)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

#if NET6_0_OR_GREATER
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $"; ({count} server found)");
#else
            _auditWriter.AppendLine($"; ({count} server found)");
#endif
        }

        public string Build(IDnsQueryResponse response = null)
        {
            if (!Settings.EnableAuditTrail)
            {
                return string.Empty;
            }

            var audit = _auditWriter.ToString();
            if (response != null)
            {
                DnsQueryResponse.SetAuditTrail(response, audit);
            }

            return audit;
        }

        public void AuditTruncatedRetryTcp()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; Truncated, retrying in TCP mode.");
            _auditWriter.AppendLine();
        }

        public void AuditResponseError(DnsHeaderResponseCode responseCode)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

#if NET6_0_OR_GREATER
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; ERROR: {DnsResponseCodeText.GetErrorText((DnsResponseCode)responseCode)}");
#else
            _auditWriter.AppendLine($";; ERROR: {DnsResponseCodeText.GetErrorText((DnsResponseCode)responseCode)}");
#endif
        }

        public void AuditOptPseudo()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; OPT PSEUDOSECTION:");
        }

        public void AuditResponseHeader(DnsResponseHeader header)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine(";; Got answer:");
            _auditWriter.AppendLine(header.ToString());
            if (header.RecursionDesired && !header.RecursionAvailable)
            {
                _auditWriter.AppendLine(";; WARNING: recursion requested but not available");
            }
            _auditWriter.AppendLine();
        }

        public void AuditEdnsOpt(short udpSize, byte version, bool doFlag, DnsResponseCode responseCode)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }
#if NET6_0_OR_GREATER
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $"; EDNS: version: {version}, flags:{(doFlag ? " do" : string.Empty)}; UDP: {udpSize}; code: {responseCode}");
#else
            _auditWriter.AppendLine($"; EDNS: version: {version}, flags:{(doFlag ? " do" : string.Empty)}; UDP: {udpSize}; code: {responseCode}");
#endif
        }

        public void AuditEnd(IDnsQueryResponse queryResponse, NameServer nameServer)
        {
            if (nameServer is null)
            {
                throw new ArgumentNullException(nameof(nameServer));
            }

            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            var elapsed = _swatch.ElapsedMilliseconds;

            // TODO: find better way to print the actual TTL of cached values
            if (queryResponse != null)
            {
                if (queryResponse.Questions.Count > 0)
                {
                    _auditWriter.AppendLine(";; QUESTION SECTION:");
                    foreach (var question in queryResponse.Questions)
                    {
                        _auditWriter.AppendLine(question.ToString(PrintOffset));
                    }
                    _auditWriter.AppendLine();
                }

                if (queryResponse.Answers.Count > 0)
                {
                    _auditWriter.AppendLine(";; ANSWER SECTION:");
                    foreach (var answer in queryResponse.Answers)
                    {
                        _auditWriter.AppendLine(answer.ToString(PrintOffset));
                    }
                    _auditWriter.AppendLine();
                }

                if (queryResponse.Authorities.Count > 0)
                {
                    _auditWriter.AppendLine(";; AUTHORITIES SECTION:");
                    foreach (var auth in queryResponse.Authorities)
                    {
                        _auditWriter.AppendLine(auth.ToString(PrintOffset));
                    }
                    _auditWriter.AppendLine();
                }

                var additionals = queryResponse.Additionals.Where(p => !(p is OptRecord)).ToArray();
                if (additionals.Length > 0)
                {
                    _auditWriter.AppendLine(";; ADDITIONALS SECTION:");
                    foreach (var additional in additionals)
                    {
                        _auditWriter.AppendLine(additional.ToString(PrintOffset));
                    }
                    _auditWriter.AppendLine();
                }
            }

#if NET6_0_OR_GREATER
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; Query time: {elapsed} msec");
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; SERVER: {nameServer.Address}#{nameServer.Port}");
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; WHEN: {DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss K yyyy", CultureInfo.InvariantCulture)}");
            _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; MSG SIZE  rcvd: {queryResponse.MessageSize}");
#else
            _auditWriter.AppendLine($";; Query time: {elapsed} msec");
            _auditWriter.AppendLine($";; SERVER: {nameServer.Address}#{nameServer.Port}");
            _auditWriter.AppendLine($";; WHEN: {DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss K yyyy", CultureInfo.InvariantCulture)}");
            _auditWriter.AppendLine($";; MSG SIZE  rcvd: {queryResponse.MessageSize}");
#endif
        }

        public void AuditException(Exception ex)
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            if (ex is DnsResponseException dnsEx)
            {
#if NET6_0_OR_GREATER
                _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; Error: {DnsResponseCodeText.GetErrorText(dnsEx.Code)} {dnsEx.InnerException?.Message ?? dnsEx.Message}");
#else
                _auditWriter.AppendLine($";; Error: {DnsResponseCodeText.GetErrorText(dnsEx.Code)} {dnsEx.InnerException?.Message ?? dnsEx.Message}");
#endif
            }
            else if (ex is AggregateException aggEx)
            {
#if NET6_0_OR_GREATER
                _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; Error: {aggEx.InnerException?.Message ?? aggEx.Message}");
#else
                _auditWriter.AppendLine($";; Error: {aggEx.InnerException?.Message ?? aggEx.Message}");
#endif
            }
            else
            {
#if NET6_0_OR_GREATER
                _auditWriter.AppendLine(CultureInfo.InvariantCulture, $";; Error: {ex.Message}");
#else
                _auditWriter.AppendLine($";; Error: {ex.Message}");
#endif
            }

            if (Debugger.IsAttached)
            {
                _auditWriter.AppendLine(ex.ToString());
            }
        }

        public void AuditRetryNextServer()
        {
            if (!Settings.EnableAuditTrail)
            {
                return;
            }

            _auditWriter.AppendLine();
            _auditWriter.AppendLine($"; Trying next server.");
        }
    }
}
