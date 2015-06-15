using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorGenetic
{
    public class Rider
    {
        public string Id { get; private set; }

        // Queue of floors to travel to
        public Queue<int> Destinations { get; private set; }
        // Elevator this dude is currently using (null if on landing somewhere)
        public Elevator Using { get; private set; }
        // Floor this dude is on (-1 if in-between floors)
        public int OnFloor { get; private set; }
        
        public Rider(string id)
        {
            this.Id = id;
            this.Destinations = new Queue<int>();
            this.OnFloor = 1;
        }

        public void Tick(Elevator[] cars)
        {
            //this dude has somewhere to go
            if (Destinations.Count > 0)
            {
                int dest = Destinations.Peek();
                //he's on some landing (not in elevator)
                if (Using == null)
                {
                    //pick fastest available elevator (ignore travel direction)
                    Elevator choice = cars.Where(c => c.Floor == this.OnFloor && !c.IsFull)
                        .FirstOrDefault();
                    
                    //load 'em up
                    if (choice != null)
                    {
                        choice.Load(this);
                        Using = choice;
                    }
                }
                else //inside elevator
                {
                    //at our destination
                    if (OnFloor == dest)
                    {
                        //get off lift
                        Using.OffLoad(this);
                        Using = null;

                        //dequeue destination
                        Destinations.Dequeue();
                    }
                }
            }
        }

        //Set OnFloor in seperate method so I don't mess this up accidentally..
        public void SetFloor(Elevator sender, int floor)
        {
            if (Using == null)
                throw new InvalidOperationException(String.Format(
                    "Rider {0} cannot move without an elevator!", this.Id));
            else if (Using != sender)
                throw new InvalidOperationException(String.Format(
                    "Rider {1} cannot be moved in a different elevator!", this.Id));
            else
                this.OnFloor = floor;
        }
    }
}
