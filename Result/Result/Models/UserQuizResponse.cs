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

        public List<Object> QuestionBank { get; set; }
    }

    public class Question
    {
       
        public string QuestionId { get; set; }

        public string QuestionText { get; set; }

        public string Raw { get; set; }

        public List<Options> Options { get; set; }

        public string QuestionType { get; set; }

        public string Domain { get; set; }

        public string Title { get; set; }

        public string Taxonomy { get; set; }

        public string[] ConceptTags { get; set; }

        public int DifficultyLevel { get; set; }

        public string Response { get; set; }

       // public bool IsCorrect { get; set; }

        public Options CorrectAnswer { get; set; }
    }

    public class Options
    {
        public string Raw { get; set; }
        public string OptionText { get; set; }
    }
}
