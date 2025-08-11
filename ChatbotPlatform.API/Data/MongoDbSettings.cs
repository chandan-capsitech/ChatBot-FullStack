using MongoDB.Driver.Core.Connections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatbotPlatform.API.Data
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// The max number of connection allowed in the connection pool
        /// Increasing this may help in high-concurrency scenarios.
        /// Default is 100
        /// </summary>
        public int MaxConnectionPoolSize { get; set; } = 100;

        /// <summary>
        /// The max amount of time the driver will wait to find an available server
        /// before throwing an exception. Useful for handling failover or cluster selection.
        /// Default is 30 seconds
        /// </summary>
        public TimeSpan ServerSelectionTimeout { get; set; } = TimeSpan.FromSeconds(30);


        /// <summary>
        /// The amount of time allowed for a TCP connection to be established with the MongoDB server.
        /// Helps detect slow or unreachable hosts.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}