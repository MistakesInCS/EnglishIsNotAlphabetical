using Optimizer;
using GeneticAlgo;
using System.Text.Json;

namespace IsEnglishAlphabetical{
    class Program
    {
        static void Main()
        {
            UserFlow();
        }

        static void UserFlow()
        {
            Random random = new();
            while (true)
            {
                string fileLocation = "";
                while (!File.Exists(fileLocation))
                {
                    Console.WriteLine("Please input the location of your file of words to be analyzed. Each word should be on its own line.");
                    fileLocation = Console.ReadLine();
                }
                Console.WriteLine($"File found: {fileLocation}. Make your choice below.");
                FileIn wordFile = new FileIn(fileLocation);
                double[,] alphabetMatrix = wordFile.GetAlphabetMatrix(26);
                for (int i = 0; i < 26; i++) { alphabetMatrix[i, i] = 0; }

                string userInput = "-1";
                while (true)
                {
                    Console.WriteLine($"0 = Generate random orders, 1 = Generate orders using gradient descent, 2 = Generate orders using the genetic algorithm, 3 = Score an order given as a string");
                    userInput = Console.ReadLine();
                    if(userInput == "3")
                    {
                        UserScoreOrder(wordFile);
                    }
                    if(userInput == "0" || userInput == "1" || userInput == "2")
                    {
                        UserGenerateOrders(wordFile, userInput, random, alphabetMatrix);
                    }
                }


            }
        }

        static void UserScoreOrder(FileIn wordFile)
        {
            while (true)
            {
                Console.WriteLine("Write the order to be scored without blank spaces. Type 1 to return to the menu.");
                string orderToScore = Console.ReadLine();
                if (orderToScore == "1")
                {
                    return;
                }
                // Given order must be only letters in the alphabet and must be exactly 26 characters long
                if (orderToScore.All(c => FileIn.englishAlphabet.Contains(c)) && orderToScore.Length == 26)
                {
                    // Given order must not contain duplicates
                    for (int i = 0; i < orderToScore.Length - 1; i++)
                    {
                        for (int j = i + 1; j < orderToScore.Length; j++)
                        {
                            if (orderToScore[i] == orderToScore[j])
                            {
                                continue;
                            }
                        }
                    }
                    // Given order is valid
                    Console.WriteLine($"The score of the order {orderToScore} is {wordFile.GetTotalScore(orderToScore).Item2}");
                }
                else
                {
                    continue;
                }
            }
        }

        static void UserGenerateOrders(FileIn wordFile, string choice, Random random, double[,] alphabetMatrix)
        {
            while (true)
            {
                Console.WriteLine("Type back to return to the menu. Otherwise, make your selection.");
                Console.WriteLine("How many orders do you want to generate?");
                string input = Console.ReadLine();
                string newFileLoc = "";
                int num;
                if (input == "back")
                {
                    return;
                }
                if (!int.TryParse(input, out num))
                {
                    break;
                }
                double mom = 0;
                if (choice == "1")
                {
                    Console.WriteLine("Enter the momentum value you want to use. Type 0 for no momentum: ");
                    string myNum = Console.ReadLine();
                    if (!double.TryParse(myNum, out mom))
                    {
                        mom = 0;
                        break;
                    }
                }
                Console.WriteLine("Do you want them written to a file, or the console? 0 for file, 1 for console.");
                input = Console.ReadLine();
                if (input != "0" && input != "1")
                {
                    break;
                }
                if (input == "0")
                {
                    Console.WriteLine("Write the location and name of the file you want to write: ");
                    newFileLoc = Console.ReadLine();
                    newFileLoc = Path.GetFullPath(newFileLoc);
                }
                switch (choice)
                {
                    case "0":
                        UserTakeSamples(alphabetMatrix, num, "Rand", input, random, filePath: newFileLoc);
                        break;
                    case "1":
                        UserTakeSamples(alphabetMatrix, num, "GD", input, random, filePath: newFileLoc, momentum: mom);
                        break;
                    case "2":
                        UserTakeSamples(alphabetMatrix, num, "GA", input, random, filePath: newFileLoc);
                        break;
                }
            }
            
        }

        static void DisplayMatrix(double[,] alphabetMatrix, int matSize)
        {
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    Console.Write(alphabetMatrix[row, col] + " ");
                }
                Console.Write('\n');
            }
            return;
        }

        static void UserTakeSamples(double[,] alphabetMatrix, int sample_num, string method, string console, Random random, string filePath, double momentum = 0)
        {
            OrderList SampleList = new();
            switch (method)
            {
                case "GD":
                    for (int i = 0; i < sample_num; i++)
                    {
                        Console.WriteLine($"Working on gradient descent... currently on sample {i}");
                        Optimizer.Optimizer opt = new Optimizer.Optimizer();
                        double[,] alphabetMatrixSmall = opt.MatMulConst(alphabetMatrix, 0.001);
                        double[,] perm = opt.optimizeLoop(alphabetMatrixSmall, momentum: momentum);
                        double[,] perm2 = opt.GreedyValidPermutation(perm);
                        string GDString = opt.OutputOrder(perm2, false);
                        double GDScore = opt.getScore(alphabetMatrix, perm2);
                        SampleList.GDList.Add(new OrderScore(GDString, GDScore));
                    }
                    if (console == "0")
                    {
                        SampleList.NotStupidToFile(filePath);
                        Console.WriteLine($"Written to file {filePath}. Returning...");
                        return;
                    }
                    else
                    {
                        UserOutputSLConsole(SampleList);
                        return;
                    }
                case "GA":
                    for (int i = 0; i < sample_num; i++)
                    {
                        Console.WriteLine($"Working on the genetic algorithm... currently on sample {i}");
                        GeneticOptimizer optim = new(alphabetMatrix, popSize: 1500, gens: 130, mutaRate: 0.05, cullRate: 0.8, parents: 10);
                        (AlphabetOrder GAOrder, double GAScore) = optim.RunSimulation();
                        string GAString = string.Join("", GAOrder.OrderToList());
                        GAString = GAString[1..^1];
                        SampleList.GDList.Add(new OrderScore(GAString, GAScore));
                    }
                    if (console == "0")
                    {
                        SampleList.NotStupidToFile(filePath);
                        Console.WriteLine($"Written to file {filePath}. Returning...");
                        return;
                    }
                    else
                    {
                        UserOutputSLConsole(SampleList);
                        return;
                    }
                case "Rand":
                    for (int i = 0; i < sample_num; i++)
                    {
                        Console.WriteLine($"Working on random samples... currently on sample {i}");
                        GeneticOptimizer optim = new(alphabetMatrix, popSize: 1500, gens: 130, mutaRate: 0.05, cullRate: 0.8, parents: 10);
                        RandOrder GAOrder = new(random);
                        double GAScore = optim.GetFitness(GAOrder);
                        string GAString = string.Join("", GAOrder.OrderToList());
                        GAString = GAString[1..^1];
                        SampleList.GDList.Add(new OrderScore(GAString, GAScore));
                    }
                    if (console == "0")
                    {
                        SampleList.NotStupidToFile(filePath);
                        Console.WriteLine($"Written to file {filePath}. Returning...");
                        return;
                    }
                    else
                    {
                        UserOutputSLConsole(SampleList);
                        return;
                    }
                default:
                    break;
            }
        }

        static void UserOutputSLConsole(OrderList sl)
        { 
            foreach(OrderScore score in sl.GDList)
            {
                Console.WriteLine($"Order: {score.Order} Score: {score.Score}");
            }
        }

        static OrderList TakeSamples(double[,] alphabetMatrix, int sample_num)
        {
            OrderList SampleList = new();
            Random random = new();
            for (int i = 0; i < sample_num; i++)
            {
                Optimizer.Optimizer opt = new Optimizer.Optimizer();
                double[,] alphabetMatrixSmall = opt.MatMulConst(alphabetMatrix, 0.001);
                double[,] perm = opt.optimizeLoop(alphabetMatrixSmall);
                double[,] perm2 = opt.GreedyValidPermutation(perm);
                string GDString = opt.OutputOrder(perm2, false);
                double GDScore = opt.getScore(alphabetMatrix, perm2);

                /*GeneticOptimizer optim = new(alphabetMatrix, popSize: 1500, gens: 130, mutaRate: 0.05, cullRate: 0.8, parents: 10);
                //(AlphabetOrder GAOrder, double GAScore) = optim.RunSimulation();
                RandOrder GAOrder = new(random);
                double GAScore = optim.GetFitness(GAOrder);
                string GAString = string.Join("", GAOrder.OrderToList());
                GAString = GAString[1..^1];*/

                SampleList.GDList.Add(new OrderScore(GDString, GDScore));
                //SampleList.GAList.Add(new OrderScore(GAString, GAScore));
            }

            return SampleList;
        }
        
    }

    public class OrderList
    {
        public List<OrderScore> GDList { get; set; } = new();
        public List<OrderScore> GAList { get; set; } = new();
        
        public void NotStupidToFile(string file)
        {
            File.WriteAllText(file, JsonSerializer.Serialize(GDList));
        }
    }

    public class OrderScore(string o, double s)
    {
        public string Order { get; init; } = o;
        public double Score { get; init; } = s;
    }

    public class FileIn
    {
        public FileIn(string s = """words.txt""")
        {
            words = new List<(string, int)>();
            filePath = s;
            GetWords();
        }
        public List<(string, int)> words;
        string filePath;
        static public string englishAlphabet = "abcdefghijklmnopqrstuvwxyz";
        public int total2grams = 0;

        public double[,] GetAlphabetMatrix(int matSize)
        {
            IEnumerable<string> lines = File.ReadLines(filePath);

            double[,] alphabetMatrix = new double[matSize, matSize];
            //int total2grams = 0;

            foreach (string line in lines)
            {
                if (line.Length > 1 && line.All(c => englishAlphabet.Contains(c)))
                {
                    for (int i = 0; i < line.Length - 1; i++)
                    {
                        total2grams++;
                        alphabetMatrix[char.ToLower(line[i]) - 'a', char.ToLower(line[i + 1]) - 'a']++;
                    }
                }
            }

            return alphabetMatrix;
        }

        public int GetScore(string wordToScore)
        {
            if (wordToScore.Length <= 1)
                return 0;

            int score = 0;
            for (int i = 0; i < wordToScore.Length - 1; ++i)
            {
                // -= because CompareTo returns a negative number if the argument  comes after the base
                score -= Math.Sign(wordToScore[i].CompareTo(wordToScore[i + 1]));
            }
            return score;
        }

        public List<(string, int)> GetWords()
        {
            words = new List<(string, int)>();

            IEnumerable<string> lines = File.ReadLines(filePath);

            (bool, string, int) wordToValidate;

            foreach (string line in lines)
            {
                wordToValidate = returnValidWord(line);
                if (wordToValidate.Item1)
                    words.Add((wordToValidate.Item2, wordToValidate.Item3));
            }

            return words;
        }

        public (bool, string, int) returnValidWord(string wordToValidate)
        {
            return (wordToValidate.All(c => englishAlphabet.Contains(c)) && wordToValidate.Length > 1,
            wordToValidate.ToLower(),
            GetScore(wordToValidate.ToLower()));
        }

        public (int, double) GetTotalScore(string alph)
        {
            int twoGramsInOrder = 0;
            double averageWordScore = 0;
            Dictionary<char, int> alphabet = new();

            for (int i = 0; i < alph.Length; i++)
            {
                alphabet.Add(alph[i], i);
            }

            foreach ((string, int) word in words)
            {
                double score = 0;
                for (int i = 0; i < word.Item1.Length - 1; i++)
                {
                    if (alphabet[word.Item1[i]] - alphabet[word.Item1[i + 1]] < 0)
                    {
                        // Characters in order
                        ++twoGramsInOrder;
                        ++score;
                    }
                    else if (alphabet[word.Item1[i]] - alphabet[word.Item1[i + 1]] > 0)
                    {
                        // Characters not in order
                        --score;
                    }
                }
                //score /= word.Item1.Length;
                averageWordScore += score;
            }

            //averageWordScore /= words.Count;
            return (twoGramsInOrder, averageWordScore);
        }

        public List<(double, double, char)> NumberOfStartEnd()
        {
            List<(double, double, char)> alphabetScores = Enumerable.Repeat((0.0, 0.0, 'a'), 26).ToList();

            foreach ((string, int) word in words)
            {
                alphabetScores[word.Item1[0] - 'a'] = (alphabetScores[word.Item1[0] - 'a'].Item1 + 1, alphabetScores[word.Item1[0] - 'a'].Item2, alphabetScores[word.Item1[0] - 'a'].Item3);
                alphabetScores[word.Item1[word.Item1.Length - 1] - 'a'] = (alphabetScores[word.Item1[word.Item1.Length - 1] - 'a'].Item1, alphabetScores[word.Item1[word.Item1.Length - 1] - 'a'].Item2 + 1, alphabetScores[word.Item1[word.Item1.Length - 1] - 'a'].Item3);
            }
            for(char c = 'a'; c <= 'z'; c++)
            {
                alphabetScores[c - 'a'] = (alphabetScores[c - 'a'].Item1, alphabetScores[c - 'a'].Item2, c);
            }

            return alphabetScores;
        }

        public (string, string, string) OrdersFromNoSE(List<(double, double, char)> alph)
        {
            string order1 = "", order2 = "", order3 = "";
            // b compared to a since we want the highest count first
            alph.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            foreach (var tuple in alph)
            {
                order1 += tuple.Item3;
            }
            // Inverted order since we want most common last to be last and not first
            alph.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            foreach (var tuple in alph)
            {
                order2 += tuple.Item3;
            }
            // Item1/Item2 is the ratio of occurances as the first letter to occurances as the last, then we sort in desc
            alph.Sort((a, b) => (b.Item1 / b.Item2).CompareTo(a.Item1 / a.Item2));
            foreach (var tuple in alph)
            {
                order3 += tuple.Item3;
            }
            return (order1, order2, order3);
        }

        public void AwesomeManualOrders()
        {
            var startEndCounts = NumberOfStartEnd();
            for (char c = 'a'; c <= 'z'; c++) { Console.Write($"| {c} "); }
            Console.WriteLine();
            for (int i = 0; i < 26; i++) { Console.Write($"| {startEndCounts[i].Item1} "); }
            Console.WriteLine();
            for (int i = 0; i < 26; i++) { Console.Write($"| {startEndCounts[i].Item2} "); }
            Console.WriteLine();
            for (int i = 0; i < 26; i++) { Console.Write($"| {Math.Round(startEndCounts[i].Item1 / startEndCounts[i].Item2, 2)} "); }
            Console.WriteLine();
            var newOrders = OrdersFromNoSE(startEndCounts);
            Console.WriteLine($"Order by most common as first: {newOrders.Item1} , score: {GetTotalScore(newOrders.Item1)}");
            Console.WriteLine($"Order by least common as last: {newOrders.Item2} , score: {GetTotalScore(newOrders.Item2)}");
            Console.WriteLine($"Order by ratio of most common as first to most common as last: {newOrders.Item3} , score: {GetTotalScore(newOrders.Item3)}");
        }
        
        public void OrderFromAvgPos()
        {
            List<(double, double, char)> alphabetScores = Enumerable.Repeat((0.0, 0.0, 'a'), 26).ToList();

            foreach ((string, int) word in words)
            {
                for(int i = 0; i < word.Item1.Length; i++)
                {
                    alphabetScores[word.Item1[i] - 'a'] = (alphabetScores[word.Item1[i] - 'a'].Item1 + ((double)i)/word.Item1.Length, alphabetScores[word.Item1[i] - 'a'].Item2 + 1, alphabetScores[word.Item1[i] - 'a'].Item3);
                }
                
            }
            for (char c = 'a'; c <= 'z'; c++)
            {
                alphabetScores[c - 'a'] = (alphabetScores[c - 'a'].Item1, alphabetScores[c - 'a'].Item2, c);
            }

            for (char c = 'a'; c <= 'z'; c++) { Console.Write($"| {c} "); }
            Console.WriteLine();
            for (int i = 0; i < 26; i++) { Console.Write($"| {Math.Round(alphabetScores[i].Item1 / alphabetScores[i].Item2, 2)} "); }
            Console.WriteLine();

            alphabetScores.Sort((a, b) => (a.Item1 / a.Item2).CompareTo(b.Item1 / b.Item2));

            string order1 = "";
            foreach (var tuple in alphabetScores)
            {
                order1 += tuple.Item3;
            }

            Console.WriteLine($"Order: {order1} , score: {GetTotalScore(order1)}");
        }
    }
}

