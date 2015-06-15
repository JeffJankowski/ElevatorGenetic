using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorGenetic
{
    public struct Request
    {
        public int Time { get; private set; }
        public int FromFloor { get; private set; }
        public int ToFloor { get; private set; }

        public Request(int time, int from, int to) : this()
        {
            Time = time;
            FromFloor = from;
            ToFloor = to;
        }
    }
}
