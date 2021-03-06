﻿using MongoDB.Bson;
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

        public List<CumulativeTagScore> TagWiseCumulativeScore { get; set; }
    }


    public class QuizResult
    {
      
        public string QuizId { get; set; }

        public DateTime Date { get; set; }

        public List<QuestionAttempted> QuestionsAttempted { get; set; }

        public double ObtainedScore { get; set; }

        public double TotalScore { get; set; }

        public double PercentageScore { get; set; }

        public List<TagWiseResult> TagWiseResults { get; set; }
    }

    public class TagWiseResult
    {
        public string TagName { get; set; }

        public double TagCorrectAnsCount { get; set; }

        public double TagTotalQuestCount { get; set; }

        public double TagCorrectPercentage { get; set; }

        public double TagRating { get; set; }

        public string TaxonomyLevel { get; set; }

        public double TaxonomyScore { get; set; }

        public List<TaxonomyListAndScore> TaxonomyListAndScores{ get; set; }
    }

    public class CumulativeTagScore
    {
        public string TagName { get; set; }

        public double TagRating { get; set; }

        public string TaxonomyLevelReached { get; set; }

        public double TaxonomyScore { get; set; }

        public List<TaxonomyListAndScore> TaxonomyListAndScores { get; set; }
    }

    public class TaxonomyListAndScore
    {
        public string TaxonomyName { get; set; }

        public int TaxonomyScoreNumber { get; set; }
    }
}

