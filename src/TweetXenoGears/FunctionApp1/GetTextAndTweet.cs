using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static int nextLineIndex = 0;

        [FunctionName("TweetXenoGears")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                if (!File.Exists(DatabaseName))
                    await CreateDatabase();

                string line = await GetNextLineAsync(nextLineIndex);

                log.LogInformation("Tweeting line #{Index}: {Line}", nextLineIndex, line);

                nextLineIndex++;

            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Warning thrown");
            }

            log.LogDebug($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private static async Task<string> GetNextLineAsync(int index)
        {
            using var db = new SQLiteConnection($"Data Source={DatabaseName}");

            db.Open();

            string sql = "SELECT line FROM script WHERE number = @index";

            SQLiteCommand command = new(sql, db);
            command.Parameters.AddWithValue("index", index);

            return Convert.ToString(await command.ExecuteScalarAsync());
        }

        private static async Task CreateDatabase()
        {
            SQLiteConnection db = CreateAndGetDb();

            await AddLinesToDb(db);
        }

        private static async Task AddLinesToDb(SQLiteConnection db)
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

                    SQLiteCommand command2 = new(insertQuery, db);
                    command2.Parameters.AddWithValue("line", line);
                    command2.Parameters.AddWithValue("count", count);
                    command2.ExecuteNonQuery();

                    count++;
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        private static SQLiteConnection CreateAndGetDb()
        {
            SQLiteConnection.CreateFile(DatabaseName);

            var db = new SQLiteConnection($"Data Source={DatabaseName}");
            db.Open();

            string sql = CreateTableCommand;

            SQLiteCommand command = new(sql, db);

            command.ExecuteNonQuery();

            return db;
        }
    }

}
