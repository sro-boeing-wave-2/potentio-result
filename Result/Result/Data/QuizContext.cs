using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Result.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Data
{
    public class QuizContext
    {
        private readonly IMongoDatabase _database = null;

        public QuizContext(IOptions<Settings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            if (client != null)
                _database = client.GetDatabase(settings.Value.Database);
        }

        public IMongoCollection<UserQuizDetail> UserQuizDetail
        {
            get
            {
                return _database.GetCollection<UserQuizDetail>("UserQuizDetail"); //<Quiz> is the Document ,"Quiz" is the Collection in our QuizDb
            }
        }

        public IMongoCollection<UserResult> UserResult
        {
            get
            {
                return _database.GetCollection<UserResult>("UserResult");
            }
        }

        public IMongoCollection<UserQuizResponse> UserQuizResponse
        {
            get
            {
                return _database.GetCollection<UserQuizResponse>("UserQuizResponse");
            }
        }

    }
}
