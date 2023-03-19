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
        public static async Task Run([TimerTrigger("0 0 */1 * * *")] TimerInfo myTimer,
            ILogger log)
        {
            DatabaseName = Environment.GetEnvironmentVariable("DatabasePath");

            try
            {   
                if (!File.Exists(DatabaseName))
                {
                    log.LogError("No database was found on path {Path}", DatabaseName);
                    throw new FileNotFoundException("No Sqlite db found");
                }

                log.LogInformation("Getting index");
                int nextLineIndex = await GetNextLineIndexAsync(log);

                log.LogInformation("Getting next line with index {Index}", nextLineIndex);
                string line = await GetNextLineAsync(nextLineIndex);

                log.LogInformation("Tweeting line #{Index}: {Line}", nextLineIndex, line);
                var twitterBusiness = new TwitterBusiness();

                await twitterBusiness.TweetString(line);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception thrown");
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

                //await InsertIndexTable(db);
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
                .Single(str => str.EndsWith("xenogears-disc2.txt"));

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new(stream);
            var lineCount = 7309;

            while (!reader.EndOfStream)
            {
                try
                {
                    var line = await reader.ReadLineAsync();
                    string insertQuery = Insertcommand;
                    await db.OpenAsync();

                    SQLiteCommand command2 = new(insertQuery, db);
                    command2.Parameters.AddWithValue("line", line);
                    command2.Parameters.AddWithValue("count", lineCount);
                    command2.ExecuteNonQuery();
                    lineCount++;

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

            // SQLiteConnection.CreateFile(DatabaseName);

            var db = new SQLiteConnection($"Data Source={DatabaseName}");

            return db;

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
