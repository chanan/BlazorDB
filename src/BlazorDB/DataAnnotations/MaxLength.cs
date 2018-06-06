using System;

namespace BlazorDB.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLength : Attribute
    {
        public MaxLength(int length)
        {
            this.length = length;
        }

        internal int length;
    }
}