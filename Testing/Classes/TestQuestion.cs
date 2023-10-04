using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Testing.Classes
{
    class TestQuestion
    {
        public string QuestionText { get; set; }
        public List<string> AnswerOptions { get; set; }
        public string CorrectAnswer { get; set; }
        public int Points { get; set; }
    }
}
