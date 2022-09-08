using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Parameters;

namespace FunctionApp1
{

    class TwitterSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ApiKeySecret { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string AccessTokenSecret { get; set; } = string.Empty;
        public long UserId { get; set; }
    }

    public class TwitterBusiness
    {
        private readonly TwitterSettings _twitterSettings;

        public TwitterBusiness()
        {
            _twitterSettings = new TwitterSettings
            {
                ApiKey = Environment.GetEnvironmentVariable("ApiKey"),
                ApiKeySecret = Environment.GetEnvironmentVariable("ApiKeySecret"),
                AccessToken = Environment.GetEnvironmentVariable("AccessToken"),
                AccessTokenSecret = Environment.GetEnvironmentVariable("AccessTokenSecret"),
                UserId = Convert.ToInt64(Environment.GetEnvironmentVariable("UserId"))
            };
        }

        public async Task TweetString(string message)
        {
            var userClient = GetTwitterClient();
            try
            {
                _ = await userClient.Tweets.PublishTweetAsync(message);
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Error quoting tweet {TweetId}", tweet.Id);
                throw;
            }
        }

        private TwitterClient GetTwitterClient()
        {
            return new TwitterClient(_twitterSettings.ApiKey, _twitterSettings.ApiKeySecret,
                _twitterSettings.AccessToken, _twitterSettings.AccessTokenSecret);
        }
    }
}
