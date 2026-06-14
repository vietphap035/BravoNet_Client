using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DACS_1.Database
{
    public class DatabaseConnection
    {
        private static readonly string connectionString = "Server=localhost;Database=LTCSharp;Integrated Security=True;TrustServerCertificate=True;";
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static SqlCommand CreateCommand(string query, SqlConnection connection)
        {
            return new SqlCommand(query, connection);
        }
    }
}
