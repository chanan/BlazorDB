using System;
using System.Collections.Generic;

namespace BlazorDB.Storage
{
    internal class Metadata
    {
        public List<Guid> Guids { get; set; }
        public string ModelName { get; set; }
        public string ContextName { get; set; }
    }
}