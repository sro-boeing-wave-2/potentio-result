using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Models
{
    public class UserResult
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserResultId { get; set; }

        public int UserId { get; set; }

        public string DomainName { get; set; }

        public double AveragePercentage { get; set; }

        public List<QuizResult> QuizResults { get; set; }
    }


    public class QuizResult
    {
      
        public string QuizId { get; set; }

        public List<QuestionAttempted> QuestionsAttempted { get; set; }

        public double ObtainedScore { get; set; }

        public double TotalScore { get; set; }

        public double PercentageScore { get; set; }

        public List<TagWiseResult> TagWiseResults { get; set; }
    }

    public class TagWiseResult
    {
        public string TagName { get; set; }

        public double TagScoreObtained { get; set; }

        public double TagScoreTotal { get; set; }

        public double TagScorePercentage { get; set; }
    }
}

