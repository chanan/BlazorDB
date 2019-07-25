using System;

namespace BlazorDB
{
    public class BlazorDBUpdateException : Exception
    {
        public BlazorDBUpdateException(string error) : base(error)
        {
        }
    }
}
