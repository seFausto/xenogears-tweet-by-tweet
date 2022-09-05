using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public class GetTextAndTweet
    {
        [FunctionName("TweetXenoGears")]
        public void Run([TimerTrigger("0 */10 * * * *")]TimerInfo myTimer, ILogger log)
        {
            //Get Current line
            //read next non-empty line
            //tweet. If text is long than max chars, make 2 tweets
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
