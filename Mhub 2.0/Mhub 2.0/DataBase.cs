using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Mhub_2._0
{
    static class DataBase
    {
        public static LREntities DatebaseConection { get; }
        static DataBase() => DatebaseConection = new LREntities();
    }
}
