using GAF;
using GAF.Operators;
using GAF.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorGenetic
{
    /* Daily Programmer Challege #218 - Hard
     * Elevator Scheduling
     * http://www.reddit.com/r/dailyprogrammer/comments/39ixxi/20150612_challenge_218_hard_elevator_scheduling/
     * 
     * 
     * NOTES:
     *  - Simulation is at 1-second granularity
     *  - Elevators possess all request history up to current time tick
     *  - Performance is based on total time to complete ALL requests
     *  - Requests can be erratic/odd
     *      -- eg. riders request same floor, riders change destination mid-ride, etc.
     *  - Conflicting requests (for an individual) should be filled in-order
     *  - Rider pickup is instantaneous
     *  - Riders all start on level 1, but do not end anywhere in particular
     *  - Riders do not migrate floors without an elevator
     * 
     * 
     * IMPLEMENTATION:
     *  - genetic algoritm for honing in on ideal parameters
     *      -- problem models Traveling Salesman, etc
     *      -- Genetic Algorithm Framework for .NET
     *  - let GA find optimal elevator moves
     *      -- essentially shortest path
     *      
     *  - pay no heed to rider wait time.. they have no feelings.
     *  - since no overhead to pickup, always grab waiting riders (if direction of travel)?
     *  
     */

    class ElevatorGenetic
    {
        #region Genetic Algorithm Parameters

        // GA population size (num of solutions)
        const int POP_SIZE = 1000;
        // size of GA chromosome (int array representing queue of floors for elevators to pick)
        const int ACTIONS = 200;

        // Genetic operators to create diveristy from one generation to the next
        // http://aiframeworks.net/gaf/#GeneticOperators
        static readonly IGeneticOperator[] GeneticOpertors = new IGeneticOperator[] 
        {
            new Crossover(.95)
            {
                CrossoverType = CrossoverType.DoublePoint,
                ReplacementMethod = ReplacementMethod.GenerationalReplacement
            },
            new SwapMutate(0.1),
            new Elite(5)
        };

        #endregion


        static void Main(string[] args)
        {
            Simulation sim = createSim();

            //create population of random chromosomes
            var population = new Population(POP_SIZE);
            for (int i = 0; i < POP_SIZE; i++)
                population.Solutions.Add(CreateChromosome(sim.Floors, ACTIONS));

            //set up GA
            var ga = new GeneticAlgorithm(population, CalculateFitness);
            ga.Operators.AddRange(GeneticOpertors);

            ga.OnGenerationComplete += ga_OnGenerationComplete;
            ga.OnRunComplete += ga_OnRunComplete;

            ga.Run(TerminateFunction);

            Console.ReadKey();
        }

        private static Simulation createSim()
        {
            using (StreamReader reader = new StreamReader("../../data/input.txt"))
            {
                //first line is number of cars, N
                int n = int.Parse(reader.ReadLine());

                //create Elevators out of next N lines
                Elevator[] cars = new Elevator[n];
                for (int i = 0; i < n; i++)
                {
                    string[] vals = reader.ReadLine().Split(' ');
                    cars[i] = new Elevator(vals[0], int.Parse(vals[1]), double.Parse(vals[2]),
                        int.Parse(vals[3]));
                }
                //sort by speed
                cars = cars.OrderByDescending(c => c.Speed).ToArray();

                //skip line with num requests, M
                reader.ReadLine();

                Dictionary<string, Rider> riders = new Dictionary<string, Rider>();
                //create M requests from remaining lines, and sort then by Time
                Queue<KeyValuePair<Rider, Request>> reqs = new Queue<KeyValuePair<Rider, Request>>();
                var reqQ = reader.ReadToEnd().Split('\n').Select(s =>
                {
                    string[] vals = s.Split(' ');

                    if (!riders.ContainsKey(vals[0]))
                        riders.Add(vals[0], new Rider(vals[0]));

                    Rider rider = riders[vals[0]];
                    return new KeyValuePair<Rider, Request>(rider, new Request(int.Parse(
                        vals[1]), int.Parse(vals[2]), int.Parse(vals[3])));
                }).OrderBy(kv => kv.Value.Time);
                foreach (KeyValuePair<Rider, Request> kv in reqQ)
                    reqs.Enqueue(kv);

                //find max floor
                int topFloor = reqQ.Max(kv => System.Math.Max(kv.Value.ToFloor, kv.Value.FromFloor));
                //set elevator floor range
                foreach (Elevator car in cars)
                    car.TopFloor = topFloor;

                //create simulation
                return new Simulation(topFloor, cars, new HashSet<Rider>(riders.Select(kv =>
                    kv.Value)), reqs);
            }
        }

        private static void ga_OnRunComplete(object sender, GaEventArgs e)
        {
            Console.WriteLine("--------------------------------------------------");
            logGeneration(e); 
        }

        private static void ga_OnGenerationComplete(object sender, GaEventArgs e)
        {
            logGeneration(e);
        }

        private static double _best = 0;
        private static void logGeneration(GaEventArgs e)
        {
            //get fittest population (solution)
            var fittest = e.Population.GetTop(1)[0];
            //run this pop and get sim result
            Simulation.Result res = createSim().Run(fittest);

            Console.WriteLine("Generation: {0}{1}", e.Generation, fittest.IsElite ? "*" : "");
            Console.WriteLine("Fitness:    {0:0.000000000}", fittest.Fitness);
            Console.WriteLine("Tick:       {0}", res.Tick);
            Console.WriteLine("Missed:     {0}", res.Unfulfilled);
            Console.WriteLine("Moves:      {0}\n", res.UsedActions);

            //log this queue of floor instructions if best so far
            if (res.Unfulfilled <= 0 && fittest.Fitness > _best)
            {
                _best = fittest.Fitness;
                using (StreamWriter sw = new StreamWriter("out.txt"))
                {
                    var chrom = fittest.Genes.Select(g => (int)g.RealValue);
                    sw.WriteLine(String.Join(", ", chrom));
                }
            }
        }

        private static double CalculateFitness(Chromosome chromosome)
        {
            return createSim().Run(chromosome).Fit;
        }

        private static bool TerminateFunction(Population population, int currentGeneration,
            long currentEvaluation)
        {
            return population.MaximumFitness > 0.32; //somewhere less than 1100 ticks
        }

        private static Random _rand = new Random();
        private static Chromosome CreateChromosome(int floors, int actions)
        {
            var chromosome = new Chromosome();

            //create a chromosome of genes (of length 'action') containing random ints (between 1 
            //  and 'floors')
            var moves = Enumerable.Range(0, actions).Select(r => _rand.Next(1, floors + 1));
            foreach (var action in moves)
                chromosome.Genes.Add(new Gene((int)action));

            return chromosome;
        }
    }

}
