using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace XenoGearsByTweet
{
    public class GetTextAndTweet
    {
        private const string DatabaseName = "xeno.sqlite";
        private const string Insertcommand = "insert into script values (@line, @count)";
        private const string CreateTableCommand = "create table script (line varchar(8000), number int)";
        private const string CreateIndexTableCommand = "create table IndexTable (number int)";

        [FunctionName("TweetXenoGears")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string line = string.Empty;

            try
            {
                if (!File.Exists(DatabaseName))
                    await CreateDatabase();

                int nextLineIndex = await GetNextLineIndexAsync();

                line = await GetNextLineAsync(nextLineIndex);

                log.LogInformation("Tweeting line #{Index}: {Line}", nextLineIndex, line);

            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Warning thrown");
            }
        }

        private static async Task<int> GetNextLineIndexAsync()
        {
            const string query = @"
                    select max(number) from IndexTable;
                    insert into IndexTable values ((select max(number) from IndexTable) + 1);
                ";
            try
            {
                using var db = new SQLiteConnection($"Data Source={DatabaseName}");
                await db.OpenAsync();

                SQLiteCommand command2 = new(query, db);
                var result = await command2.ExecuteScalarAsync();
                await db.CloseAsync();
                return Convert.ToInt32(result);

            }
            catch (Exception ex)
            {
                throw;
            }

        }

        private static async Task<string> GetNextLineAsync(int index)
        {
            const string sql = "SELECT line FROM script WHERE number = @index";

            using var db = new SQLiteConnection($"Data Source={DatabaseName}");

            await db.OpenAsync();

            SQLiteCommand command = new(sql, db);
            command.Parameters.AddWithValue("index", index);

            object value = await command.ExecuteScalarAsync();
            await db.CloseAsync();

            return Convert.ToString(value);
        }

        private static async Task CreateDatabase()
        {
            try
            {
                SQLiteConnection db = await CreateAndGetDb();

                await InsertScriptTable(db);

                await InsertIndexTable(db);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static async Task InsertIndexTable(SQLiteConnection db)
        {
            try
            {

                string insertQuery = "insert into IndexTable values (0);";

                await db.OpenAsync();

                SQLiteCommand command2 = new(insertQuery, db);


                await db.CloseAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static async Task InsertScriptTable(SQLiteConnection db)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith("xenogears-disc1.txt"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            var count = 0;

            while (!reader.EndOfStream)
            {
                try
                {
                    var line = await reader.ReadLineAsync();
                    string insertQuery = Insertcommand;
                    await db.OpenAsync();

                    SQLiteCommand command2 = new(insertQuery, db);
                    command2.Parameters.AddWithValue("line", line);
                    command2.Parameters.AddWithValue("count", count);
                    command2.ExecuteNonQuery();
                    count++;

                    await db.CloseAsync();

                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        private static async Task<SQLiteConnection> CreateAndGetDb()
        {
            SQLiteConnection.CreateFile(DatabaseName);

            var db = new SQLiteConnection($"Data Source={DatabaseName}");
            await db.OpenAsync();

            SQLiteCommand command = new(CreateTableCommand, db);

            await command.ExecuteNonQueryAsync();

            command = new(CreateIndexTableCommand, db);

            await command.ExecuteNonQueryAsync();

            await db.CloseAsync();

            return db;
        }
    }

}
