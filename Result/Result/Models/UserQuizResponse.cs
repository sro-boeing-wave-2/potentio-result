using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Models
{
    public class UserQuizResponse
    {
       
        public string QuizId { get; set; }

        public int UserId { get; set; }
       
        public string DomainName { get; set; }
        
        public int CurrentQuestionIndex { get; set; }

        public List<Question> QuestionsAttempted { get; set; }

        public List<Question> QuestionBank { get; set; }
    }

    public class Question
    {
       
        public string QuestionId { get; set; }

        public string QuestionText { get; set; }

        public List<Options> OptionList { get; set; }

        public string QuestionType { get; set; }

        public string Domain { get; set; }

        public string[] ConceptTags { get; set; }

        public int DifficultyLevel { get; set; }

        public string userResponse { get; set; }

        public bool IsCorrect { get; set; }

        public string CorrectOption { get; set; }
    }

    public class Options
    {
        public string Option { get; set; }
    }
}
