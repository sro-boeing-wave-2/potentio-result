using Result.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Services
{
    public interface IQuizResultService
    {
        Task<UserQuizDetail> AddQuiz(UserQuizDetail quiz);
        Task<UserResult> GetUserResults(int userId, string domainName);
    }

}
