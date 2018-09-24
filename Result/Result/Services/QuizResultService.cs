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
        //UserQuizResponse is what u get from QuizEngine
        public async Task<UserQuizResponse> AddQuiz(UserQuizResponse quiz)
        {
            // inserting in userquizresponse, userquizdetail and updating userresult

            await _context.UserQuizResponse.InsertOneAsync(quiz);
            UserQuizDetail userQuizDetail = UpdateUserQuizDetail(quiz);
            //UpdateUserResult updates the database
            UpdateUserResults(userQuizDetail);
            return quiz;
        }

        //Returns result of a user for a particular domain using userId and domain
        public async Task<UserResult> GetUserResultsFromUserIdAndDomain(int userId, string domainName)
        {
            return await _context.UserResult.Find(entry => entry.UserId == userId && entry.DomainName == domainName).FirstOrDefaultAsync();
        }


        //Returns result of a user for a particular domain using only quizId
        public async Task<UserResult> GetUserResultsFromQuizId(string quizId)
        {
            var x = _context.UserResult.AsQueryable<UserResult>().Where(u => u.QuizResults.Any(a => a.QuizId == quizId)).FirstOrDefault();
            return await Task.FromResult(x);
            //return await _context.UserResult.Find(e => e.QuizResults.Find(u => u.QuizId == quizId));
            //return await _context.UserResult.Find(entry => entry.UserId == 0 && entry.DomainName == "").FirstOrDefaultAsync();
            // remove after testing
        }

        public async Task<IEnumerable<UserResult>> GetAllUserResult()
        {
            var x = _context.UserResult.Find(q => q.UserId > 0);
            return await x.ToListAsync();
        }

        public HashSet<string> GetAllDistinctDomainUserList(int userId)
        {
            // x has all the documents of that user from the database
            var x = _context.UserResult.Find(y => y.UserId == userId).ToList();
            HashSet<String> domains = new HashSet<string>();
            foreach (var item in x)
            {
                domains.Add(item.DomainName);
            }
            return domains;
        }


        public async void UpdateUserResults(UserQuizDetail quiz)
        {
            int userId = quiz.UserId;

            //Calculate total score of this quiz
            double newTotalScore = calculateTotalScoreOfQuiz(quiz);
            //calculate  obtained score
            double newObtainedScore = calculateObtainedScoreOfQuiz(quiz);
            double newPercentageScore = ((newObtainedScore * 100) / newTotalScore);
            newPercentageScore = Math.Round(newPercentageScore, 2);
            string domainName = quiz.Domain;
            List<QuestionAttempted> questionsList = quiz.QuestionsAttempted;
            List<TagWiseResult> tagWiseResults = new List<TagWiseResult>();
            getTagWiseResult(quiz, tagWiseResults);                             //calculates the tagwise result for this quiz


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
                //calculate cumulative tag wise score
                List<QuizResult> quizResults = new List<QuizResult>();
                quizResults.Add(quizResult);
                List<CumulativeTagScore> cumulativeTagScores = new List<CumulativeTagScore>();
                getCumulativeTagWiseResultFirst(quiz, cumulativeTagScores);                   
             
                UserResult userResults = new UserResult()
                {
                    UserId = userId,
                    DomainName = domainName,
                    AveragePercentage = newPercentageScore,
                    QuizResults = quizResults,
                    TagWiseCumulativeScore = cumulativeTagScores
                };
                //Insert the newly found entry to the UserResult Collection
                await _context.UserResult.InsertOneAsync(userResults);
            }

            // If the entry with unique (user + domain) is already in the userResult, update the existing entry
            else
            {
                List<QuizResult> quizResults = userResultsEntry.QuizResults;
                quizResults.Add(quizResult);

                //List<CumulativeTagScore> cumulativeTagScores = userResultsEntry.TagWiseCumulativeScore;
                List<CumulativeTagScore> cumulativeTagScores = getCumulativeTagWiseResult(quiz, userResultsEntry);

                double averagePercentage = userResultsEntry.AveragePercentage;

                int numOfEntry = quizResults.Count;
                double totalPercentage = numOfEntry * averagePercentage;
                double updatedTotalPercentage = totalPercentage + newPercentageScore;
                updatedTotalPercentage = updatedTotalPercentage / (numOfEntry + 1);
                updatedTotalPercentage = Math.Round(updatedTotalPercentage, 2);
                userResultsEntry.AveragePercentage = updatedTotalPercentage;

                var filter = Builders<UserResult>.Filter.Eq(x => x.UserId, userId);
                filter = filter & (Builders<UserResult>.Filter.Eq(x => x.DomainName, domainName));
                var update = Builders<UserResult>.Update
                    .Set(x => x.AveragePercentage, updatedTotalPercentage)
                    .Set(x => x.QuizResults, quizResults)
                    .Set(x => x.TagWiseCumulativeScore,cumulativeTagScores);
                var result = await _context.UserResult.UpdateOneAsync(filter, update);
            }
        }


        public double calculateObtainedScoreOfQuiz(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
            double score = 0;
            double multiplyFactor = 1;
            foreach (QuestionAttempted quest in questionAttemptedList)
            {
                if (quest.IsCorrect) score += quest.DifficultyLevel * multiplyFactor;
            }
            return score;
        }

        public double calculateTotalScoreOfQuiz(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
            double score = 0;
            double multiplyFactor = 1;
            foreach (QuestionAttempted quest in questionAttemptedList)
            {
                score += quest.DifficultyLevel * multiplyFactor;
            }
            return score;
        }



        public void calculateTagWiseResult(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
        }

        public void getTagWiseResult(UserQuizDetail quiz, List<TagWiseResult> tagWiseResult)
        {
            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));
            }

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                correctTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        if (question.IsCorrect)
                        {
                            correctTagCount[item] += 1;
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                        }
                    }
                }

                tagRatingList[item] /= totalTagCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);

                double tagCorrectPercentage = ((double)correctTagCount[item] / (double)totalTagCount[item]) * 100;
                tagCorrectPercentage = Math.Round(tagCorrectPercentage, 2);

                TagWiseResult tag = new TagWiseResult();
                tag.TagName = item;
                tag.TagTotalQuestCount = totalTagCount[item];
                tag.TagCorrectAnsCount = correctTagCount[item];
                tag.TagCorrectPercentage = tagCorrectPercentage;
                tag.TagRating = tagRatingList[item];
                tagWiseResult.Add(tag);
            }
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
                List<Result.Models.Options> options = item.OptionList;
                string questionType = item.QuestionType;
                string domain = item.Domain;
                string[] conceptTags = item.ConceptTags;
                int difficultyLevel = item.DifficultyLevel;
                string userResponse = item.userResponse;
                string correctOption = item.CorrectOption;
                Boolean isCorrect = item.IsCorrect;

                QuestionAttempted question = new QuestionAttempted();
                question.QuestionId = questionId;
                question.QuestionText = questionText;
                question.QuestionNumber = questionCount++;
                List<string> optionList = new List<string>();
                foreach (var option in options)
                {
                    optionList.Add(option.Option);
                }
                question.Options = optionList;
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


        public void getCumulativeTagWiseResultFirst(UserQuizDetail quiz, List<CumulativeTagScore> cumulativeTagScore)
        {
            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));

            }

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        if (question.IsCorrect)
                        {
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                        }
                    }
                }
                tagRatingList[item] /= totalTagCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);
                CumulativeTagScore tag = new CumulativeTagScore();
                tag.TagName = item;
                tag.TagRating = tagRatingList[item];
                cumulativeTagScore.Add(tag);
            }
        }



        public List<CumulativeTagScore> getCumulativeTagWiseResult(UserQuizDetail quiz, UserResult userResult)
        {

            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));
            }
            List<CumulativeTagScore> newCumulativeTagScores = new List<CumulativeTagScore>();

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        if (question.IsCorrect)
                        {
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                        }
                    }
                }
                tagRatingList[item] /= totalTagCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);
            }
            //calculated the value for the current test, now we'll calculate the aggregate

            int numOfQuiz = userResult.QuizResults.Count;
            List<CumulativeTagScore> cumulativeTagScores = userResult.TagWiseCumulativeScore;

            foreach (var item in cumulativeTagScores)
            {
                CumulativeTagScore score = new CumulativeTagScore();
                string tagName = item.TagName;
                double tagRating = item.TagRating;

                double oldTotalTemp = tagRating * numOfQuiz;
                double newTotalTemp = oldTotalTemp + tagRatingList[tagName];
                double newTagRating = newTotalTemp / (numOfQuiz + 1);
                newTagRating = Math.Round(newTagRating, 2);
                score.TagName = tagName;
                score.TagRating = newTagRating;
                newCumulativeTagScores.Add(score);
            }
            
            return newCumulativeTagScores;
        }
    }


}