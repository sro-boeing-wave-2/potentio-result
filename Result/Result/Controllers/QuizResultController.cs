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
        public async Task<IActionResult> UserResultForGivenQuizAndDomain([FromQuery (Name = "userId")] int userId, [FromQuery(Name = "domainName")] string domainName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userResult = await _quizResultService.GetUserResults(userId, domainName);

            if (userResult == null)
            {
                return NotFound("no such entry");
            }
            return Ok(userResult);
        }

        //public async Task<> UserResultByQuizId([FromQuery] string quizId)
        //{
        //    var quizResult = await _quizResultService
        //}


        //public async Task<IActionResult> LastTestUserDomainDetails([FromQuery] int userId, [FromQuery] string domainName)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }
        //    var userResult = await _quizResultService.GetUserResults(userId, domainName);
        //    if (userResult == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(userResult);
        //}




    }
}