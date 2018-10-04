using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shpora.WordSearcher
{
    class GameNavigator
    {
        public static string server = "http://shpora.skbkontur.ru:8080";
        public static string authKey = "6b2a2faf-9372-4acc-8f03-68de5bca7ad8";

        public readonly List<string> foundWords = new List<string>();
        public readonly Stopwatch stopWatch;
        private char[,] screen = new char[7, 7];
        private bool widthFound = false;
        private int currentTop;
        private int currentLeft;
        private int currentRight;
        private int currentBot;
        private int globalRightSteps = 0;
        private int width = 0;
        private Dictionary<string, int> widthWords = new Dictionary<string, int>();
        private int prewordLevel = 0;
        private int maxPrewordLevel = 0;
        private int currentwidth;
        private int expires;
        private int repetitionsSuccessiveCount = 0;

        public GameNavigator()
        {
            stopWatch = new Stopwatch();
        }

        private void WordHunter()
        {
            prewordLevel = 0;
            while (AnalizeScreen() && (currentBot != 5 || currentLeft != 3))
            {          
                if (currentBot < 5)
                    GoUp(1);
                else if (currentBot > 5)
                    GoDown(1);

                if (currentLeft > 3)
                    GoRight(1);
                else if (currentLeft < 3)
                    GoLeft(1);
            }
            if (currentBot == 5 && currentLeft == 3)
            {
                GoUp(1);
                while (AnalizeScreen() && currentLeft != 3)
                    GoLeft(1);
                GoRight(3);

                while (AnalizeScreen() && currentTop != 0)
                    GoDown(1);
                LettersReader();
            }
        }

        private void LettersReader()
        {
            var letters = new List<string>();
            while (!FindRightBorder())
            {
                var letter = "";
                for (var i = 0; i < 7; i++)
                    for (var j = 0; j < 7; j++)
                        letter += screen[i, j];
                letters.Add(letter);
                GoRight(8);
            }
            BuildWord(letters);
            ReturnToLayer();
        }
        
        private void WordNotCorrect(List<string> letters, int index)
        {
            Console.WriteLine("Ошибка распознования слова");
            Console.WriteLine("index = " + index);

            for (var i = 0; i < letters.Count; i++)
            {
                Console.WriteLine(letters[i]);
            }

        }

        private void BuildWord(List<string> letters)
        {
            var newWord = "";
            for (var i = 0; i < letters.Count; i++)
            {
                if (!Leters.lang.ContainsKey(letters[i]))
                {
                    WordNotCorrect(letters, i);
                    return;
                }
                newWord += Leters.lang[letters[i]];
            }
            if (newWord != "")
                AddWord(newWord);
            else
                Console.WriteLine("Найденно пустое слово!");
        }

        private void AddWord(string word)
        {
            var distanceBetweenBirstAndLastMeeting = foundWords.Count - foundWords.IndexOf(word);
            if (widthFound &&
                foundWords.Count > repetitionsSuccessiveCount &&
                distanceBetweenBirstAndLastMeeting < foundWords.Count &&
                distanceBetweenBirstAndLastMeeting > width / 25)
            {
                repetitionsSuccessiveCount++;
                Console.WriteLine("Встречен не случайный повтор номер " + repetitionsSuccessiveCount + " из " + (width / 50 + 2));
            }
            if (!widthFound)
                SavedWidth(word);
            Console.WriteLine("Найдено! номер: " + foundWords.Count + " слово: " + word);
            foundWords.Add(word);
        }

        private bool FindRightBorder()
        {
            for (var j = 0; j < 7; j++)
            {
                if (screen[j, 0] == '1')
                    return false;
            }
            Console.WriteLine("Конец слова");
            return true;
        }

        private void SavedWidth(string word)
        {

            if (widthWords.ContainsKey(word))
            {
                widthFound = true;
                width = globalRightSteps - widthWords[word];
            }
            else
            {
                widthWords[word] = globalRightSteps;
            }

        }

        private void ReturnToLayer()
        {
            Console.WriteLine("возврат на уровень поиска " + prewordLevel);
            if (prewordLevel < 0)
            {
                GoDown(Math.Abs(prewordLevel));
            }
            else
            {
                maxPrewordLevel = Math.Max(maxPrewordLevel, prewordLevel);
                GoUp(prewordLevel);
                Console.WriteLine(maxPrewordLevel);
            }
        }

        private bool AnalizeScreen()
        {
            currentTop = 10;
            currentLeft = 10;
            currentBot = 0;
            currentRight = 0;
            var res = false;
            for (var i = 0; i < 7; i++)
            {
                for (var j = 0; j < 7; j++)
                {
                    if (screen[i, j] == '1')
                    {
                        currentTop = Math.Min(currentTop, i);
                        currentBot = Math.Max(currentBot, i);
                        currentLeft = Math.Min(currentLeft, j);
                        currentRight = Math.Max(currentRight, j);
                        res = true;
                    }
                }
            }
            return res;
        }

        private void SetScreen(StreamReader reader)
        {
            for (var i = 0; i < 7; i++)
            {
                var line = reader.ReadLine();
                for (var j = 0; j < 7; j++)
                {
                    screen[i, j] = line[j];
                }
            }
        }

        private void GoLeft(int count)
        {
            while (0 < count--)
            {
                currentwidth--;
                PostForMove("left");
            }
        }

        private void GoRight(int count)
        {
            while (0 < count--)
            {
                globalRightSteps++;
                currentwidth++;
                PostForMove("right");
            }
        }

        private void GoDown(int count)
        {
            while (0 < count--)
            {
                prewordLevel++;
                PostForMove("down");
            }
        }

        private void GoUp(int count)
        {
            while (0 < count--)
            {
                prewordLevel--;
                PostForMove("up");
            }
        }

        private void PostForMove(string dir)
        {
            var request = WebRequest.Create(server + "/task/move/" + dir);
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "token " + authKey;
            request.Method = "Post";

            new StreamWriter(request.GetRequestStream()).Dispose();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                SetScreen(new StreamReader(responseStream, Encoding.UTF8));
            }
        }

        public void Start()
        {
            stopWatch.Start();
            var request = WebRequest.Create(server + "/task/game/start");
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "token " + authKey;
            request.Method = "POST";

            new StreamWriter(request.GetRequestStream()).Dispose();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            using (Stream responseStream = response.GetResponseStream())
            {
                expires = int.Parse(response.Headers.Get("Expires"));
                Console.WriteLine(expires);
                SetScreen(new StreamReader(responseStream, Encoding.UTF8));
            }
            Findwidth();
        }

        private void Findwidth()
        {
            var stepsDownCount = 0;
            Console.WriteLine("Поиск первого слова");
            while (foundWords.Count == 0)
            {
                GoRight(1);
                if (stepsDownCount > 100)
                {
                    stepsDownCount = 0;
                    GoDown(8);
                }
                WordHunter();
                if (stopWatch.ElapsedMilliseconds / 1000 > expires - 150)
                    break;
            }
            Console.WriteLine("Поиск ширины");
            while (!widthFound)
            {
                GoRight(1);
                WordHunter();
                if (stopWatch.ElapsedMilliseconds / 1000 > expires - 150)
                    break;
            }
            Console.WriteLine("Ширина карты = " + width);
            WordSearcher();
        }

        private void WordSearcher()
        {
            currentwidth = 0;
            Console.WriteLine("Начинаем основной поиск");
            while (repetitionsSuccessiveCount < width / 50 + 2)
            {
                GoRight(8);
                WordHunter();
                if (currentwidth >= (width - 15))
                {
                    Console.WriteLine("Переходим на следующий уровень, длина текущего " + currentwidth + " из " + (width - 15) + "смкщение вниз на  " + (13 - maxPrewordLevel));
                    GoDown(13 - maxPrewordLevel);
                    maxPrewordLevel = 0;
                    currentwidth = 0;
                }
                if (stopWatch.ElapsedMilliseconds / 1000 > expires - 150)
                    break;
            }
            Console.WriteLine("Число повторов " + repetitionsSuccessiveCount + " >= среднее количество слов на одном уровне+2" + width / 50 + 2);
            SendWords();
        }

        private void SendWords()
        {
            foundWords.Sort((x1, x2) => x1.Length.CompareTo(x2.Length));
            var uniqueFoundWords = foundWords.Distinct().ToList();
            Console.WriteLine();
            uniqueFoundWords.ForEach(w => Console.WriteLine(w));
            WebRequest request = WebRequest.Create(server + "/task/words/");
            request.Method = "Post";
            var resultForSend = "[";
            for (var i = 0; i < uniqueFoundWords.Count; i++)
                resultForSend += i == uniqueFoundWords.Count - 1 ? "\"" + uniqueFoundWords[i] + "\"" : "\"" + uniqueFoundWords[i] + "\",";
            resultForSend += "]";
            byte[] byteArray = Encoding.UTF8.GetBytes(resultForSend);
            request.ContentType = "application/json";
            request.Headers["Authorization"] = "token " + authKey;
            request.ContentLength = byteArray.Length;
            using (Stream dataStream = request.GetRequestStream())
                dataStream.Write(byteArray, 0, byteArray.Length);
            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
                Console.WriteLine(reader.ReadToEnd());
            GetStats();
        }

        private void GetStats()
        {
            var requestg = WebRequest.Create(server + "/task/game/stats");
            requestg.ContentType = "application/json";
            requestg.Headers["Authorization"] = "token " + authKey;
            requestg.Method = "GET";

            HttpWebResponse responseg = requestg.GetResponse() as HttpWebResponse;
            using (Stream responseStream = responseg.GetResponseStream())
            {
                Console.WriteLine(responseg.StatusCode);
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                Console.WriteLine(reader.ReadToEnd());
            }
            Finish();
        }

        private void Finish()
        {
            var requestF = WebRequest.Create(server + "/task/game/finish");
            requestF.ContentType = "application/json";
            requestF.Headers["Authorization"] = "token " + authKey;
            requestF.Method = "Post";

            new StreamWriter(requestF.GetRequestStream()).Dispose();

            HttpWebResponse responseF = requestF.GetResponse() as HttpWebResponse;
            using (Stream responseStream = responseF.GetResponseStream())
            {
                Console.WriteLine(responseF.StatusCode);
                StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                Console.WriteLine(reader.ReadToEnd());
            }
            stopWatch.Stop();
            Console.WriteLine(stopWatch.Elapsed);
        }

        static void Main(string[] args)
        {
            //server = args[0];
            //authKey = args[1];
            var gameNavigator = new GameNavigator();
            gameNavigator.Start();
            Console.ReadLine();
        }
    }
}
