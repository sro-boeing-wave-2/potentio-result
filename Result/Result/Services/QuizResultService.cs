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

        public async Task<UserQuizDetail> AddQuiz(UserQuizDetail quiz)
        {

            quiz.Time = new DateTime();
            await _context.Quiz.InsertOneAsync(quiz);

            //Perform calculating tasks on the UserResults Collection
            int userId = quiz.UserId;
            UpdateUserResults(quiz);
            return quiz;
        }

        //Returns result of a user for a particular domain
        public async Task<UserResult> GetUserResults(int userId, string domainName)
        {
            return await _context.userResult.Find(entry => entry.UserId == userId && entry.DomainName == domainName).FirstOrDefaultAsync();
        }


        //public async Task<> GetLastTestUserDomainResults(int userId, string domainName)
        //{

        //}




        //Whenever a new Quiz is submitted by a user, the UserResult gets upadated
        public async void UpdateUserResults(UserQuizDetail quiz)
        {
            int userId = quiz.UserId;

            //Calculate total score of this quiz
            double newScore = calculateTotalScoreOfQuiz(quiz);
            string domainName = quiz.Domain;

            // Check whether an entry with same User and domain already exists or not
            var userResultsEntry = await _context.userResult.Find(entryy => entryy.UserId.Equals(userId) && entryy.DomainName.Equals(domainName)).FirstOrDefaultAsync();

            //If the entry with unique (user + domain) cannot be found in userResult, create a new entry and insert in the userResult
            if (userResultsEntry == null)
            {
                List<double> scores = new List<double>();
                scores.Add(newScore);
                UserResult userResults = new UserResult()
                {
                    UserId = userId,
                    DomainName = quiz.Domain,
                    AverageScore = newScore,
                    Scores = scores,
                };
                //Insert the newly found entry to the UserResult Collection
                await _context.userResult.InsertOneAsync(userResults); 
            }
            // If the entry with unique (user + domain) is already in the userResult, update the existing entry
            else
            {
                double averageScore = userResultsEntry.AverageScore;
                List<double> scores = userResultsEntry.Scores;
                int numOfEntry = scores.Count;
                double totalScore = numOfEntry * averageScore;
                double updatedTotalScore = totalScore + newScore;
                updatedTotalScore = updatedTotalScore / (numOfEntry + 1);
                scores.Add(newScore);
                var filter = Builders<UserResult>.Filter.Eq(x => x.UserId, userId);
                filter = filter & (Builders<UserResult>.Filter.Eq(x => x.DomainName, domainName));
                var update = Builders<UserResult>.Update.Set(x => x.AverageScore, updatedTotalScore).Set(x => x.Scores, scores);
                var result = await _context.userResult.UpdateOneAsync(filter, update);
            }
        }


        public double calculateTotalScoreOfQuiz(UserQuizDetail quizDetail)
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

        public void calculateTagWiseResult(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;

        }


    }
}