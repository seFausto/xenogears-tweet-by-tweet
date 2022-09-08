using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using FunctionApp1;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace XenoGearsByTweet
{
    public class GetTextAndTweet
    {
        private static string DatabaseName = "xeno.sqlite";

        private const string Insertcommand = "INSERT INTO script VALUES (@line, @count)";
        private const string CreateTableCommand = "CREATE TABLE script (line VARCHAR(8000), number INT)";
        private const string CreateIndexTableCommand = "CREATE TABLE IndexTable (number INT)";
        private const string NextIndexQuery = @"
                    select max(number) from IndexTable;
                    insert into IndexTable values ((select max(number) from IndexTable) + 1);
                ";


        [FunctionName("TweetXenoGears")]
        public static async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger log)
        {
            DatabaseName = Environment.GetEnvironmentVariable("DatabasePath");

            try
            {
                if (!File.Exists(DatabaseName))
                    await CreateDatabase();

                log.LogInformation("Getting index at {Timer}", myTimer);
                int nextLineIndex = await GetNextLineIndexAsync(log);

                log.LogInformation("Getting next line with index {Index} at {Timer}", nextLineIndex, myTimer);
                string line = await GetNextLineAsync(nextLineIndex);

                log.LogInformation("Tweeting line #{Index}: {Line} at {Timer}", nextLineIndex, line, myTimer);
                var twitterBusiness = new TwitterBusiness();

                await twitterBusiness.TweetStringList(line);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Warning thrown");
            }
        }

        private static async Task<int> GetNextLineIndexAsync(ILogger log)
        {
            try
            {
                log.LogDebug("Creating Database");
                using var db = new SQLiteConnection($"Data Source={DatabaseName}");
                await db.OpenAsync();

                SQLiteCommand command2 = new(NextIndexQuery, db);
                var result = await command2.ExecuteScalarAsync();
                await db.CloseAsync();
                return Convert.ToInt32(result);

            }
            catch
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
                string insertQuery = "insert into IndexTable (number) values (0);";

                await db.OpenAsync();

                SQLiteCommand command2 = new(insertQuery, db);

                await command2.ExecuteNonQueryAsync();

                await db.CloseAsync();
            }
            catch
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
                catch
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
