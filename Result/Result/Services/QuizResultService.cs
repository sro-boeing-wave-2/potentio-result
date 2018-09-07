using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Result.Data;
using Result.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Services
{
    public class QuizResultService : IQuizResultService
    {
        private readonly QuizContext _context = null;

        public QuizResultService(IOptions<Settings> settings)
        {
            _context = new QuizContext(settings);
        }

        public async Task<UserQuizResponse> AddQuiz(UserQuizResponse quiz)
        {
            // inserting in userquizresponse, userquizdetail and updating userresult

            await _context.UserQuizResponse.InsertOneAsync(quiz);
            UserQuizDetail userQuizDetail =  UpdateUserQuizDetail(quiz);
            UpdateUserResults(userQuizDetail);
            return quiz;
        }

        //Returns result of a user for a particular domain
        public async Task<UserResult> GetUserResults(int userId, string domainName)
        {
            return await _context.UserResult.Find(entry => entry.UserId == userId && entry.DomainName == domainName).FirstOrDefaultAsync();
        }

        


        public async void UpdateUserResults(UserQuizDetail quiz)
        {
            int userId = quiz.UserId;

            //Calculate total score of this quiz
            double newTotalScore = calculateTotalScoreOfQuiz(quiz);
            double newObtainedScore = calculateObtainedScoreOfQuiz(quiz);
            double newPercentageScore = ((newObtainedScore * 100 )/ newTotalScore);
            string domainName = quiz.Domain;
            List<QuestionAttempted> questionsList = quiz.QuestionsAttempted;
            List<TagWiseResult> tagWiseResults = new List<TagWiseResult>();
            TagWiseResult tagWiseResult = new TagWiseResult();
            getTagWiseResult(quiz, tagWiseResult);
            tagWiseResults.Add(tagWiseResult);

            // Check whether an entry with same User and domain already exists or not
            var userResultsEntry = await _context.UserResult.Find(entryy => entryy.UserId.Equals(userId) && entryy.DomainName.Equals(domainName)).FirstOrDefaultAsync();

            QuizResult quizResult = new QuizResult()
            {
                QuizId = quiz.QuizId,
                QuestionsAttempted = questionsList,
                ObtainedScore = newObtainedScore,
                TotalScore = newTotalScore,
                PercentageScore = newPercentageScore,
                TagWiseResults = tagWiseResults
            };
            
            //If the entry with unique (user + domain) cannot be found in userResult, create a new entry and insert in the userResult
            if (userResultsEntry == null)
            {
                List<QuizResult> quizResults = new List<QuizResult>();
                quizResults.Add(quizResult);

                UserResult userResults = new UserResult()
                {
                    UserId = userId,
                    DomainName = domainName,
                    AveragePercentage = newPercentageScore,
                    QuizResults = quizResults
                };
                //Insert the newly found entry to the UserResult Collection
                await _context.UserResult.InsertOneAsync(userResults);
            }

            // If the entry with unique (user + domain) is already in the userResult, update the existing entry
            else
            {
                List<QuizResult> quizResults = userResultsEntry.QuizResults;
                quizResults.Add(quizResult);

                double averagePercentage = userResultsEntry.AveragePercentage;
                
                int numOfEntry = userResultsEntry.QuizResults.Count;
                double totalPercentage = numOfEntry * averagePercentage;
                double updatedTotalPercentage = totalPercentage + newPercentageScore;
                updatedTotalPercentage = updatedTotalPercentage / (numOfEntry + 1);

                userResultsEntry.AveragePercentage = updatedTotalPercentage;
                
                var filter = Builders<UserResult>.Filter.Eq(x => x.UserId, userId);
                filter = filter & (Builders<UserResult>.Filter.Eq(x => x.DomainName, domainName));
                var update = Builders<UserResult>.Update.Set(x => x.AveragePercentage, updatedTotalPercentage).Set(x => x.QuizResults, quizResults);
                var result = await _context.UserResult.UpdateOneAsync(filter, update);
            }
        }


        public double calculateObtainedScoreOfQuiz(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
            double score = 0;
            double multiplyFactor = 1;
            foreach(QuestionAttempted quest in questionAttemptedList)
            {
                if (quest.IsCorrect) score += quest.DifficultyLevel*multiplyFactor;   
            }
            return score;
        }

        public double calculateTotalScoreOfQuiz(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
            double score = 0;
            double multiplyFactor = 1;
            foreach(QuestionAttempted quest in questionAttemptedList)
            {
                score += quest.DifficultyLevel*multiplyFactor;   
            }
            return score;
        }



        public void calculateTagWiseResult(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
        }

        public void getTagWiseResult(UserQuizDetail quiz,TagWiseResult tagWiseResult)
        {

        }


        public UserQuizDetail UpdateUserQuizDetail(UserQuizResponse userQuizResponse)
        {
            UserQuizDetail userQuizDetail = new UserQuizDetail();
            List<QuestionAttempted> questionsAttempted = new List<QuestionAttempted>();


            string quizId = userQuizResponse.QuizId;
            int userId = userQuizResponse.UserId;
            string domainName = userQuizResponse.DomainName;
            List<Question> questionsList = userQuizResponse.QuestionsAttempted;

            int questionCount = 1;

            foreach (var item in questionsList)
            {
                string questionId = item.QuestionId;
                string questionText = item.QuestionText;
                List<string> options = item.Options;
                string questionType = item.QuestionType;
                string domain = item.Domain;
                string[] conceptTags = item.ConceptTags;
                int difficultyLevel = item.DifficultyLevel;
                string userResponse = item.UserResponse;
                string correctOption = item.CorrectOption;
                Boolean isCorrect = item.IsCorrect;
                
                QuestionAttempted question = new QuestionAttempted();
                question.QuestionId = questionId;
                question.QuestionText = questionText;
                question.QuestionNumber = questionCount++;
                question.Options = options;
                question.QuestionType = questionType;
                question.ConceptTags = conceptTags;
                question.DifficultyLevel = difficultyLevel;
                question.Response = userResponse;
                question.CorrectAns = correctOption;
                question.IsCorrect = isCorrect;

                questionsAttempted.Add(question);
            }

            
            userQuizDetail.QuizId = quizId;
            userQuizDetail.UserId = userId;
            userQuizDetail.Domain = domainName;
            userQuizDetail.Time = new DateTime();
            userQuizDetail.QuestionsAttempted = questionsAttempted;

            _context.UserQuizDetail.InsertOne(userQuizDetail);
            
            return userQuizDetail;
        }

    }
}