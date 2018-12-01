﻿using Microwave.Application.Exceptions;

namespace Microwave.Application.Results
{
    internal class ConcurrencyError : Result
    {
        public long ExpectedVersion { get; }
        public long ActualVersion { get; }

        public ConcurrencyError(long expectedVersion, long actualVersion)
        {
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }

        public override void Check()
        {
            throw new ConcurrencyViolatedException(ExpectedVersion, ActualVersion);
        }
    }
}