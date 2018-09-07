﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Models
{
    public class UserQuizDetail
    {
        public string _id { get; set; }
        public int UserId { get; set; }
        //public int QuizId { get; set; }
        public string Domain { get; set; }
        //public double Score { get; set; }
        public DateTime Time { get; set; }
        public List<QuestionAttempted> QuestionsAttempted { get; set; }
    }

    public class QuestionAttempted
    {
        public int QuestionNumber { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public int DifficultyLevel { get; set; }
        public string Response { get; set; }
        public bool IsCorrect { get; set; }
        public string[] ConceptTags { get; set; }
        public string CorrectAns { get; set; }
    }
}
