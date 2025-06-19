// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;

namespace DnsClient
{
    /// <summary>
    /// Represents a simple request message which can be send through <see cref="DnsMessageHandler"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Request:{Header} => {Question}")]
    internal class DnsRequestMessage
    {
        public DnsRequestHeader Header { get; }

        public DnsQuestion Question { get; }

        public DnsQueryOptions Options { get; }

        public DnsRequestMessage(DnsRequestHeader header, DnsQuestion question, DnsQueryOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(header);
            ArgumentNullException.ThrowIfNull(question);
            this.Header = header;
            this.Question = question;
            this.Options = options ?? new();
        }

        public override string ToString()
        {
            return $"{this.Header} => {this.Question}";
        }
    }
}