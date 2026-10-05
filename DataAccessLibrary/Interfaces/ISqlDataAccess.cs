using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Interfaces
{
    public interface ISqlDataAccess
    {
        List<T> LoadData<T, U>(string sql,
                          U parameters,
                          string connectionString);
        void SaveData<T>(string sql,
                   T parameters,
                   string connectionString);

    }
}
