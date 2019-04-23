using System;
using System.Collections.Generic;

namespace BlazorDB.Storage
{
    public class Metadata
    {
        public List<Guid> Guids { get; set; }
        public string ModelName { get; set; }
        public string ContextName { get; set; }
        public int MaxId { get; set; }
    }
}