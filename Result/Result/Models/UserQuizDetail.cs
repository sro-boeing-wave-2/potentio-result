using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Models
{
    public class UserQuizDetail
    {
        
        public string QuizId { get; set; }

        public int UserId { get; set; }
      
        public string Domain { get; set; }
        
        public DateTime Time { get; set; }

        public List<QuestionAttempted> QuestionsAttempted { get; set; }
    }

    public class QuestionAttempted
    {
        
        public string QuestionId { get; set; }

        public int QuestionNumber { get; set; }
      
        public string QuestionText { get; set; }

        public int DifficultyLevel { get; set; }

        public string Response { get; set; }

        public bool IsCorrect { get; set; }

        public string[] ConceptTags { get; set; }

        public string CorrectAns { get; set; }

        public List<string> Options { get; set; }

        public string QuestionType { get; set; }
    }
}
