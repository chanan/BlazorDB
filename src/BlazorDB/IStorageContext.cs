using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorDB
{
    public interface IStorageContext
    {
        void SaveChanges();
    }
}
