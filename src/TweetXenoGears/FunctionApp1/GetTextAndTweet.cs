using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

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
                Console.WriteLine(line);
            }
            catch (Exception ex)
            {
                throw;
            }

            //var line = GetNextLine(nextLineIndex);

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }

        private async Task<string> GetNextLineAsync(int index)
        {
            using var sqlite2 = new SQLiteConnection($"Data Source={DatabaseName}");

            sqlite2.Open();
            
            string sql = $"Select line from script where number = {index}";
            
            SQLiteCommand command = new(sql, sqlite2);

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

                using Stream stream = Assembly.GetEntryAssembly()
                    .GetManifestResourceStream(resourceName);

                using StreamReader reader = new(stream);
                var count = 0;
                while (!reader.EndOfStream)
                {
                    string insertQuery = $"insert into script values ('{await reader.ReadLineAsync()}', {count})";
                    SQLiteCommand command2 = new SQLiteCommand(insertQuery, sqlite2);
                    command2.ExecuteNonQuery();
                }


            }
        }
    }

}
