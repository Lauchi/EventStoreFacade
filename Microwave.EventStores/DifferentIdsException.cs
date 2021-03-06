using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microwave.EventStores
{
    [Serializable]
    public class DifferentIdsException : Exception
    {
        protected DifferentIdsException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public DifferentIdsException(IEnumerable<string> identities)
            : base($"Not able to write to different streams in one turn, write them separatly: {string.Join(",", identities)} Ids to differe")
        {
        }
    }
}