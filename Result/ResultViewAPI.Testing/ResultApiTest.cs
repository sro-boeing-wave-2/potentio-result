using Result.Models;
using System;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using Result.Services;
using Result.Controllers;
using System.Threading.Tasks;
using Moq;
using MongoDB.Driver;
//using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;


//using Microsoft.AspNetCore.Mvc;


namespace ResultViewAPI.Testing
{
    public class ResultApiTest
    {

        [Fact]
        public async void GetUserResultTest()
        {

            Mock<IQuizResultService> mockRepo = new Mock<IQuizResultService>();
            MockUserResult mockDbHelper = new MockUserResult();

            mockRepo.Setup(entry => entry.GetUserResults(16, "C")).Returns(mockDbHelper.GetTestResultData());
            QuizResultController controller = new QuizResultController(mockRepo.Object);
            // Act
            var result = await controller.UserResultForGivenQuizAndDomain(16, "C");
            OkObjectResult objectResult = result as OkObjectResult;
            UserResult objectResultValue = objectResult.Value as UserResult;
            // Assert
            Assert.Equal(50, objectResultValue.AverageScore);
        }
        [Fact]
        public async void PostTest()
        {
            
            // Arrange
            Mock<IQuizResultService> mockRepo = new Mock<IQuizResultService>();
            MockUserResult mockDbHelper = new MockUserResult();
            UserQuizDetail entryQuiz = await mockDbHelper.GetQuizEntry();
            mockRepo.Setup(repo => repo.AddQuiz(entryQuiz)).Returns(mockDbHelper.GetQuizEntry());
            QuizResultController controller = new QuizResultController(mockRepo.Object);

            //Act
            var result = await controller.PostQuiz(entryQuiz);
            OkObjectResult objectResult = result as OkObjectResult;

            //Assert
            Assert.Equal("Java", entryQuiz.Domain);



        }
    }
}   

