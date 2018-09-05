﻿using System;
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
        public async Task<IActionResult> PostQuiz([FromBody] UserQuizDetail quiz)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _quizResultService.AddQuiz(quiz);
            return Ok();
        }

        [HttpGet]
       
        public async Task<IActionResult> UserResultForGivenQuizAndDomain([FromQuery] int userId ,[FromQuery] string domainName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userResult = await _quizResultService.GetUserResults(userId, domainName);

            if(userResult == null)
            {
                return NotFound();
            }
            return Ok(userResult);
        }

        public async Task<IActionResult> LastTestUserDomainDetails([FromQuery] int userId, [FromQuery] string domainName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userResult = await _quizResultService.GetUserResults(userId, domainName);

            if (userResult == null)
            {
                return NotFound();
            }

            return Ok(userResult);
        }
        
    }
}