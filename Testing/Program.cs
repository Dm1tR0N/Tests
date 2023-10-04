using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using Testing.Classes;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "Data Source=TestingBD.db;Version=3;";

        using (SQLiteConnection connection = new SQLiteConnection(connectionString))
        {
            connection.Open();

            while (true)
            {
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("1. Регистрация");
                Console.WriteLine("2. Авторизация");
                Console.WriteLine("3. Создание теста");
                Console.WriteLine("4. Прохождение теста");
                Console.WriteLine("5. Формирование анализа результата тестирования");
                Console.WriteLine("6. Просмотр результатов тестирования");
                Console.WriteLine("7. Вывод всех прошедших тестирование за день в текстовый документ");
                Console.WriteLine("8. Сортировка по категориям теста");
                Console.WriteLine("9. Сортировка по баллам совместимости с должностью");
                Console.WriteLine("10. Почистить чат");
                Console.WriteLine("11. Выход");

                Console.Write("Ваш выбор: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        RegisterUser(connection);
                        break;
                    case "2":
                        AuthenticateUser(connection);
                        break;
                    case "3":
                        if (Users.Username != null)
                        {
                            CreateTest(connection);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Авторизируйся!");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "4":                       
                        if (Users.Username != null)
                        {
                            TakeTest(connection);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Авторизируйся!");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "5":
                        if (Users.Username != null)
                        {
                            AnalyzeTestResults(connection);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Авторизируйся!");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "6":
                        ViewTestResults(connection); 
                        break;
                    case "7":
                        Console.Write("Введите дату (гггг-мм-дд): ");
                        string date = Console.ReadLine();

                        string outputPath = "результаты_тестирования_" + date + ".txt";

                        if (File.Exists(outputPath))
                        {
                            Console.WriteLine($"Файл {outputPath} уже существует. Вы уверены, что хотите перезаписать его? (да/нет)");
                            string response = Console.ReadLine();

                            if (response.ToLower() != "да")
                            {
                                Console.WriteLine("Операция отменена.");
                                return;
                            }
                        }

                        using (StreamWriter writer = new StreamWriter(outputPath, false, Encoding.UTF8))
                        {
                            ListTestResultsForDate(connection, date, writer);
                        }

                        Console.WriteLine($"Результаты тестирования за {date} были записаны в файл {outputPath}.");
                        break;
                    case "8":
                        Console.WriteLine("Список доступных категорий:");
                        DisplayCategories(connection);

                        Console.Write("Введите номер категории для просмотра тестов: ");
                        if (int.TryParse(Console.ReadLine(), out int selectedCategoryID))
                        {
                            Console.WriteLine("Список тестов в выбранной категории:");
                            DisplayTestsInCategory(connection, selectedCategoryID);
                        }
                        else
                        {
                            Console.WriteLine("Некорректный ввод.");
                        }                  
                        break;
                    case "9":
                        List<string> jobTitles = GetJobTitles(connection);

                        Console.WriteLine("Список доступных должностей:");
                        for (int i = 0; i < jobTitles.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {jobTitles[i]}");
                        }

                        Console.Write("Введите номер должности:");
                        if (int.TryParse(Console.ReadLine(), out int selectedJobIndex) && selectedJobIndex > 0 && selectedJobIndex <= jobTitles.Count)
                        {
                            string selectedJobTitle = jobTitles[selectedJobIndex - 1];
                            List<Applicant> applicants = GetApplicants(connection);
                            Dictionary<int, int> compatibilityScores = CalculateCompatibilityScores(connection, selectedJobTitle, applicants);

                            Console.WriteLine("#################################################################################");
                            Console.WriteLine($"Список соискателей для должности '{selectedJobTitle}', отсортированный по баллам совместимости:");
                            foreach (var applicant in applicants.OrderByDescending(a => compatibilityScores[a.UserID]))
                            {
                                int compatibilityScore = compatibilityScores[applicant.UserID];
                                Console.WriteLine($"{applicant.Username} (ID: {applicant.UserID}) - Совместимость: {compatibilityScore}");
                            }
                            Console.WriteLine("#################################################################################");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Некорректный выбор должности.");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        break;
                    case "10":
                        Console.Clear();
                        break;
                    case "11":
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Неверный выбор. Попробуйте еще раз.");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
        }
    }

    static void RegisterUser(SQLiteConnection connection)
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();

        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        string insertUserQuery = "INSERT INTO 'Users' ('Username', 'Password') VALUES (@Username, @Password)";

        using (SQLiteCommand command = new SQLiteCommand(insertUserQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);

            try
            {
                int rowsInserted = command.ExecuteNonQuery();
                if (rowsInserted > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Регистрация успешно завершена.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Произошла ошибка при регистрации.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (SQLiteException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Произошла ошибка при выполнении SQL-запроса: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }

    static void AuthenticateUser(SQLiteConnection connection)
    {
        Console.Write("Введите имя пользователя: ");
        string username = Console.ReadLine();

        Console.Write("Введите пароль: ");
        string password = Console.ReadLine();

        string selectUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";

        using (SQLiteCommand command = new SQLiteCommand(selectUserQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);
            int userCount = Convert.ToInt32(command.ExecuteScalar());

            if (userCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Авторизация успешна.");
                Users.Username = username;
                Users.Password = password;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
               Console.ForegroundColor = ConsoleColor.Red;
               Console.WriteLine("Неверное имя пользователя или пароль.");
               Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }

    static void CreateTest(SQLiteConnection connection)
    {
        Console.Write("Введите название теста: ");
        string testName = Console.ReadLine();

        Console.Write("Введите ID категории теста: ");
        int categoryID;
        while (!int.TryParse(Console.ReadLine(), out categoryID) || categoryID < 1)
        {
            Console.Write("Пожалуйста, введите корректный ID категории: ");
        }

        List<TestQuestion> testQuestions = new List<TestQuestion>();

        bool addingQuestions = true;

        while (addingQuestions)
        {
            TestQuestion question = GetUserInputForQuestion();
            testQuestions.Add(question);

            Console.Write("Хотите добавить еще вопрос? (да/нет): ");
            string response = Console.ReadLine();
            if (response.ToLower() != "да")
            {
                addingQuestions = false;
            }
        }

        CreateTest(connection, testName, categoryID, testQuestions);

       Console.ForegroundColor = ConsoleColor.Green;
       Console.WriteLine("Тест успешно создан и сохранен в базе данных.");
       Console.ForegroundColor = ConsoleColor.White;
    }

    static void CreateTest(SQLiteConnection connection, string testName, int categoryID, List<TestQuestion> testQuestions)
    {
        using (SQLiteTransaction transaction = connection.BeginTransaction())
        {
            try
            {
                string insertTestQuery = "INSERT INTO 'Tests' ('CategoryID', 'TestName') VALUES (@CategoryID, @TestName)";
                using (SQLiteCommand testCommand = new SQLiteCommand(insertTestQuery, connection, transaction))
                {
                    testCommand.Parameters.AddWithValue("@CategoryID", categoryID);
                    testCommand.Parameters.AddWithValue("@TestName", testName);
                    testCommand.ExecuteNonQuery();
                }

                string getLastInsertTestIDQuery = "SELECT last_insert_rowid();";
                long testID;
                using (SQLiteCommand lastInsertTestIDCommand = new SQLiteCommand(getLastInsertTestIDQuery, connection, transaction))
                {
                    testID = (long)lastInsertTestIDCommand.ExecuteScalar();
                }

                foreach (TestQuestion question in testQuestions)
                {
                    string insertQuestionQuery = "INSERT INTO 'Questions' ('TestID', 'QuestionText') VALUES (@TestID, @QuestionText)";
                    using (SQLiteCommand questionCommand = new SQLiteCommand(insertQuestionQuery, connection, transaction))
                    {
                        questionCommand.Parameters.AddWithValue("@TestID", testID);
                        questionCommand.Parameters.AddWithValue("@QuestionText", question.QuestionText);
                        questionCommand.ExecuteNonQuery();
                    }

                    string getLastInsertQuestionIDQuery = "SELECT last_insert_rowid();";
                    long questionID;
                    using (SQLiteCommand lastInsertQuestionIDCommand = new SQLiteCommand(getLastInsertQuestionIDQuery, connection, transaction))
                    {
                        questionID = (long)lastInsertQuestionIDCommand.ExecuteScalar();
                    }

                    foreach (string answerText in question.AnswerOptions)
                    {
                        int isCorrect = (answerText == question.CorrectAnswer) ? 1 : 0;
                        string insertAnswerQuery = "INSERT INTO 'Answers' ('QuestionID', 'AnswerText', 'IsCorrect') VALUES (@QuestionID, @AnswerText, @IsCorrect)";
                        using (SQLiteCommand answerCommand = new SQLiteCommand(insertAnswerQuery, connection, transaction))
                        {
                            answerCommand.Parameters.AddWithValue("@QuestionID", questionID);
                            answerCommand.Parameters.AddWithValue("@AnswerText", answerText);
                            answerCommand.Parameters.AddWithValue("@IsCorrect", isCorrect);
                            answerCommand.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Тест успешно создан.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (SQLiteException ex)
            {
                transaction.Rollback();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Произошла ошибка при создании теста: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }

    static TestQuestion GetUserInputForQuestion()
    {
        Console.Write("Введите текст вопроса: ");
        string questionText = Console.ReadLine();

        List<string> answerOptions = new List<string>();
        Console.WriteLine("Введите варианты ответов (по одному в строке, для завершения введите пустую строку):");
        while (true)
        {
            string answer = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(answer))
            {
                break;
            }
            answerOptions.Add(answer);
        }

        Console.Write("Введите правильный ответ: ");
        string correctAnswer = Console.ReadLine();

        Console.Write("Введите количество баллов за этот вопрос: ");
        int points;
        while (!int.TryParse(Console.ReadLine(), out points) || points < 0)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Пожалуйста, введите положительное целое число для баллов: ");
            Console.ForegroundColor = ConsoleColor.White;
        }

        return new TestQuestion
        {
            QuestionText = questionText,
            AnswerOptions = answerOptions,
            CorrectAnswer = correctAnswer,
            Points = points
        };
    }

    static void TakeTest(SQLiteConnection connection)
    {        
        string fullName = Users.Username;

        List<Test> availableTests = GetAvailableTests(connection);
        if (availableTests.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Нет доступных тестов.");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }

        Console.WriteLine("Доступные тесты:");
        for (int i = 0; i < availableTests.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {availableTests[i].TestName}");
        }

        Console.Write("Выберите номер теста: ");
        if (!int.TryParse(Console.ReadLine(), out int testIndex) || testIndex < 1 || testIndex > availableTests.Count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Неверный выбор теста.");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }

        Test selectedTest = availableTests[testIndex - 1];
        List<Question> questions = GetQuestionsForTest(connection, selectedTest.TestID);

        int score = 0;

        foreach (Question question in questions)
        {
            Console.WriteLine($"Вопрос: {question.QuestionText}");
            List<Answer> answers = GetAnswersForQuestion(connection, question.QuestionID);

            for (int i = 0; i < answers.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {answers[i].AnswerText}");
            }

            Console.Write("Выберите номер правильного ответа: ");
            if (!int.TryParse(Console.ReadLine(), out int selectedAnswerIndex) || selectedAnswerIndex < 1 || selectedAnswerIndex > answers.Count)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный выбор ответа.");
                Console.ForegroundColor = ConsoleColor.White;
                return;
            }

            Answer selectedAnswer = answers[selectedAnswerIndex - 1];
            if (selectedAnswer.IsCorrect == 1)
            {
                score++;
            }
        }

        Console.WriteLine($"Тест завершен. Ваш результат: {score} из {questions.Count} правильных ответов.");

        SaveTestResult(connection, Users.Username, selectedTest.TestName, score);
    }

    static List<Test> GetAvailableTests(SQLiteConnection connection)
    {
        List<Test> tests = new List<Test>();
        string selectTestsQuery = "SELECT * FROM Tests";

        using (SQLiteCommand command = new SQLiteCommand(selectTestsQuery, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Test test = new Test
                    {
                        TestID = Convert.ToInt32(reader["TestID"]),
                        TestName = reader["TestName"].ToString(),
                    };
                    tests.Add(test);
                }
            }
        }

        return tests;
    }

    static List<Question> GetQuestionsForTest(SQLiteConnection connection, int testID)
    {
        List<Question> questions = new List<Question>();
        string selectQuestionsQuery = "SELECT * FROM Questions WHERE TestID = @TestID";

        using (SQLiteCommand command = new SQLiteCommand(selectQuestionsQuery, connection))
        {
            command.Parameters.AddWithValue("@TestID", testID);
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Question question = new Question
                    {
                        QuestionID = Convert.ToInt32(reader["QuestionID"]),
                        QuestionText = reader["QuestionText"].ToString(),
                    };
                    questions.Add(question);
                }
            }
        }

        return questions;
    }

    static List<Answer> GetAnswersForQuestion(SQLiteConnection connection, int questionID)
    {
        List<Answer> answers = new List<Answer>();
        string selectAnswersQuery = "SELECT * FROM Answers WHERE QuestionID = @QuestionID";

        using (SQLiteCommand command = new SQLiteCommand(selectAnswersQuery, connection))
        {
            command.Parameters.AddWithValue("@QuestionID", questionID);
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Answer answer = new Answer
                    {
                        AnswerID = Convert.ToInt32(reader["AnswerID"]),
                        AnswerText = reader["AnswerText"].ToString(),
                        IsCorrect = Convert.ToInt32(reader["IsCorrect"]),
                    };
                    answers.Add(answer);
                }
            }
        }

        return answers;
    }

    static void SaveTestResult(SQLiteConnection connection, string fullName, string testName, int score)
    {
        string insertResultQuery = "INSERT INTO 'TestResults' ('UserID', 'TestID', 'Score', 'TestDate') VALUES (@UserID, @TestID, @Score, @TestDate)";

        using (SQLiteCommand command = new SQLiteCommand(insertResultQuery, connection))
        {
            command.Parameters.AddWithValue("@UserID", GetUserID(connection, fullName));
            command.Parameters.AddWithValue("@TestID", GetTestID(connection, testName));
            command.Parameters.AddWithValue("@Score", score);
            command.Parameters.AddWithValue("@TestDate", DateTime.Now);
            command.ExecuteNonQuery();
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Результаты тестирования сохранены.");
        Console.ForegroundColor = ConsoleColor.White;
    }

    static int GetUserID(SQLiteConnection connection, string fullName)
    {
        string selectUserIDQuery = "SELECT UserID FROM Users WHERE Username = @Username";

        using (SQLiteCommand command = new SQLiteCommand(selectUserIDQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", Users.Username);
            object result = command.ExecuteScalar();

            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                return -1;
            }
        }
    }

    static int GetTestID(SQLiteConnection connection, string testName)
    {
        string selectTestIDQuery = "SELECT TestID FROM Tests WHERE TestName = @TestName";

        using (SQLiteCommand command = new SQLiteCommand(selectTestIDQuery, connection))
        {
            command.Parameters.AddWithValue("@TestName", testName);
            object result = command.ExecuteScalar();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                return -1;
            }
        }
    }

    static void AnalyzeTestResults(SQLiteConnection connection)
    {
        Console.Write("Введите ФИО пользователя: ");
        string fullName = Console.ReadLine();

        Console.Write("Введите название теста: ");
        string testName = Console.ReadLine();

        Console.Write("Введите баллы пользователя: ");
        if (!int.TryParse(Console.ReadLine(), out int score))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Неверный формат баллов.");
            Console.ForegroundColor = ConsoleColor.White;
            return;
        }

        double percentage = (double)score / GetTotalPointsForTest(connection, testName) * 100;

        Console.WriteLine($"ФИО пользователя: {fullName}");
        Console.WriteLine($"Название теста: {testName}");
        Console.WriteLine($"Баллы: {score}");
        Console.WriteLine($"Процент правильных ответов: {percentage}%");

        SaveTestAnalysis(connection, fullName, testName, score, percentage);
    }

    static int GetTotalPointsForTest(SQLiteConnection connection, string testName)
    {
        string selectTotalPointsQuery = "SELECT SUM(Points) FROM Questions WHERE TestID = (SELECT TestID FROM Tests WHERE TestName = @TestName)";

        using (SQLiteCommand command = new SQLiteCommand(selectTotalPointsQuery, connection))
        {
            command.Parameters.AddWithValue("@TestName", testName);
            object result = command.ExecuteScalar();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                return -1;
            }
        }
    }

    static void SaveTestAnalysis(SQLiteConnection connection, string fullName, string testName, int score, double percentage)
    {
        string insertAnalysisQuery = "INSERT INTO TestAnalysis (FullName, TestName, Score, Percentage, AnalysisDate) VALUES (@FullName, @TestName, @Score, @Percentage, @AnalysisDate)";

        using (SQLiteCommand command = new SQLiteCommand(insertAnalysisQuery, connection))
        {
            command.Parameters.AddWithValue("@FullName", fullName);
            command.Parameters.AddWithValue("@TestName", testName);
            command.Parameters.AddWithValue("@Score", score);
            command.Parameters.AddWithValue("@Percentage", percentage);
            command.Parameters.AddWithValue("@AnalysisDate", DateTime.Now);
            command.ExecuteNonQuery();
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Анализ результата тестирования сохранен.");
        Console.ForegroundColor = ConsoleColor.White;
    }

    static void ViewTestResults(SQLiteConnection connection)
    {
        Console.Write("Введите имя пользователя, чьи результаты вы хотите просмотреть: ");
        string username = Console.ReadLine();

        List<TestResult> results = GetTestResultsForUser(connection, username);

        if (results.Count > 0)
        {
            Console.WriteLine($"Результаты тестирования для пользователя {username}:");
            foreach (TestResult result in results)
            {
                Console.WriteLine($"Тест: {result.TestName}");
                Console.WriteLine($"Всего правильных ответов: {result.Score}");
                Console.WriteLine();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Результаты тестирования для пользователя {username} не найдены.");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    static List<TestResult> GetTestResultsForUser(SQLiteConnection connection, string username)
    {
        List<TestResult> results = new List<TestResult>();

        string selectResultsQuery = @"
            SELECT Tests.TestName, TestResults.Score
            FROM TestResults
            INNER JOIN Users ON TestResults.UserID = Users.UserID
            INNER JOIN Tests ON TestResults.TestID = Tests.TestID
            WHERE Users.Username = @Username
        ";

        using (SQLiteCommand command = new SQLiteCommand(selectResultsQuery, connection))
        {
            command.Parameters.AddWithValue("@Username", username);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string testName = reader["TestName"].ToString();
                    int score = Convert.ToInt32(reader["Score"]);

                    results.Add(new TestResult
                    {
                        TestName = testName,
                        Score = score
                    });
                }
            }
        }

        return results;
    }

    static void ListTestResultsForDate(SQLiteConnection connection, string date, StreamWriter writer)
    {
        string selectResultsQuery = @"
            SELECT Users.Username, Tests.TestName, TestResults.Score
            FROM TestResults
            INNER JOIN Users ON TestResults.UserID = Users.UserID
            INNER JOIN Tests ON TestResults.TestID = Tests.TestID
            WHERE strftime('%Y-%m-%d', TestResults.TestDate) = @Date
        ";

        using (SQLiteCommand command = new SQLiteCommand(selectResultsQuery, connection))
        {
            command.Parameters.AddWithValue("@Date", date);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string username = reader["Username"].ToString();
                    string testName = reader["TestName"].ToString();
                    int score = Convert.ToInt32(reader["Score"]);

                    writer.WriteLine($"Соискатель: {username}");
                    writer.WriteLine($"Тест: {testName}");
                    writer.WriteLine($"Результат: {score}");
                    writer.WriteLine();
                }
            }
        }
    }

    static void DisplayCategories(SQLiteConnection connection)
    {
        string selectCategoriesQuery = "SELECT CategoryID, CategoryName FROM TestCategories";
        using (SQLiteCommand command = new SQLiteCommand(selectCategoriesQuery, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int categoryID = Convert.ToInt32(reader["CategoryID"]);
                    string categoryName = reader["CategoryName"].ToString();
                    Console.WriteLine($"{categoryID}. {categoryName}");
                }
            }
        }
    }

    static void DisplayTestsInCategory(SQLiteConnection connection, int categoryID)
    {
        string selectTestsQuery = "SELECT TestName FROM Tests WHERE CategoryID = @CategoryID";
        using (SQLiteCommand command = new SQLiteCommand(selectTestsQuery, connection))
        {
            command.Parameters.AddWithValue("@CategoryID", categoryID);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string testName = reader["TestName"].ToString();
                    Console.WriteLine($"- {testName}");
                }
            }
        }
    }

    static List<string> GetJobTitles(SQLiteConnection connection)
    {
        string selectJobTitlesQuery = "SELECT CategoryName FROM TestCategories";
        List<string> jobTitles = new List<string>();

        using (SQLiteCommand command = new SQLiteCommand(selectJobTitlesQuery, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    string jobTitle = reader["CategoryName"].ToString();
                    jobTitles.Add(jobTitle);
                }
            }
        }

        return jobTitles;
    }

    static List<Applicant> GetApplicants(SQLiteConnection connection)
    {
        string selectApplicantsQuery = "SELECT UserID, Username FROM Users";
        List<Applicant> applicants = new List<Applicant>();
        using (SQLiteCommand command = new SQLiteCommand(selectApplicantsQuery, connection))
        {
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int userID = Convert.ToInt32(reader["UserID"]);
                    string username = reader["Username"].ToString();
                    applicants.Add(new Applicant { UserID = userID, Username = username });
                }
            }
        }

        return applicants;
    }

    static Dictionary<int, int> CalculateCompatibilityScores(SQLiteConnection connection, string jobTitle, List<Applicant> applicants)
    {
        Dictionary<int, int> compatibilityScores = new Dictionary<int, int>();

        int categoryID = GetCategoryIDForJobTitle(connection, jobTitle);

        List<int> testIDsInCategory = GetTestIDsInCategory(connection, categoryID);

        foreach (var applicant in applicants)
        {
            int totalCompatibilityScore = 0;

            foreach (var testID in testIDsInCategory)
            {
                int correctAnswersCount = GetCorrectAnswersCountForUser(connection, applicant.UserID, testID);
                totalCompatibilityScore += correctAnswersCount;
            }

            compatibilityScores[applicant.UserID] = totalCompatibilityScore;
        }

        return compatibilityScores;
    }

    static int GetCategoryIDForJobTitle(SQLiteConnection connection, string jobTitle)
    {
        string selectCategoryIDQuery = "SELECT CategoryID FROM TestCategories WHERE CategoryName = @JobTitle";

        using (SQLiteCommand command = new SQLiteCommand(selectCategoryIDQuery, connection))
        {
            command.Parameters.AddWithValue("@JobTitle", jobTitle);
            object result = command.ExecuteScalar();

            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            else
            {
                return -1; 
            }
        }
    }

    static List<int> GetTestIDsInCategory(SQLiteConnection connection, int categoryID)
    {
        string selectTestIDsQuery = "SELECT TestID FROM Tests WHERE CategoryID = @CategoryID";
        List<int> testIDs = new List<int>();

        using (SQLiteCommand command = new SQLiteCommand(selectTestIDsQuery, connection))
        {
            command.Parameters.AddWithValue("@CategoryID", categoryID);
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int testID = Convert.ToInt32(reader["TestID"]);
                    testIDs.Add(testID);
                }
            }
        }

        return testIDs;
    }

    static int GetCorrectAnswersCountForUser(SQLiteConnection connection, int userID, int testID)
    {
        string selectCorrectAnswersCountQuery = "SELECT COUNT(*) FROM TestResults " +
                                                 "INNER JOIN Answers ON TestResults.TestID = Answers.QuestionID " +
                                                 "WHERE TestResults.UserID = @UserID AND Answers.IsCorrect = 1 AND TestResults.TestID = @TestID";

        using (SQLiteCommand command = new SQLiteCommand(selectCorrectAnswersCountQuery, connection))
        {
            command.Parameters.AddWithValue("@UserID", userID);
            command.Parameters.AddWithValue("@TestID", testID);
            int correctAnswersCount = Convert.ToInt32(command.ExecuteScalar());
            return correctAnswersCount;
        }
    }
}