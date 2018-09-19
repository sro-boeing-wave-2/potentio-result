using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Result.Models;
using Result.Services;

namespace Result.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizResultController : Controller
    {
        private readonly IQuizResultService _quizResultService;

        public QuizResultController(IQuizResultService quizResultService)
        {
            _quizResultService = quizResultService;
        }


        [HttpPost]
        public async Task<IActionResult> PostQuiz([FromBody] UserQuizResponse quiz)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _quizResultService.AddQuiz(quiz);
            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> UserResultForGivenUserIdAndDomain([FromQuery(Name = "userId")] int userId, [FromQuery(Name = "domainName")] string domainName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userResult = await _quizResultService.GetUserResultsFromUserIdAndDomain(userId, domainName);

            if (userResult == null)
            {
                return NotFound("no such entry");
            }
            return Ok(userResult);
        }

        [HttpGet("quizId/{quizId}")]
        public async Task<IActionResult> UserResultByQuizId([FromRoute] string quizId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userResult = await _quizResultService.GetUserResultsFromQuizId(quizId);

            if (userResult == null)
            {
                return NotFound("no such entry");
            }
            return Ok(userResult);
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllUserResult()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userResult = await _quizResultService.GetAllUserResult();

            if (userResult == null)
            {
                return NotFound("no such entry");
            }
            return Ok(userResult);
        }

        [HttpGet("getDomains")]
        public async Task<IActionResult> DistinctDomainUserList([FromQuery(Name = "userId")] int userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var userResult = _quizResultService.GetAllDistinctDomainUserList(userId);

            if (userResult == null)
            {
                return NotFound("no such entry");
            }
            return Ok(userResult);
        }


    }
}