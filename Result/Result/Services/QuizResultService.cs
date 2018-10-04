using Manager.QuizResultManager;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Result.Data;
using Result.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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


        //logic to calculate some parameters taking from the UserQuizDetail table and putting/updating in the UserResult table
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

            //calculates the tagwise result for this quiz
            getTagWiseResult(quiz, tagWiseResults);

            // Check whether an entry with same User and domain already exists or not
            var userResultsEntry = await _context.UserResult.Find(entryy => entryy.UserId.Equals(userId) && entryy.DomainName.Equals(domainName)).FirstOrDefaultAsync();

            QuizResult quizResult = new QuizResult()
            {
                QuizId = quiz.QuizId,
                QuestionsAttempted = questionsList,
                ObtainedScore = newObtainedScore,
                TotalScore = newTotalScore,
                PercentageScore = newPercentageScore,
                TagWiseResults = tagWiseResults,
                Date = quiz.Time
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

                List<CumulativeTagScore> cumulativeTagScores = getCumulativeTagWiseResult(quiz, userResultsEntry);

                double averagePercentage = userResultsEntry.AveragePercentage;


                

                int numOfEntry = userResultsEntry.QuizResults.Count;

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
                    .Set(x => x.TagWiseCumulativeScore, cumulativeTagScores);
                var result = await _context.UserResult.UpdateOneAsync(filter, update);
            }
        }


        //Calculating the score OBTAINED from the response given by the QuizEngine 
        public double calculateObtainedScoreOfQuiz(UserQuizDetail quizDetail)
        {
            List<QuestionAttempted> questionAttemptedList = quizDetail.QuestionsAttempted;
            double score = 0;
            double multiplyFactor = 1;          //can change the multiply factor if needed
            foreach (QuestionAttempted quest in questionAttemptedList)
            {
                if (quest.IsCorrect) score += quest.DifficultyLevel * multiplyFactor;
            }
            return score;
        }

        //Calculating the TOTAL score obtained from the response given by the QuizEngine 
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


        //Get tag wise result of the given quiz and save it in UserResult Table
        public void getTagWiseResult(UserQuizDetail quiz, List<TagWiseResult> tagWiseResult)
        {
            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();
            Dictionary<string, double> taxonomyTotalScore = new Dictionary<string, double>();
            Dictionary<string, double> totalDenCount = new Dictionary<string, double>();


            string[] taxonomyLevels = {"Remember", "Understand", "Apply", "Analyze", "Evaluate", "Create" };

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));
            }

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                correctTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                taxonomyTotalScore.Add(item, 0);
                totalDenCount.Add(item, 0);
                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        totalDenCount[item] += 1/(float)(question.ConceptTags.Length);
                        if (question.IsCorrect)
                        {
                            correctTagCount[item] += 1;
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                            taxonomyTotalScore[item] += Array.IndexOf(taxonomyLevels, question.Taxonomy)+1;
                        }
                    }
                }

                tagRatingList[item] /= totalDenCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);

                double tagCorrectPercentage = ((double)correctTagCount[item] / (double)totalTagCount[item]) * 100;
                tagCorrectPercentage = Math.Round(tagCorrectPercentage, 2);

                TagWiseResult tag = new TagWiseResult();
                tag.TagName = item;
                tag.TagTotalQuestCount = totalTagCount[item];
                tag.TagCorrectAnsCount = correctTagCount[item];
                tag.TagCorrectPercentage = tagCorrectPercentage;
                tag.TagRating = tagRatingList[item];
                tag.TaxonomyLevel = getTaxonomyLevel(item, questions);
                tag.TaxonomyScore = taxonomyTotalScore[item];
                tagWiseResult.Add(tag);
            }
        }

        public string getTaxonomyLevel(string concept, List<QuestionAttempted> questions)
        {
            //dont change the order of this, cause this is how it is.
            string[] taxonomyLevels = { "Create", "Evaluate", "Analyze", "Apply", "Understand", "Remember" };
            foreach (string taxLevel in taxonomyLevels){
                int trueCount = 0;
                int falseCount = 0;
                foreach(QuestionAttempted question in questions){
                    if (question.ConceptTags.Contains(concept) && question.Taxonomy == taxLevel)
                    {
                        if (question.IsCorrect) trueCount++;
                        else falseCount++;
                    }
                }

                if (trueCount!=0 && trueCount >= falseCount)
                {
                    return taxLevel;
                }
            }
            return "NA";
        }


        //Update UserQuizDetail table from the UserQuizResponse to use it in UserResult table. This is to make the code less coupled
        //so that even if the parameters of the UserQuizDetail has changed, we need not have to change everything.
        public UserQuizDetail UpdateUserQuizDetail(UserQuizResponse userQuizResponse)
        {
            UserQuizDetail userQuizDetail = new UserQuizDetail();
            List<QuestionAttempted> questionsAttempted = new List<QuestionAttempted>();
           

            string quizId = userQuizResponse.QuizId;
            int userId = userQuizResponse.UserId;
            string domainName = userQuizResponse.DomainName;
            DateTime time = DateTime.Now;

            List<Object> questionsList = userQuizResponse.QuestionsAttempted;

            Console.WriteLine(questionsList.Count+ "COUNT");

            int questionCount = 1;

            foreach (var item in questionsList)
            {
                QuestionAttempted question = new QuestionAttempted();
                try
                {
                    //  Question item = x as Question;
                    Object x = item;
                    JObject Parseddetail = JObject.Parse(JsonConvert.SerializeObject(x));
                    string questionType = Parseddetail.GetValue("questionType").ToString();
                    Console.WriteLine("THIS IS THE TYP" + questionType);
                    System.Reflection.Assembly b = System.Reflection.Assembly.Load("Potentiometer.Core");
                    Type QuestionType = b.GetType("Potentiometer.Core.QuestionTypes." + questionType);
                    object instanceObjectOfQuestion = Activator.CreateInstance(QuestionType);
                    JsonConvert.PopulateObject(JsonConvert.SerializeObject(x), instanceObjectOfQuestion);
                    Console.WriteLine("THIS IS THE POPILATED QUESTION " + JsonConvert.SerializeObject(x));
                    string questionId = Parseddetail.GetValue("questionId").ToString();
                    Console.WriteLine("THIS IS THE QUESTION ID " + questionId);

                    string questionText = Parseddetail.GetValue("questionText").ToString();
                    List<Result.Models.Options> options = JsonConvert.DeserializeObject<List<Result.Models.Options>>(Parseddetail.GetValue("options").ToString()); // Parseddetail.GetValue("options").ToString() ;
                    
                    string conceptTagsString =   Parseddetail.GetValue("conceptTags").ToString() ;
                    int difficultyLevel = Convert.ToInt32(Parseddetail.GetValue("difficultyLevel").ToString());
                    //string userResponse = Parseddetail.GetValue("userResponse").ToString();
                    string raw = Parseddetail.GetValue("raw").ToString();
                   // string correctOption = Parseddetail.GetValue("correctOption").ToString();
                    string taxonomy = Parseddetail.GetValue("taxonomy").ToString();
                    
                    Console.WriteLine(conceptTagsString.Substring(1, conceptTagsString.Length -2 ).Split(','));
                    string[] arr = conceptTagsString.Substring(1, conceptTagsString.Length - 2).Split(',');

                    string[] conceptTags = new string[arr.Length];
                    int index = 0;
                    foreach (string s in arr)
                    {
                        string replacement = Regex.Replace(s, @"\t|\n|\r", "");
                        replacement = replacement.Replace('"', ' ').Trim();
                        conceptTags[index] = replacement;
                        index++;
                    
                    }
                    Console.WriteLine("---");
                    
                    question.QuestionId = questionId;
                    question.QuestionText = questionText;
                    question.QuestionNumber = questionCount++;
                    question.Raw = raw;
                    question.DifficultyLevel = difficultyLevel;
                    question.Taxonomy = taxonomy;
                    question.ConceptTags = conceptTags;
                    question.QuestionType = questionType;

                    List<string> optionList = new List<string>();
                    foreach (var option in options)
                    {
                        optionList.Add(option.Raw);
                    }
                    question.Options = optionList;

                    if(questionType == "MCQ")
                    {
                        string resp = JsonConvert.DeserializeObject<Result.Models.Options>(Parseddetail.GetValue("response").ToString()).Raw;
                        string ans = JsonConvert.DeserializeObject<Result.Models.Options>(Parseddetail.GetValue("correctAnswer").ToString()).Raw;

                        Console.WriteLine(resp + " " + resp.Length);
                        Console.WriteLine(ans + " "+ ans.Length );
                        question.IsCorrect = (resp == ans);
                        Console.WriteLine(question.IsCorrect);
                        question.Response = resp;
                        question.CorrectAns = ans;
                    }
                    else
                    {
                        List<Result.Models.Options> resp = JsonConvert.DeserializeObject<List<Result.Models.Options>>(Parseddetail.GetValue("response").ToString());
                        List <Result.Models.Options> ans = JsonConvert.DeserializeObject<List<Result.Models.Options>>(Parseddetail.GetValue("correctAnswer").ToString());

                        HashSet<string> h = new HashSet<string>();
                        HashSet<string> h1 = new HashSet<string>();
                        
                        foreach (var itemm in resp)
                        {
                            h.Add(itemm.Raw);
                        }
                        foreach (var item1 in ans)
                        {
                            h1.Add(item1.Raw);
                        }
                        question.IsCorrect = h.SetEquals(h1) && h.Count==h1.Count;
                        question.ResponseList = resp;
                        question.CorrectAnsList = ans;
                    }
                    questionsAttempted.Add(question);
                    
                }
                catch(Exception e) {
                    Console.WriteLine("exception occured -------- "+e);
                }
        

                //string questionId = item.QuestionId;
                //string questionText = item.QuestionText;
                //List<Result.Models.Options> options = item.Options;
                //string questionType = item.QuestionType;
                //string domain = item.Domain;
                //string[] conceptTags = item.ConceptTags;
                //int difficultyLevel = item.DifficultyLevel;
                //string userResponse = item.Response.Raw;
                //string raw = item.Raw;
                //string correctOption = item.CorrectAnswer.Raw;

                
                //Boolean isCorrect = (userResponse==correctOption);
                //string taxonomy = item.Taxonomy;

                
                
                
            }

            userQuizDetail.QuizId = quizId;
            userQuizDetail.UserId = userId;
            userQuizDetail.Domain = domainName;
            userQuizDetail.Time = time;
            userQuizDetail.QuestionsAttempted = questionsAttempted;

            _context.UserQuizDetail.InsertOne(userQuizDetail);

            return userQuizDetail;
        }


        //for the first time, calculate the cumultaive tag wise result for that user-domain test.
        public void getCumulativeTagWiseResultFirst(UserQuizDetail quiz, List<CumulativeTagScore> cumulativeTagScore)
        {
            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();
            Dictionary<string, double> taxScoreCumulative = new Dictionary<string, double>();
            Dictionary<string, double> totalDenCount = new Dictionary<string, double>();
            string[] taxonomyLevels = { "Remember", "Understand", "Apply", "Analyze", "Evaluate", "Create"};

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));
            }

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                taxScoreCumulative.Add(item, 0);
                totalDenCount.Add(item, 0);

                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        totalDenCount[item] += 1 / (float)(question.ConceptTags.Length);
                        if (question.IsCorrect)
                        {
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                            taxScoreCumulative[item] += Array.IndexOf(taxonomyLevels, question.Taxonomy)+1;
                        }
                    }
                }
                tagRatingList[item] /= totalDenCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);
                CumulativeTagScore tag = new CumulativeTagScore();
                tag.TagName = item;
                tag.TagRating = tagRatingList[item];
                tag.TaxonomyLevelReached = getTaxonomyLevel(item,questions);
                tag.TaxonomyScore = taxScoreCumulative[item];
                cumulativeTagScore.Add(tag);
            }
        }



        // for not-the-first-time-user calculate the cumulative tag result -
        // or you may say update the current knowledge status of the user
        // unique for user-domain
        public List<CumulativeTagScore> getCumulativeTagWiseResult(UserQuizDetail quiz, UserResult userResult)
        {
            List<QuestionAttempted> questions = quiz.QuestionsAttempted;
            HashSet<string> labels = new HashSet<string>();
            Dictionary<string, int> totalTagCount = new Dictionary<string, int>();
            Dictionary<string, int> correctTagCount = new Dictionary<string, int>();
            Dictionary<int, int> questionTagCount = new Dictionary<int, int>();
            Dictionary<string, double> tagRatingList = new Dictionary<string, double>();
            Dictionary<string, double> taxScoreCumulative = new Dictionary<string, double>();
            Dictionary<string, double> totalDenCount = new Dictionary<string, double>();
            string[] taxonomyLevels = { "Remember", "Understand", "Apply", "Analyze", "Evaluate", "Create" };

            foreach (var item in questions)
            {
                labels.UnionWith(new HashSet<string>(item.ConceptTags));
            }
            List<CumulativeTagScore> newCumulativeTagScores = new List<CumulativeTagScore>();

            foreach (var item in labels)
            {
                totalTagCount.Add(item, 0);
                tagRatingList.Add(item, 0);
                taxScoreCumulative.Add(item, 0);
                totalDenCount.Add(item, 0);
                foreach (var question in questions)
                {
                    if (question.ConceptTags.Contains(item))
                    {
                        totalTagCount[item] += 1;
                        totalDenCount[item] += 1 / (float)(question.ConceptTags.Length);
                        if (question.IsCorrect)
                        {
                            tagRatingList[item] += 1 / (float)(question.ConceptTags.Length);
                            taxScoreCumulative[item] += Array.IndexOf(taxonomyLevels, question.Taxonomy)+1;
                        }
                    }
                }
                tagRatingList[item] /= totalDenCount[item];
                tagRatingList[item] = Math.Round(tagRatingList[item], 2);
            }
            //calculated the value for the current test, now we'll calculate the aggregate

            int numOfQuiz = userResult.QuizResults.Count-1;
            List<CumulativeTagScore> cumulativeTagScores = userResult.TagWiseCumulativeScore;


            HashSet<string> oldTag = new HashSet<string>();


            foreach (var old in cumulativeTagScores)
            {
                oldTag.Add(old.TagName);
            }

            HashSet<string> h = new HashSet<string>(labels);
            HashSet<string> h1 = new HashSet<string>(oldTag);
            
            Console.WriteLine("================");
            Console.WriteLine(String.Join(",", h1));
            Console.WriteLine(String.Join(",", h));
            Console.WriteLine("================");

            h.ExceptWith(oldTag);
            h1.ExceptWith(labels);

            Console.WriteLine(String.Join(",", labels));

            Console.WriteLine("================");
            Console.WriteLine(String.Join(",", h1));
            Console.WriteLine(String.Join(",", h));
            Console.WriteLine("================");

            foreach (var item in cumulativeTagScores)
            {
                if (!h1.Contains(item.TagName))
                {
                    CumulativeTagScore score = new CumulativeTagScore();
                    string tagName = item.TagName;
                    double tagRating = item.TagRating;
                    string taxLevelOld = item.TaxonomyLevelReached;
                    string taxLevelNew = getTaxonomyLevel(tagName, questions);

                    double taxScoreOld = item.TaxonomyScore;


                    double taxScoreNew = taxScoreCumulative[item.TagName] + taxScoreOld;

                    double oldTotalTemp = tagRating * numOfQuiz;
                    double newTotalTemp = oldTotalTemp + tagRatingList[tagName];
                    double newTagRating = newTotalTemp / (numOfQuiz + 1);
                    newTagRating = Math.Round(newTagRating, 2);
                    score.TagName = tagName;
                    score.TagRating = newTagRating;
                    score.TaxonomyLevelReached = getHigerTaxonomyLevel(taxLevelOld, taxLevelNew);
                    score.TaxonomyScore = taxScoreNew;
                    newCumulativeTagScores.Add(score);
                }
                else
                {
                    CumulativeTagScore score = new CumulativeTagScore();
                    score.TagName = item.TagName;
                    score.TagRating = item.TagRating;
                    score.TaxonomyLevelReached = item.TaxonomyLevelReached;
                    score.TaxonomyScore = item.TaxonomyScore;
                    newCumulativeTagScores.Add(score);
                }
            }

            foreach (var item in h)
            {
                CumulativeTagScore score = new CumulativeTagScore();
                score.TagName = item;
                score.TagRating = tagRatingList[item];
                score.TaxonomyLevelReached = getTaxonomyLevel(item, questions);
                score.TaxonomyScore = taxScoreCumulative[item];
                newCumulativeTagScores.Add(score);
            }

            return newCumulativeTagScores;
        }

        public static string getHigerTaxonomyLevel(string tax1, string tax2)
        {
            string[] taxonomyLevels = { "Remember", "Understand", "Apply", "Analyze", "Evaluate", "Create" };

            return Array.IndexOf(taxonomyLevels, tax1) > Array.IndexOf(taxonomyLevels, tax2) ? tax1 : tax2;
        }

    }
}