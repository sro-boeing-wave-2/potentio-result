using System;
using System.Collections.Generic;
using System.Text;
using Result.Models;
using System.Threading.Tasks;

namespace ResultViewAPI.Testing
{
    class MockUserResult
    {
        public async Task<UserResult> GetTestResultData()
        {
            var UserResult = new UserResult()
            {
                UserId = 10,
                DomainName = "Java",
                Scores = new List<double>() { 50, 50, 50 },
                AverageScore = 50,

            };
            return await Task.FromResult(UserResult);

        }
        public async Task<UserQuizDetail> GetQuizEntry()
        {
            QuestionAttempted qa = new QuestionAttempted();
            var QuizEntry = new UserQuizDetail()
            {
                UserId = 10,
                Domain = "Java",
                //Score = 50,

            };
            return await Task.FromResult(QuizEntry);

        }
        public async Task<List<UserResult>> GetTestResultListAsync()
        {
            var UserResults = new List<UserResult>
            {
                new UserResult()
                {
                    UserId = 10,
                    DomainName = "Java",
                    Scores  = new List<double>(){50,50,50},
                    AverageScore = 50,

                },
                new UserResult()
                {

                    UserId = 16,
                    DomainName = "C",
                    AverageScore = 60,
                    Scores  = new List<double>(){60,60,60},


                }
            };
            return await Task.FromResult(UserResults);
        }
    }
}
