using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace viafront3.Services
{
    public class WebsocketToken
    {
        public DateTime Created;
        public int ExchangeUserId;

        public WebsocketToken(int exchangeUserId)
        {
            Created = DateTime.Now;
            ExchangeUserId = exchangeUserId;
        }
    }

    public interface IWebsocketTokens
    {
        string NewToken(int exchangeUserId);
        void Add(string token, int exchangeUserId);
        WebsocketToken Remove(string token);
    }

    public class WebsocketTokens : IWebsocketTokens
    {
        Dictionary<string, WebsocketToken> tokens;

        public WebsocketTokens()
        {
            tokens = new Dictionary<string, WebsocketToken>();
        }

        void ExpireOldTokens()
        {
            var timeLimit = DateTime.Now.AddMinutes(-5);
            var tokensToRemove = new List<string>();
            foreach (var pair in tokens)
                if (pair.Value.Created < timeLimit)
                    tokensToRemove.Add(pair.Key);
            foreach (var token in tokensToRemove)
                tokens.Remove(token);
        }

        public string NewToken(int exchangeUserId)
        {
            var token = Guid.NewGuid().ToString();
            Add(token, exchangeUserId);
            return token;
        }

        public void Add(string token, int exchangeUserId)
        {
            tokens.Add(token, new WebsocketToken(exchangeUserId));
            ExpireOldTokens();
        }

        public WebsocketToken Remove(string token)
        {
            if (tokens.ContainsKey(token))
            {
                var value = tokens[token];
                tokens.Remove(token);
                return value;
            }
            return null;
        }
    }
}
