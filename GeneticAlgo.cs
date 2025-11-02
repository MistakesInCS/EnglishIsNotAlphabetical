namespace GeneticAlgo{
    public class GeneticOptimizer{
        public static char START = '!', END = '.';
        public static string ALPH = "abcdefghijklmnopqrstuvwxyz";
        public GeneticOptimizer(double[,] mat, int popSize = 100, int gens = 100, double mutaRate = 0.02, double cullRate = 0.5, int parents = 5){
            populationSize = popSize;
            generations = gens;
            mutationRate = mutaRate;
            scoreMatrix = mat;
            popToCull = (int)(populationSize * cullRate);
            maxParents = parents;
        }
        int populationSize, generations, popToCull, maxParents;
        double mutationRate;
        double[,] scoreMatrix;
        Random random = new();

        public (AlphabetOrder, double) RunSimulation(){
            List<AlphabetOrder> population = new(populationSize);
            for(int i = 0; i < populationSize; i++){
                population.Add(new AlphabetOrder(random));
            }

            for(int gen = 0; gen < generations; gen++){
                Repopulate(ref population);
                OutputStatus(ref population, gen);
            }

            // Reversed order because we want descending order
            population.Sort( (x,y) => GetFitness(y).CompareTo(GetFitness(x)) );
            AlphabetOrder bestOrder = population[0];
            double bestFitness = GetFitness(bestOrder);
            Console.WriteLine($"The best found order is ");
            bestOrder.PrintOrder();
            Console.WriteLine($"Its score is {bestFitness}");
            return (bestOrder, bestFitness);
        }

        void Repopulate(ref List<AlphabetOrder> population){
            // See above
            population.Sort( (x,y) => GetFitness(y).CompareTo(GetFitness(x)) );
            for(int index = populationSize - popToCull; index < populationSize; index++){
                int numParents = random.Next(0, maxParents);
                AlphabetOrder[] parentArr = new AlphabetOrder[numParents];
                for(int i = 0; i < numParents; i++){
                    parentArr[i] = population[random.Next(0, populationSize - popToCull)];
                }
                population[index] = new AlphabetOrder(random, mutationRate, parentArr);
            }
        }

        public double GetFitness(AlphabetOrder order){
            List<char> orderList = order.OrderToList();
            double fitness = 0;
            for(int row = 0; row < 26; row++){
                for(int col = 0; col < 26; col++){
                    if(col < row)
                        fitness -= scoreMatrix[orderList[row+1]-'a',orderList[col+1]-'a'];
                    else
                        fitness += scoreMatrix[orderList[row+1]-'a',orderList[col+1]-'a'];
                }
            }

            return fitness;
        }

        void OutputStatus(ref List<AlphabetOrder> population, int gen){
            Console.WriteLine($"Currently on generation {gen}. The top 5 scores are {GetFitness(population[0])}"
            +$", {GetFitness(population[1])}, {GetFitness(population[2])}, "
            +$"{GetFitness(population[3])}, {GetFitness(population[4])}");
        }

        public static double TestFitness(double[,] mat){
            double fitness = 0;
            for(int row = 0; row < 26; row++){
                for(int col = 0; col < 26; col++){
                    if(col < row)
                        fitness -= mat[row,col];
                    else
                        fitness += mat[row,col];
                }
            }

            return fitness;
        }
        
    }

    public class AlphabetOrder{
        public AlphabetOrder(Random random, double muta = 0.02, params AlphabetOrder[] parents){
            RefreshSets();
            alphabet = new Dictionary<char, LetterPointer>(28);
            foreach(char c in GeneticOptimizer.ALPH){
                alphabet.Add(c, new LetterPointer(c, c));
            }
            alphabet.Add(GeneticOptimizer.START, new LetterPointer(GeneticOptimizer.START, GeneticOptimizer.START));
            alphabet.Add(GeneticOptimizer.END, new LetterPointer(GeneticOptimizer.END, GeneticOptimizer.END));

            mutationRate = muta;
            
            // No parents, generate random order
            if(parents.Length == 0){
                GenRandomOrder(random);
                return;
            }
            // Else parents, inherit genes
            InheritGeneticOrder(random, parents);
            
        }
        public Dictionary<char, LetterPointer> alphabet;
        double mutationRate;

        HashSet<char> heads = new HashSet<char>();
        HashSet<char> tails = new HashSet<char>();

        public void RefreshSets(){
            foreach(char c in GeneticOptimizer.ALPH){
                heads.Add(c);
                tails.Add(c);
            }
            heads.Add(GeneticOptimizer.START);
            tails.Add(GeneticOptimizer.END);
        }

        void InheritGeneticOrder(Random random, params AlphabetOrder[] parents){
            while(heads.Count != 0){
                int iter = 0;
                while(true){
                    iter++;
                    char tail, head = heads.ElementAt(random.Next(0, heads.Count));
                    // Mutate
                    if(iter > 100 || random.NextDouble() < mutationRate){
                        tail = tails.ElementAt(random.Next(0, tails.Count));
                        TryAddOrder(head, tail);
                        break;
                    }
                    // Choose a random parent, and then choose its letter that comes after the head for the "gene"
                    tail = parents[random.Next(0, parents.Length)].alphabet[head].next;
                    if(!tails.Contains(tail))
                        break;
                    if(TryAddOrder(head, tail))
                        break;
                }
                //PrintRelations();
                
            }
        }

        public void GenRandomOrder(Random random){
            while(heads.Count != 0){
                // Choose a head and tail
                char head = heads.ElementAt(random.Next(0, heads.Count));
                char tail = tails.ElementAt(random.Next(0, tails.Count));

                TryAddOrder(head, tail);
            }
        }

        bool TryAddOrder(char head, char tail){
            if(alphabet[head].next == tail){
                // There would be a cycle from adding this order
                return false;
            }
            // Need to check if an order would be made that starts with START and ends with END too early
            if(alphabet[head].next == GeneticOptimizer.START 
                && alphabet[tail].prev == GeneticOptimizer.END
                && heads.Count != 1)
                return false;
            // 4 things need to be updated:
            // the next pointer of the previous head to the previous tail, 
            // the previous pointer of the previous tail to the previous head, 
            // the next pointer of the new head to the new tail, 
            // and the previous pointer of the new tail to the new head.
            // The new head is the previous pointer of the previous tail.
            char newHead = alphabet[tail].prev;
            // The new tail is the next pointer of the previous head.
            char newTail = alphabet[head].next;
            // Then update
            alphabet[head].next = tail;
            alphabet[tail].prev = head;
            alphabet[newHead].next = newTail;
            alphabet[newTail].prev = newHead;

            heads.Remove(head);
            tails.Remove(tail);
            
            return true;
        }

        public List<char> OrderToList(){
            List<char> order = new(28){GeneticOptimizer.START};
            char next = alphabet[GeneticOptimizer.START].next;
            
            while(next != GeneticOptimizer.END){
                order.Add(next);
                next = alphabet[next].next;
            }

            order.Add(GeneticOptimizer.END);

            return order;
        }

        public void PrintOrder()
        {
            List<char> order = OrderToList();
            Console.WriteLine(string.Join(" ", order));
        }

        public void PrintRelations(){
            HashSet<char> letters = [GeneticOptimizer.START, .. GeneticOptimizer.ALPH, GeneticOptimizer.END];
            List<char> print = new();

            while(letters.Count != 0){
                char next = letters.ElementAt(0);
                while(letters.Contains(next)){
                    //print.Add(next);
                    Console.Write(next);
                    letters.Remove(next);
                    next = alphabet[next].next;
                }   
                Console.Write(" | ");
            }
            Console.WriteLine();

            //Console.WriteLine(string.Join(" ",print));
        }

    }

    public class LetterPointer
    {
        public LetterPointer(char n, char p) { next = n; prev = p; }
        public char next, prev;
    }
    
    public class RandOrder : AlphabetOrder
    {
        public RandOrder(Random random) : base(random){
            RefreshSets();
            alphabet = new Dictionary<char, LetterPointer>(28);
            foreach(char c in GeneticOptimizer.ALPH){
                alphabet.Add(c, new LetterPointer(c, c));
            }
            alphabet.Add(GeneticOptimizer.START, new LetterPointer(GeneticOptimizer.START, GeneticOptimizer.START));
            alphabet.Add(GeneticOptimizer.END, new LetterPointer(GeneticOptimizer.END, GeneticOptimizer.END));
            
            GenRandomOrder(random);
            return;
        }
    }
}