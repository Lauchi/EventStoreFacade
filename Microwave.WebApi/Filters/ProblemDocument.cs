using System.Collections.Generic;
using System.Linq;
using Microwave.Domain.Validation;
using Newtonsoft.Json;

namespace Microwave.WebApi.Filters
{
    public class ProblemDocument
    {
        public string Key { get; }

        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string Detail { get; }

        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public IEnumerable<ProblemDocument> DomainErrors { get; }

        public ProblemDocument(string key, string detail)
        {
            Key = key;
            Detail = detail;
        }

        public ProblemDocument(string key, string detail, IEnumerable<DomainError> domainErrors)
        {
            Key = key;
            Detail = detail;
            DomainErrors =
                domainErrors.Select(error => new ProblemDocument(error.ErrorType, error.Description));
        }
    }
}