using System;
using System.Configuration;
using System.Threading.Tasks;
using EmailService.App.Models;
using MySql.Data.MySqlClient;

namespace EmailService.App.Repositories.Implementations
{
    public class MonitoringRepository : IMonitoringRepository
    {
        public async Task InitAsync(string envrionment, string appName)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"SELECT * FROM watchdog
                                 WHERE environment = @env AND appname = @appName";

                var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@env", envrionment);
                command.Parameters.AddWithValue("@appName", appName);

                var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    reader.Close();
                    sqlQuery = @"INSERT INTO watchdog(environment,appname,lastping)
                                 VALUES (@env, @appName, @lastPing)";

                    var insertMonitorRow = connection.CreateCommand();
                    insertMonitorRow.CommandText = sqlQuery;
                    insertMonitorRow.Parameters.AddWithValue("@lastPing", DateTime.Now);
                    insertMonitorRow.Parameters.AddWithValue("@env", envrionment);
                    insertMonitorRow.Parameters.AddWithValue("@appName", appName);

                    await insertMonitorRow.ExecuteScalarAsync();
                }
            }
        }

        public async Task SendImAliveAsync(string envrionment, string appName, DateTime dateTime)
        {
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();

                var sqlQuery = @"UPDATE watchdog
                                 SET lastping = @lastPing
                                 WHERE environment = @env AND appname = @appName";

                var command = connection.CreateCommand();
                command.CommandText = sqlQuery;
                command.Parameters.AddWithValue("@lastPing", dateTime);
                command.Parameters.AddWithValue("@env", envrionment);
                command.Parameters.AddWithValue("@appName", appName);

                await command.ExecuteScalarAsync();
            }
        }


        #region Private methods

        private MySqlConnection CreateConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
            return new MySqlConnection(connectionString);
        }

        #endregion
    }
}