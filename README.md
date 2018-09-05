# Potenti-o-meter

A web app used to take Adaptive Quiz.
This branch will be used for Result Module.



-Response getting from QuizEngine Team:

public class UserQuizDetail
   {
       int userId 
       string domain
       List<QuestionAttempted> questionsAttempted
   }

   public class QuestionAttempted
   {
       int id
       int questionId
       string questionText
       int difficultyLevel
       string response
       bool isCorrect
       string[] conceptTags
       string correctAnswer
   }