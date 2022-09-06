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
        private const string appSettingKey = "functionSettings:nextLine";
        private const string DatabaseName = "xeno.sqlite";
        private static int nextLineIndex = 0;
        private static List<string> script = new();

        [FunctionName("TweetXenoGears")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                await CreateDatabase();

                string line = await GetNextLineAsync(nextLineIndex);
                nextLineIndex++;
                log.LogWarning(line);
            }
            catch (Exception ex)
            {
                throw;
            }

            log.LogDebug($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private async Task<string> GetNextLineAsync(int index)
        {
            using var db = new SQLiteConnection($"Data Source={DatabaseName}");

            db.Open();
            
            string sql = "SELECT line FROM script WHERE number = @index";
            
            SQLiteCommand command = new(sql, db);
            command.Parameters.AddWithValue("index", index);

            return Convert.ToString(await command.ExecuteScalarAsync());
        }

        private async Task CreateDatabase()
        {
            if (!System.IO.File.Exists(DatabaseName))
            {
                Console.WriteLine("Just entered to create Sync DB");
                SQLiteConnection.CreateFile(DatabaseName);

                using var sqlite2 = new SQLiteConnection($"Data Source={DatabaseName}");

                sqlite2.Open();
                string sql = "create table script (line varchar(8000), number int)";
                SQLiteCommand command = new(sql, sqlite2);
                command.ExecuteNonQuery();
               

                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith("xenogears-disc1.txt"));

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                using StreamReader reader = new(stream);
                var count = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    string insertQuery = $"insert into script values ('{line}', {count})";
                    count++;
                    SQLiteCommand command2 = new(insertQuery, sqlite2);
                    command2.ExecuteNonQuery();
                }
            }
        }
    }

}
