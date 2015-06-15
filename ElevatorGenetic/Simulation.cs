using GAF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorGenetic
{
    public class Simulation
    {
        //number of ticks before giving up on trying to deliver everybody
        private const int MAX_TICK = 2500;

        //number of floors
        public int Floors { get; private set; }
        //number of elevators
        public int Elevators { get { return _cars.Length; } }

        private Elevator[] _cars; //elevator array
        private HashSet<Rider> _riders; //rider array
        private Queue<KeyValuePair<Rider, Request>> _requestQueue; //rider request queue
        private int _tick = 0;
        private Queue<int> _brain; //chromosome contents (list of floor targets)

        
        public Simulation(int topFloor, Elevator[] cars, HashSet<Rider> riders, 
            Queue<KeyValuePair<Rider,Request>> reqs)
        {
            this.Floors = topFloor;
            _cars = cars;
            _riders = riders;
            _requestQueue = reqs;
        }

        public Result Run(Chromosome chrom)
        {
            //queue up control flow directions (genes)
            _brain = new Queue<int>();
            foreach (Gene gene in chrom.Genes)
                _brain.Enqueue((int)gene.RealValue);

            while (!isFinished() && _tick < MAX_TICK)
            {
                //dispatch all requests for this time tick
                while (_requestQueue.Count > 0 && _requestQueue.Peek().Value.Time == _tick)
                {
                    KeyValuePair<Rider, Request> kv = _requestQueue.Dequeue();
                    kv.Key.Destinations.Enqueue(kv.Value.ToFloor);
                }

                //resolve one step of simulation
                foreach (Rider rider in _riders)
                    rider.Tick(_cars);
                foreach (Elevator car in _cars)
                    car.Tick(_brain);

                _tick++;
            }

            // GA Fitness
            int unfulfilled = unfulfilledRequests();
            double fit;
            if (unfulfilled > 0) //gave up
            {
                //scale by number of requests missed (shrink value so as for less impact on fit)
                fit = (1.0 - (unfulfilled / 359.0)) / 10000.0;
            }
            else //completed all requests
            {
                //use tick for fitness (where each tick closer to 1000 increases weight)
                // fit = e^(-t/1000)
                fit = System.Math.Pow(System.Math.E, -(_tick / 1000.0));
            }
               
            ////naive fit: handled requests per second

            return new Result()
            {
                Tick = _tick,
                Fit = fit,
                Unfulfilled = unfulfilled,
                UsedActions = chrom.Genes.Count - _brain.Count
            };
        }

        public struct Result
        {
            public int Tick;
            public int Unfulfilled;
            public double Fit;
            public int UsedActions;
        }

        private bool isFinished() 
        { 
            //no more rider requests left to handle
            return _requestQueue.Count == 0 && unfulfilledRequests() == 0;
        }

        private int unfulfilledRequests()
        {
            //sum of pending requests of all riders
            return _riders.Sum(r => r.Destinations.Count);
        }
    }
}
