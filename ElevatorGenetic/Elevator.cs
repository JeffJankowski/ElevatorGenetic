using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorGenetic
{
    public class Elevator
    {
        public string Id { get; private set; }
        public int TotalCapacity { get; private set; }
        public double Speed { get; private set; }
        public int TopFloor { get; set; }

        public bool IsFull { get { return _occupants.Count == TotalCapacity; } }
        private const double EPS = 0.000001;
        // Current floor elevator is on, returns -1 if in-between floors
        public int Floor
        { 
            get 
            { 
                return Math.Abs((int)(_location + 0.5) - _location) < EPS ? 
                    (int)(_location + 0.5) : -1; 
            } 
        }
        //Current direction of travel 
        public State Direction { get; private set; }


        private double _location; // fractional floor location
        private int _prevFloor = 0; //previously visited floor
        private HashSet<Rider> _occupants = new HashSet<Rider>(); //riders inside
        private int _targetFloor; // floor we're traveling to

        public Elevator(string id, int capacity, double speed, int startFloor)
        {
            this.Id = id;
            this.TotalCapacity = capacity;
            this.Speed = speed;

            this.Direction = State.IDLE;

            _location = startFloor;
            _targetFloor = startFloor;
        }

        public void Tick(Queue<int> brain)
        {
            //if we've arrived at our target, dequeue a new floor target
            if (_targetFloor == Floor)
            {
                if (brain.Count <= 0)
                    Direction = State.IDLE;
                else
                {
                    _targetFloor = brain.Dequeue();
                    Direction = (State)Math.Sign(_targetFloor - Floor);
                }
            }

            //move a tick
            _prevFloor = Floor;
            _location = _location + Speed * (int)Direction;

            //update rider location inside this lift
            if (Floor != _prevFloor)
            {
                foreach (Rider rider in _occupants)
                    rider.SetFloor(this, Floor);
            }

            if (Floor != -1 && (Floor > TopFloor || Floor < 1))
                throw new Exception("Magical flying elevator..");
        }

        public void Load(Rider rider) { _occupants.Add(rider); }
        public void OffLoad(Rider rider) { _occupants.Remove(rider); }


        public enum State
        {
            ASCENDING = 1,
            IDLE = 0,
            DESCENDING = -1
        }
    }
}
