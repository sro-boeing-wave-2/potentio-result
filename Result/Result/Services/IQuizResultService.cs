using Result.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Result.Services
{
    public interface IQuizResultService
    {
        Task<UserQuizResponse> AddQuiz(UserQuizResponse quiz);
        Task<UserResult> GetUserResultsFromUserIdAndDomain(int userId, string domainName);
        Task<UserResult> GetUserResultsFromQuizId(string quizId);
        Task<IEnumerable<UserResult>> GetAllUserResult();
        HashSet<string> GetAllDistinctDomainUserList(int userId);
    }

}
