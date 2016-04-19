using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReplayLibrary
{

    public struct FlagIndexer
    {
        private int _bitField;
        public bool this[int flag]
        {
            get
            {
                return (_bitField & flag) > 0;
            }
            set
            {
                if (value)
                    _bitField |= flag;
                else
                    _bitField &= ~flag;
            }
        }
    }

    public enum Character
    {
        Dustman = 0,
        Dustgirl = 1,
        Dustkid = 2,
        Dustworth = 3,
    }

    public enum Category
    {
        Score,
        Time,
    }

    public enum HorizDirState
    {
        Left = 0,
        Neutral = 1,
        Right = 2,
        Both = 3,
    }
    public enum VerticalDirState
    {
        Up = 0,
        Neutral = 1,
        Down = 2,
        Both = 3,
    }

    public interface IInputEvent
    {
        int Frame { get; set; }
        float GameTime { get; }
        string Notes { get; set; }
        SyncState LastSyncPosition { get; set; }
        InputEvent NextEvent { get; set; }
        SyncState NextSyncPosition { get; set; }
        InputEvent PrevEvent { get; set; }
    }

    public class InputEvent : IInputEvent
    {
        public int Frame { get; set; }
        public float GameTime
        {
            get
            {
                int realFrame = Frame - 55;
                return realFrame / 60f;
            }
        }
        private string _notes = null;
        public string Notes
        {
            get
            {
                if (_notes == null)
                    return "";
                return _notes;
            }
            set
            {
                _notes = value;
            }
        }

        protected InputEvent _prevEvent;
        public InputEvent PrevEvent
        {
            get
            {
                return _prevEvent;
            }
            set
            {
                _prevEvent = value;
            }
        }
        protected InputEvent _nextEvent;
        public InputEvent NextEvent
        {
            get
            {
                return _nextEvent;
            }
            set
            {
                _nextEvent = value;
            }
        }
        public SyncState LastSyncPosition { get; set; }
        public SyncState NextSyncPosition { get; set; }

        public InputEvent(int frame)
        {
            Frame = frame;
            Notes = null;
        }
    }

    public interface IMultiFrameInputEvent : IInputEvent
    {
        InputEvent EndEvent { get; set; }
        int Duration { get; }
    }

    public class MultiFrameInputEvent : InputEvent, IMultiFrameInputEvent
    {
        protected InputEvent _endEvent;
        public InputEvent EndEvent
        {
            get
            {
                return _endEvent;
            }
            set
            {
                _endEvent = value;
            }
        }

        public virtual int Duration
        {
            get
            {
                if (EndEvent != null)
                    return EndEvent.Frame - Frame;
                return -1;
            }
        }

        public MultiFrameInputEvent(int frame)
            : base(frame)
        {

        }
    }

    public interface ISingleFrameInputEvent : IInputEvent
    {

    }

    public interface IReleaseInputEvent<T> : ISingleFrameInputEvent
        where T : IMultiFrameInputEvent
    {
        T ReleasingEvent { get; set; }
    }




    public class LightInputEvent : MultiFrameInputEvent, IMultiFrameInputEvent
    {
        public const int GROUND = 4;
        public const int BUFFERED_GROUND = 8;
        public const int AIR = 16;
        public const int BUFFERED_AIR = 32;
        public const int AIR_BACK_LIGHT = 128;

        public int TriggeredFrame;
        public int ReleasedFrame;
        public int TotalHeldFrameCount;
        public bool PossiblyUnbuffered = false;
        public FlagIndexer Possible = new FlagIndexer();

        public Direction HorizDir;
        public Direction VertDir;
        public LightInputEvent(int pressedFrame, int triggeredFrame, int releasedFrame, Direction horizDir, Direction vertDir)
            : base(pressedFrame)
        {
            HorizDir = horizDir;
            VertDir = vertDir;
            TotalHeldFrameCount = releasedFrame - pressedFrame;
            ReleasedFrame = releasedFrame;
            TriggeredFrame = triggeredFrame;
        }

        public override int Duration
        {
            get { return (TriggeredFrame > ReleasedFrame ? TriggeredFrame : ReleasedFrame) - Frame; }
        }
    }


    public class HeavyInputEvent : MultiFrameInputEvent, IMultiFrameInputEvent
    {
        public const int GROUND = 4;
        public const int BUFFERED_GROUND = 8;
        public const int AIR = 16;
        public const int BUFFERED_AIR = 32;
        public const int AIR_BACK_LIGHT = 128;

        public int TriggeredFrame;
        public int ReleasedFrame;
        public int TotalHeldFrameCount;
        public bool PossiblyUnbuffered = false;
        public FlagIndexer Possible = new FlagIndexer();

        public Direction HorizDir;
        public Direction VertDir;
        public HeavyInputEvent(int pressedFrame, int triggeredFrame, int releasedFrame, Direction horizDir, Direction vertDir)
            : base(pressedFrame)
        {
            HorizDir = horizDir;
            VertDir = vertDir;
            TotalHeldFrameCount = releasedFrame - pressedFrame;
            ReleasedFrame = releasedFrame;
            TriggeredFrame = triggeredFrame;
        }

        public override int Duration
        {
            get { return (TriggeredFrame > ReleasedFrame ? TriggeredFrame : ReleasedFrame) - Frame; }
        }
    }




    public class JumpInputEvent : MultiFrameInputEvent, IMultiFrameInputEvent
    {
        public const int GROUND = 4;
        public const int BUFFERED_GROUND = 8;
        public const int AIR = 16;
        public const int BUFFERED_AIR = 32;
        public const int SHORT_HOP = 64;
        public const int LEDGE_JUMP = 128;
        public const int VERT_WALL = 256;
        public const int UP_SLANT_WALL = 512;
        public const int DOWN_SLANT_WALL = 1024;
        public const int SLOPE_JUMP_45 = 2048;
        public const int AIR_DASH_JUMP = 4096;
        public const int AIR_MOMENTUM_CANCEL = 8192;

        public int TriggeredFrame;
        public int ReleasedFrame;
        public int TotalHeldFrameCount;
        public bool PossiblyUnbuffered = false;
        public FlagIndexer Possible = new FlagIndexer();

        public Direction HeldDir;
        public JumpInputEvent(int pressedFrame, int triggeredFrame, int releasedFrame, Direction heldDir)
            : base(pressedFrame)
        {
            TotalHeldFrameCount = releasedFrame - pressedFrame;
            ReleasedFrame = releasedFrame;
            TriggeredFrame = triggeredFrame;
            HeldDir = heldDir;
        }

        public override int Duration
        {
            get { return ReleasedFrame - Frame; }
        }
    }


    public class DashInputEvent : InputEvent, ISingleFrameInputEvent
    {
        public bool PossibleDowndash = false;
        public bool DashTypeBySyncData = false;

        public Direction HeldDir;
        public DashInputEvent(int frame, Direction heldDir)
            : base(frame)
        {
            HeldDir = heldDir;
        }
    }
    public class DownDashInputEvent : DashInputEvent, ISingleFrameInputEvent
    {
        public DownDashInputEvent(int frame, Direction heldDir)
            : base(frame, heldDir)
        {

        }
    }

    public class DirInputEvent : InputEvent, IMultiFrameInputEvent
    {
        public Direction Dir;
        public DirInputEvent DirPreviousEvent;
        public DirInputEvent DirNextEvent;
        public InputEvent EndEvent
        {
            get
            {
                return DirNextEvent;
            }
            set
            {
                throw new Exception("Setting not supported.");
            }
        }

        public int Duration
        {
            get
            {
                if (DirNextEvent != null)
                    return DirNextEvent.Frame - Frame;
                return -1;
            }
        }

        public DirInputEvent(int frame, Direction dir)
            : base(frame)
        {
            Dir = dir;
        }
    }


    public class HorizInputEvent : DirInputEvent, IMultiFrameInputEvent
    {
        public HorizInputEvent(int frame, Direction dir)
            : base(frame, dir)
        {
        }
    }


    public class VertInputEvent : DirInputEvent, IMultiFrameInputEvent
    {
        public VertInputEvent(int frame, Direction dir)
            : base(frame, dir)
        {
        }
    }

    public enum Direction
    {
        Left,
        Right,
        Up,
        Down,
        Neutral,
        Undefined,
    }


    public enum XYDirection
    {
        Left,
        Right,
        Up,
        Down,
        Neutral,
        Undefined,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
    }

    public enum JumpState
    {
        Released,
        HeldAnimation,
        HeldPost,
    }

    public enum CeilingAngle
    {
        Horizontal,
        SlantDownLeft,
        SlantDownRight,
        DownLeft,
        DownRight,
    }

    public enum WallAngle
    {
        Vertical,
        UpLeft,
        DownLeft,
        UpRight,
        DownRight
    }

    public enum FloorAngle
    {
        Horizontal,
        SlantDownLeft,
        SlantDownRight,
        SlantUpLeft,
        SlantUpRight,
    }


    public class SyncState
    {
        private int _frame;
        private double _xPos;
        private double _yPos;
        private double _xSpeed;
        private double _ySpeed;
        public int Frame { get { return _frame; } }
        public float GameTime
        {
            get
            {
                int realFrame = Frame - 55;
                return realFrame / 60f;
            }
        }
        public double xPos { get { return _xPos; } }
        public double yPos { get { return _yPos; } }
        public double xSpeed { get { return _xSpeed; } }
        public double ySpeed { get { return _ySpeed; } }

        public SyncState PrevSyncState { get; set; }
        public SyncState NextSyncState { get; set; }

        public static SyncState CreateFromEntityFrame(EntityFrame entityFrame)
        {
            SyncState pos = new SyncState();
            pos._frame = entityFrame.time;
            pos._xPos = entityFrame.xpos * 0.1;
            pos._yPos = entityFrame.ypos * 0.1;
            pos._xSpeed = entityFrame.xspeed * 0.01;
            pos._ySpeed = entityFrame.yspeed * 0.01;
            return pos;
        }
    }


    public class ReplayEventData
    {
        public RawReplayData RawData;
        public string PlayerName { get; set; }
        public string MapName { get; set; }

        public List<InputEvent> OrderedEvents;
        public List<SyncState> OrderedSyncData;

        public ReplayEventData(RawReplayData rawData, List<InputEvent> orderedEvents, List<SyncState> orderedSyncData)
        {
            RawData = rawData;
            OrderedEvents = orderedEvents;
            OrderedSyncData = orderedSyncData;
            PlayerName = RawData.username;
            MapName = RawData.header.level;
        }
    }

    public class EventLoader
    {
        private const int HEAVY_AIM_FRAMES = 3;

        private RawReplayData RawData;
        private ReplayEventData newData;
        private int numFrames = 0;

        private int i;

        private string horizInputs;
        private string vertInputs;
        private string jumpInputs;
        private string dashInputs;
        private string downDashInputs;
        private string lightInputs;
        private string heavyInputs;


        HorizInputEvent lastHorizEvent = null;
        VertInputEvent lastVertEvent = null;
        JumpInputEvent lastJumpEvent = null;
        DashInputEvent lastDashEvent = null;
        DashInputEvent lastDownDashEvent = null;
        LightInputEvent lastLightEvent = null;
        HeavyInputEvent lastHeavyEvent = null;

        InputEvent lastEvent = null;

        IPlayerCharacter player;

        public List<InputEvent> OrderedEvents;
        public List<SyncState> OrderedSyncData;

        public ReplayEventData LoadReplay(string id)
        {
            RawData = DustkidAPI.GetReplayData(id);

            LoadData();

            newData = new ReplayEventData(RawData, OrderedEvents, OrderedSyncData);
            return newData;
        }

        private void LoadData()
        {
            var inputs = RawData.inputs;
            numFrames = 0;
            for (int j = 0; j < inputs.Count; j++)
                if (numFrames < inputs[j].Length) numFrames = inputs[j].Length;

            horizInputs = inputs[0];
            vertInputs = inputs[1];
            jumpInputs = inputs[2];
            dashInputs = inputs[3];
            downDashInputs = inputs[4];
            lightInputs = inputs[5];
            heavyInputs = inputs[6];


            lastHorizEvent = null;
            lastVertEvent = null;
            lastJumpEvent = null;
            int jumpCompleteTo = 0;
            lastDashEvent = null;
            lastDownDashEvent = null;
            lastLightEvent = null;
            int lightCompleteTo = 0;
            lastHeavyEvent = null;
            int heavyCompleteTo = 0;

            player = GetPlayerObject(GetCharacter());

            lastEvent = null;

            OrderedEvents = new List<InputEvent>();
            OrderedSyncData = new List<SyncState>();

            i = 55;

            for (; i < numFrames; i++)
            {
                Direction vertDir = GetVertDir(i);
                Direction horizDir = GetHorizDir(i);
                #region Horizontal Directional
                if (i < horizInputs.Length && ChangedOrFirstNotDefault(lastHorizEvent, horizInputs, '1'))
                {
                    HorizInputEvent newHorizEvent = new HorizInputEvent(i, horizDir);
                    if (horizDir == Direction.Undefined)
                        newHorizEvent.Notes += "Undefined: horizInputs[" + i + "] == " + horizInputs[i];
                    if (lastHorizEvent != null)
                        LinkDirEvents(lastHorizEvent, newHorizEvent);
                    AddNewestEvent(newHorizEvent);
                    lastHorizEvent = newHorizEvent;
                }
                #endregion
                #region Vertical Directional
                if (i < vertInputs.Length && ChangedOrFirstNotDefault(lastVertEvent, vertInputs, '1'))
                {
                    VertInputEvent newVertEvent = new VertInputEvent(i, vertDir);
                    if (vertDir == Direction.Undefined)
                        newVertEvent.Notes += "Undefined: vertInputs[" + i + "] == " + vertInputs[i];
                    if (lastVertEvent != null)
                        LinkDirEvents(lastVertEvent, newVertEvent);
                    AddNewestEvent(newVertEvent);
                    lastVertEvent = newVertEvent;
                }
                #endregion
                #region Jump Events
                int SHORT_HOP_MIN_FRAMES = 4;
                if (i < jumpInputs.Length && i > jumpCompleteTo && ChangedOrFirstNotDefault(lastJumpEvent, jumpInputs, '0'))
                {
                    int preTime = 1;
                    while (i + preTime < jumpInputs.Length && jumpInputs[i] == jumpInputs[i + preTime])
                        preTime++;
                    int postTime = 0;
                    while (i + preTime + postTime < jumpInputs.Length && jumpInputs[i + preTime + postTime] == '2')
                        postTime++;

                    JumpInputEvent newJumpEvent = new JumpInputEvent(i, i + preTime - 1, i + preTime + postTime, horizDir);
                    bool dashPossible = dashInputs[i] != '0';

                    if (preTime == 1 && postTime == 0) // anything?
                    {
                        newJumpEvent.Possible[~0] = true;
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR
                                            & JumpInputEvent.BUFFERED_GROUND & JumpInputEvent.GROUND] = false;
                        if (!dashPossible)
                            newJumpEvent.Possible[JumpInputEvent.AIR_DASH_JUMP] = false;
                    }
                    else if (preTime == 1 && postTime > 0)  // most likely not ground jump, unless ledge jump. Not wall jump.
                    {
                        newJumpEvent.Possible[JumpInputEvent.AIR & JumpInputEvent.DOWN_SLANT_WALL
                                            & JumpInputEvent.LEDGE_JUMP & JumpInputEvent.SHORT_HOP & JumpInputEvent.SLOPE_JUMP_45
                                            & JumpInputEvent.UP_SLANT_WALL] = true;
                        if (dashPossible && vertDir != Direction.Down)
                        {
                            newJumpEvent.Possible[JumpInputEvent.AIR_DASH_JUMP] = true;
                        }
                    }
                    else if (preTime == 8) // most likely ground jump, all speed lost.
                    {
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR & JumpInputEvent.GROUND] = true;
                        if (postTime == 0)
                        {
                            newJumpEvent.Possible[JumpInputEvent.BUFFERED_GROUND & JumpInputEvent.VERT_WALL] = true;
                        }
                    }
                    else if (preTime == 6) // most likely wall jump
                    {
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR & JumpInputEvent.VERT_WALL] = true;
                        if (postTime > 0)
                        {
                            newJumpEvent.Possible[JumpInputEvent.LEDGE_JUMP] = true;
                        }
                        else
                        {
                            newJumpEvent.Possible[JumpInputEvent.GROUND] = true;
                        }
                    }
                    else if (preTime > 1 && preTime < 6)
                    {
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR & JumpInputEvent.LEDGE_JUMP] = true;
                        if (postTime == 0 && preTime <= SHORT_HOP_MIN_FRAMES)
                            newJumpEvent.Possible[JumpInputEvent.SHORT_HOP] = true;
                    }
                    else if (preTime == 7)
                    {
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR & JumpInputEvent.VERT_WALL & JumpInputEvent.LEDGE_JUMP] = true;
                        if (postTime == 0)
                        {
                            newJumpEvent.Possible[JumpInputEvent.GROUND] = true;
                        }
                    }
                    else if (preTime > 8)
                    {
                        newJumpEvent.Possible[JumpInputEvent.BUFFERED_AIR & JumpInputEvent.BUFFERED_GROUND] = true;
                        newJumpEvent.Possible[JumpInputEvent.SHORT_HOP] = true;
                    }

                    AddNewestEvent(newJumpEvent);
                    lastJumpEvent = newJumpEvent;
                    jumpCompleteTo = i + preTime + postTime;
                }
                #endregion
                #region Dash Events
                if ((i < dashInputs.Length && dashInputs[i] != '0') || (i < downDashInputs.Length && downDashInputs[i] != '0'))
                {
                    DashInputEvent newDashEvent = new DashInputEvent(i, horizDir);
                    if (i < downDashInputs.Length && downDashInputs[i] != '0' && vertDir == Direction.Down)
                        newDashEvent.PossibleDowndash = true;

                    AddNewestEvent(newDashEvent);
                    lastDashEvent = newDashEvent;
                }
                #endregion

                #region Light Events
                if (i < lightInputs.Length && i >= lightCompleteTo && ChangedOrFirstNotDefault(lastLightEvent, lightInputs, '0'))
                {
                    bool possiblyUnbuffered = false;
                    int pressedFrame = i;
                    int releasedFrame = i;
                    int triggeredFrame = i;
                    int bufferTime = 0;

                    int curFrame = i + 1;
                    while (curFrame < lightInputs.Length && lightInputs[curFrame] == 'a')
                    {
                        curFrame++;
                        bufferTime++;
                    }
                    if (curFrame == lightInputs.Length)
                    {
                        possiblyUnbuffered = true;
                        triggeredFrame = curFrame - 1;

                    }
                    else if (lightInputs[curFrame] == 'b')
                    {
                        triggeredFrame = curFrame - 1;
                        while (curFrame < lightInputs.Length && lightInputs[curFrame] == 'b')
                        {
                            releasedFrame = curFrame;
                            curFrame++;
                        }
                    }
                    else if (lightInputs[curFrame] == '9')
                    {
                        releasedFrame = curFrame;
                        while (curFrame < lightInputs.Length - 1 && lightInputs[curFrame] != '0' && lightInputs[curFrame] != 'a')
                        {
                            curFrame++;
                        }
                        if (lightInputs[curFrame] == '0')
                        {
                            triggeredFrame = curFrame - 1;
                            if (lightInputs[curFrame - 1] == '1')
                                possiblyUnbuffered = true;
                        }
                        else if (lightInputs[curFrame] == 'a')
                        {
                            possiblyUnbuffered = true;
                            triggeredFrame = curFrame - 1;
                        }
                    }
                    Direction actualHoriz = GetHorizDir(triggeredFrame);
                    Direction actualVert = GetVertDir(triggeredFrame);

                    LightInputEvent newLightEvent = new LightInputEvent(i, triggeredFrame, releasedFrame, actualHoriz, actualVert);

                    if (possiblyUnbuffered)
                        newLightEvent.PossiblyUnbuffered = true;

                    AddNewestEvent(newLightEvent);
                    lastLightEvent = newLightEvent;
                    lightCompleteTo = curFrame + 1;

                }
                #endregion

                #region Heavy Events
                if (i < heavyInputs.Length && i >= heavyCompleteTo && ChangedOrFirstNotDefault(lastHeavyEvent, heavyInputs, '0'))
                {
                    bool possiblyUnbuffered = false;
                    int pressedFrame = i;
                    int releasedFrame = i;
                    int triggeredFrame = i;
                    int bufferTime = 0;

                    int curFrame = i + 1;
                    while (curFrame < heavyInputs.Length && heavyInputs[curFrame] == 'a')
                    {
                        curFrame++;
                        bufferTime++;
                    }
                    if (curFrame == heavyInputs.Length)
                    {
                        possiblyUnbuffered = true;
                        triggeredFrame = curFrame - 1;

                    }
                    else if (heavyInputs[curFrame] == 'b')
                    {
                        triggeredFrame = curFrame - 1;
                        while (curFrame < heavyInputs.Length && heavyInputs[curFrame] == 'b')
                        {
                            releasedFrame = curFrame;
                            curFrame++;
                        }
                    }
                    else if (heavyInputs[curFrame] == '9')
                    {
                        releasedFrame = curFrame;
                        while (curFrame < heavyInputs.Length - 1 && heavyInputs[curFrame] != '0' && heavyInputs[curFrame] != 'a')
                        {
                            curFrame++;
                        }
                        if (heavyInputs[curFrame] == '0')
                        {
                            triggeredFrame = curFrame - 1;
                            if (heavyInputs[curFrame - 1] == '1')
                                possiblyUnbuffered = true;
                        }
                        else if (heavyInputs[curFrame] == 'a')
                        {
                            possiblyUnbuffered = true;
                            triggeredFrame = curFrame - 1;
                        }
                    }
                    Direction actualHoriz = GetHorizDir(triggeredFrame + HEAVY_AIM_FRAMES);
                    Direction actualVert = GetVertDir(triggeredFrame + HEAVY_AIM_FRAMES);

                    HeavyInputEvent newHeavyEvent = new HeavyInputEvent(i, triggeredFrame, releasedFrame, actualHoriz, actualVert);

                    if (possiblyUnbuffered)
                        newHeavyEvent.PossiblyUnbuffered = true;

                    AddNewestEvent(newHeavyEvent);
                    lastHeavyEvent = newHeavyEvent;
                    heavyCompleteTo = curFrame + 1;
                }
                #endregion
            }

            #region Load SyncData
            EntityFrameContainer playerSyncData = (from container in RawData.entityFrameContainers
                                                   where container.unk1 == 2
                                                   select container).FirstOrDefault();
            SyncState lastSyncState = null;
            InputEvent curEvent = OrderedEvents[0];
            for (int j = 0; j < playerSyncData.entityFrames.Count; j++)
            {
                SyncState curSyncState = SyncState.CreateFromEntityFrame(playerSyncData.entityFrames[j]);
                curSyncState.PrevSyncState = lastSyncState;
                if (lastSyncState != null)
                    lastSyncState.NextSyncState = curSyncState;
                while (curEvent != null && curEvent.Frame <= curSyncState.Frame)
                {
                    curEvent.NextSyncPosition = curSyncState;
                    curEvent.LastSyncPosition = lastSyncState;
                    curEvent = curEvent.NextEvent;
                }

                OrderedSyncData.Add(curSyncState);
                lastSyncState = curSyncState;
            }
            #endregion
        }

        private IPlayerCharacter GetPlayerObject(Character character)
        {
            switch (character)
            {
                case Character.Dustman:
                    return new Dustman();
                default:
                    return null;
            }
        }

        public Direction GetHorizDir(int index)
        {
            Direction dir;
            switch (horizInputs[index < horizInputs.Length ? index : horizInputs.Length - 1])
            {
                case '0':
                    dir = Direction.Left;
                    break;
                case '1':
                    dir = Direction.Neutral;
                    break;
                case '2':
                    dir = Direction.Right;
                    break;
                default:
                    dir = Direction.Undefined;
                    break;
            }
            return dir;
        }

        public Direction GetVertDir(int index)
        {
            Direction dir;
            switch (vertInputs[index < vertInputs.Length ? index : vertInputs.Length - 1])
            {
                case '0':
                    dir = Direction.Up;
                    break;
                case '1':
                    dir = Direction.Neutral;
                    break;
                case '2':
                    dir = Direction.Down;
                    break;
                default:
                    dir = Direction.Undefined;
                    break;
            }
            return dir;
        }

        public Character GetCharacter()
        {
            return (Character)(RawData.header.character ?? int.Parse(RawData.meta.character));
        }


        public Category GetCategory()
        {
            if (RawData.meta.rank_all_score > RawData.meta.rank_all_time)
                return Category.Time;
            else
                return Category.Score;
        }



        private void LinkDirEvents(DirInputEvent lastDirEvent, DirInputEvent newDirEvent)
        {
            lastDirEvent.DirNextEvent = newDirEvent;
            newDirEvent.DirPreviousEvent = lastDirEvent;
        }

        private void AddNewestEvent(InputEvent newestEvent)
        {
            if (lastEvent != null)
                lastEvent.NextEvent = newestEvent;
            newestEvent.PrevEvent = lastEvent;
            lastEvent = newestEvent;
            OrderedEvents.Add(newestEvent);
        }

        private bool ChangedOrFirstNotDefault(InputEvent lastEvent, string input, char defaultValue)
        {
            return input.Length > i
                    && ((lastEvent == null && input[i] != defaultValue)
                      || (lastEvent != null && input[i] != input[i - 1]));
        }
    }
}
