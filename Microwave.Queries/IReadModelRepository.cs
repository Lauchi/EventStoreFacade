﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microwave.Domain.Identities;
using Microwave.Domain.Results;

namespace Microwave.Queries
{
    public interface IReadModelRepository
    {
        Task<Result<T>> Load<T>() where T : Query;
        Task<ReadModelResult<T>> Load<T>(Identity id) where T : ReadModel;
        Task<Result> Save<T>(T query) where T : Query;
        Task<Result> Save<T>(ReadModelResult<T> readModelResult) where T : ReadModel, new();
        Task<Result<IEnumerable<T>>> LoadAll<T>() where T : ReadModel;
    }

    public class ReadModelResult<T> : Result<T>
    {
        protected ReadModelResult(ResultStatus status, T readModel, long version, Identity id) : base(status)
        {
            _version = version;
            _value = readModel;
            _id = id;
        }

        public ReadModelResult(T readModel, Identity id, long version) : base(new Ok())
        {
            _version = version;
            _value = readModel;
            _id = id;
        }

        private readonly long _version;
        private readonly Identity _id;

        public long Version
        {
            get
            {
                Status.Check();
                return _version;
            }
        }

        public Identity Id
        {
            get
            {
                Status.Check();
                return _id;
            }
        }

        public static ReadModelResult<T> Ok(T value, Identity id, long version)
        {
            return new Ok<T>(value, version, id);
        }

        public new static ReadModelResult<T> NotFound(Identity notFoundId)
        {
            return new NotFoundResult<T>(notFoundId);
        }
    }

    public class Ok<T> : ReadModelResult<T>
    {
        public Ok(T readModel, long version, Identity id) : base(new Ok(), readModel, version, id)
        {
        }
    }

    public class NotFoundResult<T> : ReadModelResult<T>
    {
        public NotFoundResult(Identity notFoundId) : base(new NotFound(typeof(T), notFoundId), default(T), -1, null)
        {
        }
    }
}