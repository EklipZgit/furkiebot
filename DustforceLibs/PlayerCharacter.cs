using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayLibrary
{
    public interface IPlayerCharacter
    {
        int DashSpeed { get; }
        int RunSpeed { get; }

        int TerminalVelocity { get; }

        int AirChargeCount { get; }

        float GroundJumpVel { get; }

        float AirJumpVel { get; }

        float WallJumpVertVel { get; }

        float WallJumpHorizVel { get; }

        float SpikeJumpVertVel { get; }

        float SpikeJumpLeftFastHorizVel { get; }

        float SpikeJumpRightFastHorizVel { get; }

        float SpikeJumpLeftSlowHorizVel { get; }

        float SpikeJumpRightSlowHorizVel { get; }

        float SlantJumpVertVel { get; }

        float SlantJumpHorizVel { get; }

        int HeavyLength { get; }

        int HeavyFireFrame { get; }

        int HeavyCancelFrame { get; }

        int LightLength { get; }

        int LightFireFrame { get; }

        int LightCancelFrame { get; }

        int HangFrames { get; }
        float DownDashVel { get; }


        float Position { get; set; }
        float Velocity { get; set; }
        float Acceleration { get; set; }
        float Jerk { get; set; }
        int CurrentAirCharges { get; set; }
    }

    public class Dustman : IPlayerCharacter
    {
        public int DashSpeed { get { return 522; } }
        public int RunSpeed { get { return 522; } }

        public int TerminalVelocity { get { return 2304; } }

        public int AirChargeCount { get { return 1; } }

        public float GroundJumpVel { get { return -1; } }

        public float AirJumpVel { get { return -1; } }

        public float WallJumpVertVel { get { return -1; } }

        public float WallJumpHorizVel { get { return -1; } }

        public float SpikeJumpVertVel { get { return -1; } }

        public float SpikeJumpLeftFastHorizVel { get { return -1; } }

        public float SpikeJumpRightFastHorizVel { get { return -1; } }

        public float SpikeJumpLeftSlowHorizVel { get { return -1; } }

        public float SpikeJumpRightSlowHorizVel { get { return -1; } }

        public float SlantJumpVertVel { get { return -1; } }

        public float SlantJumpHorizVel { get { return -1; } }

        public int HeavyLength { get { return 49; } }//00ab00000000000000000000000000000000000000000aa9876500000000000000000000000000000000000000aaa9876543200 uncancelled. Might be 50

        public int HeavyFireFrame { get { return 22; } } // 1549 - 1571

        public int HeavyCancelFrame { get { return 19; } } //abb00000000000000000  == 20 so frame 19 ?

        public int LightLength { get { return -1; } }

        public int LightFireFrame { get { return 10; } } //level ends on 2021, heavy pressed on 2010 == 11th frame == 10

        public int LightCancelFrame { get { return 5; } }

        public int HangFrames { get { return 6; } }
        public float DownDashVel { get { return 800; } }


        public float Position { get; set; }
        public float Velocity { get; set; }
        public float Acceleration { get; set; }
        public float Jerk { get; set; }
        public int CurrentAirCharges { get; set; }
        
    }
}
