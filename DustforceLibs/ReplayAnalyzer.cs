using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ReplayLibrary
{
    public interface IWayPoint
    {
        /// <summary>Gets the distance to way point. Not absolute distance, this may depend if this is a vertical, horizontal, or combined waypoint.</summary>
        /// <returns></returns>
        double GetDistanceToWayPoint(double xPos, double yPos);


    }

    public class TreeNode : IComparable
    {
        public IEnumerable<TreeNode> NextStates
        {
            get 
            {
                if (_NextStates == null)
                    _NextStates = new List<TreeNode>(1);
                return _NextStates;
            }
            set 
            {
                _NextStates = value;
            }
        }
        public IEnumerable<TreeNode> PrevStates
        {
            get
            {
                if (_PrevStates == null)
                    _PrevStates = new List<TreeNode>(1);
                return _PrevStates;
            }
            set
            {
                _PrevStates = value;
            }
        }
        private IEnumerable<TreeNode> _NextStates;
        private IEnumerable<TreeNode> _PrevStates;
        public IRunEvent Event;
        public XYDirection RecentDirection;
        public Direction XDirection;
        public Direction YDirection;

        public TreeNode(IRunEvent theEvent)
        {
            Event = theEvent;
        }

        public int CompareTo(object obj)
        {
            if (typeof(TreeNode).IsInstanceOfType(obj))
            {
                TreeNode other = (TreeNode)obj;
                return Event.Frame - other.Event.Frame;
            }
            throw new Exception("not an IRunEvent");
        }
    }


    public interface IRunEvent
    {
        int Frame { get; set; }
        float GameTime { get; }
        double xVel { get; set; }
        double yVel { get; set; }
        double xPos { get; set; }
        double yPos { get; set; }
    }

    public class RunEvent : IRunEvent
    {
        public int Frame { get; set; }
        public double xVel { get; set; }
        public double yVel { get; set; }
        public double xPos { get; set; }
        public double yPos { get; set; }

        public float GameTime
        {
            get
            {
                int realFrame = Frame - 55;
                return realFrame / 60f;
            }
        }

        public RunEvent()
        {
        }
    }

    public interface IBoostEvent : IRunEvent
    {

    }

    public class SpawnEvent : RunEvent, IRunEvent
    {
        public SpawnEvent(SyncState spawnState)
        {
            xPos = spawnState.xPos;
            yPos = spawnState.yPos;
            xVel = spawnState.xSpeed;
            yVel = spawnState.ySpeed;
        }
    }

    public class GoalEvent : RunEvent, IRunEvent
    {

    }

    public class WallClimbEvent : RunEvent, IRunEvent
    {
        public WallAngle WallAngle;
    }

    public class CeilingRunEvent : RunEvent, IRunEvent
    {
        public CeilingAngle CeilingAngle;
    }

    public interface IJumpEvent : IRunEvent
    {

    }

    public class GroundJumpEvent : RunEvent, IJumpEvent
    {

    }

    public class LedgeJumpEvent : RunEvent, IJumpEvent
    {

    }

    public class AirJumpEvent : RunEvent, IJumpEvent
    {

    }

    public class WallJumpEvent : RunEvent, IJumpEvent
    {

    }

    public class DashJumpEvent : RunEvent, IJumpEvent
    {

    }

    public class LandEvent : RunEvent, IRunEvent
    {
        public FloorAngle FloorAngle;
    }

    public class GroundBoostEvent : RunEvent, IBoostEvent
    {

    }

    public class SlantBoostEvent : RunEvent, IBoostEvent
    {

    }

    public class SlopeBoostEvent : RunEvent, IBoostEvent
    {

    }

    public class SlopeSlideEvent : RunEvent, IBoostEvent
    {

    }

    public class DashEvent : RunEvent, IRunEvent
    {

    }

    public class DashEndEvent : RunEvent, IRunEvent
    {

    }

    public class DownDashEvent : RunEvent, IRunEvent
    {

    }

    public interface IAttackEvent : IRunEvent
    {

    }

    public class HeavyEvent : RunEvent, IAttackEvent
    {

    }

    public class LightEvent : RunEvent, IAttackEvent
    {

    }

    public class HeavyCancelDash : DashEvent, IRunEvent
    {

    }

    public class LightCancelDash : DashEvent, IRunEvent
    {

    }

    public class HeavyCancelJump : GroundJumpEvent, IRunEvent
    {

    }

    public class LightCancelJump : GroundJumpEvent, IRunEvent
    {

    }


    public class PositionAnalysis
    {
        public List<SyncState> SyncData = new List<SyncState>();
        public List<WaypointVector> Waypoints = new List<WaypointVector>();
        //public MovementSnapshot GetSnapshotAt(int frame)
        //{

        //}
    }


    public class WaypointVector
    {
        public Vector2 Pos;
        public Vector2 Vel;

        public int BaseFrame;
        public Vector2 Direction { get { return Vector2.Normalize(Vel); } }
    }





    public class ReplayAnalyzer
    {
        public ReplayEventData Input;
        public TreeNode Spawn;
        public TreeNode Goal;

        public InputEvent LastInputEvent;
        public SyncState LastSync;
        public TreeNode LastEvent;

        public MinHeap<TreeNode> FrontierNodes = new MinHeap<TreeNode>();
        public HashSet<TreeNode> VerifiedNodes = new HashSet<TreeNode>();
        public HashSet<TreeNode> ClosedNodes = new HashSet<TreeNode>();

        public ReplayAnalyzer(ReplayEventData inputData)
        {
            Input = inputData;
            SpawnEvent spawnEvent = new SpawnEvent(Input.OrderedSyncData[0]);
            Spawn = new TreeNode(spawnEvent);
            Spawn.RecentDirection = XYDirection.Undefined;

            FrontierNodes.Add(Spawn);

            while (FrontierNodes.Count > 0)
            {
                LastEvent = FrontierNodes.PopMin();
            }
        }
    }
}
