using System;

namespace BlazorDB.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class Required : Attribute
    {
    }
}