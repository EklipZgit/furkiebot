using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReplayLibrary;

namespace CheatrDetectr
{
    class Program
    {     
        public static void Main(string[] args)
        {
            string id = "4778358";
            //PrintIntermediateReplay(id);
            //PrintAnalyzedReplay(id);
            PrintPositionAnalysis(id);
        }

        private static void PrintPositionAnalysis(string id)
        {EventLoader loader = new EventLoader();
            loader.LoadReplay(id);

            Trace.WriteLine(string.Format("pos\t\tvel\t\taccel\tGame\t Frame\tEvent\t   Duration,"));
            Trace.WriteLine(string.Format("\t\t\t\t\t\tTime\t      \tType \t   Buffer \tDirection"));
            Trace.WriteLine(string.Format("-------------------------------------------------------------------"));
            SyncState nextState = loader.OrderedSyncData[0];
            SyncState lastState = null;
            InputEvent nextInput = loader.OrderedEvents[0];
            while (nextState != null || nextInput != null)
            {
                string velString = "             ";
                string posString = "             ";
                string rateString = "             ";
                if (nextInput != null && nextState != null)
                {
                    if (nextInput.Frame == nextState.Frame)
                    {
                        velString = nextState.xSpeed.ToString("#000.0, ") + nextState.ySpeed.ToString("#000.0");
                        posString = nextState.xPos.ToString("#0000.0, ") + nextState.yPos.ToString("#0000.0");
                        if (lastState != null)
                        {
                            rateString = ((nextState.Frame - lastState.Frame) * (nextState.xSpeed - lastState.xSpeed) / 60.0).ToString("000.0, ");
                            rateString += ((nextState.Frame - lastState.Frame) * (nextState.ySpeed - lastState.ySpeed) / 60.0).ToString("000.0");
                        }
                        PrintInputWithState(velString, posString, rateString, nextInput);
                        lastState = nextState;
                        nextState = nextState.NextSyncState;
                        nextInput = nextInput.NextEvent;
                    }
                    else if (nextInput.Frame < nextState.Frame)
                    {
                        PrintInputWithState(velString, posString, rateString, nextInput);
                        nextInput = nextInput.NextEvent;
                    }
                    else if (nextInput.Frame < nextState.Frame)
                    {
                        velString = nextState.xSpeed.ToString("#000.0, ") + nextState.ySpeed.ToString("#000.0");
                        posString = nextState.xPos.ToString("#0000.0, ") + nextState.yPos.ToString("#0000.0");
                        if (lastState != null)
                        {
                            rateString = ((nextState.Frame - lastState.Frame) * (nextState.xSpeed - lastState.xSpeed) / 60.0).ToString("000.0, ");
                            rateString += ((nextState.Frame - lastState.Frame) * (nextState.ySpeed - lastState.ySpeed) / 60.0).ToString("000.0");
                        }
                        Trace.WriteLine(string.Format("{0}\t{1}\t{3}\t{4:00.000}\t {5}", posString, velString, rateString, nextState.GameTime, nextState.Frame, "SyncState"));
                        lastState = nextState;
                        nextState = nextState.NextSyncState;
                    }
                }
                else if (nextInput != null)
                {
                    velString = nextState.xSpeed.ToString("#000.0, ") + nextState.ySpeed.ToString("#000.0");
                    posString = nextState.xPos.ToString("#0000.0, ") + nextState.yPos.ToString("#0000.0");
                    if (lastState != null)
                    {
                        rateString = ((nextState.Frame - lastState.Frame) * (nextState.xSpeed - lastState.xSpeed) / 60.0).ToString("000.0, ");
                        rateString += ((nextState.Frame - lastState.Frame) * (nextState.ySpeed - lastState.ySpeed) / 60.0).ToString("000.0");
                    }
                    Trace.WriteLine(string.Format("{0}\t{1}\t{3}\t{4:00.000}\t {5}", posString, velString, rateString, nextState.GameTime, nextState.Frame, "SyncState"));
                    lastState = nextState;
                    nextState = nextState.NextSyncState;                    
                }
                else if (nextState != null)
                {
                    Trace.WriteLine(string.Format("{0}\t{1}\t{3}\t{4:00.000}\t {5}", posString, velString, rateString, nextState.GameTime, nextState.Frame, "SyncState"));
                    nextInput = nextInput.NextEvent;
                }
            }
        }

        private static void PrintInputWithState(string velString, string posString, string rateChangeString, InputEvent nextInput)
        {
            if (nextInput.GetType() == typeof(VertInputEvent))
            {
                VertInputEvent cast = (VertInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5} \t{6}  \t{7}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, "Vertical", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.Dir.ToString()));
            }
            else if (nextInput.GetType() == typeof(HorizInputEvent))
            {
                HorizInputEvent cast = (HorizInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5} \t{6}  \t{7}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, "Horizontal", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.Dir.ToString()));
            }
            else if (nextInput.GetType() == typeof(JumpInputEvent))
            {
                JumpInputEvent cast = (JumpInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5} \t{6}, {7}\t{8}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, "Jump     ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.HeldDir.ToString()));
            }
            else if (nextInput.GetType() == typeof(LightInputEvent))
            {
                LightInputEvent cast = (LightInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5} \t{6}, {7}\t{8}, \t{9}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, "Light   ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.VertDir.ToString(), cast.HorizDir.ToString()));
            }
            else if (nextInput.GetType() == typeof(HeavyInputEvent))
            {
                HeavyInputEvent cast = (HeavyInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5} \t{6}, {7}\t{8}, \t{9}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, "Heavy   ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.VertDir.ToString(), cast.HorizDir.ToString()));
            }
            else if (nextInput.GetType() == typeof(DashInputEvent))
            {
                DashInputEvent cast = (DashInputEvent)nextInput;
                Trace.WriteLine(string.Format("{0}\t{1}\t{2}\t{3:00.000}\t {4}  \t{5}   \t\t{6}", posString, velString, rateChangeString, cast.GameTime, cast.Frame, cast.PossibleDowndash ? "DownDash?" : "Dash     ", cast.HeldDir.ToString()));
            }
        }

        private static void PrintAnalyzedReplay(string id)
        {
            EventLoader loader = new EventLoader();
            loader.LoadReplay(id);
        }

        private static void PrintIntermediateReplay(string id)
        {
            EventLoader loader = new EventLoader();
            loader.LoadReplay(id);

            Trace.WriteLine(string.Format("Game\t Frame\tEvent\t   Duration,"));
            Trace.WriteLine(string.Format("Time\t      \tType \t   Buffer \tDirection"));
            Trace.WriteLine(string.Format("-------------------------------------------------------------------"));

            foreach (var frameEvent in loader.OrderedEvents)
            {
                if (frameEvent.GetType() == typeof(VertInputEvent))
                {
                    VertInputEvent cast = (VertInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2} \t{3}  \t{4}", cast.GameTime, cast.Frame, "Vertical", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.Dir.ToString()));
                }
                else if (frameEvent.GetType() == typeof(HorizInputEvent))
                {
                    HorizInputEvent cast = (HorizInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2} \t{3}  \t{4}", cast.GameTime, cast.Frame, "Horizontal", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.Dir.ToString()));
                }
                else if (frameEvent.GetType() == typeof(JumpInputEvent))
                {
                    JumpInputEvent cast = (JumpInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2} \t{3}, {4}\t{5}", cast.GameTime, cast.Frame, "Jump     ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.HeldDir.ToString()));
                }
                else if (frameEvent.GetType() == typeof(LightInputEvent))
                {
                    LightInputEvent cast = (LightInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2} \t{3}, {4}\t{5}, \t{6}", cast.GameTime, cast.Frame, "Light   ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.VertDir.ToString(), cast.HorizDir.ToString()));
                }
                else if (frameEvent.GetType() == typeof(HeavyInputEvent))
                {
                    HeavyInputEvent cast = (HeavyInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2} \t{3}, {4}\t{5}, \t{6}", cast.GameTime, cast.Frame, "Heavy   ", cast.Duration == -1 ? cast.Duration.ToString() : " " + cast.Duration, cast.TriggeredFrame - cast.Frame, cast.VertDir.ToString(), cast.HorizDir.ToString()));
                }
                else if (frameEvent.GetType() == typeof(DashInputEvent))
                {
                    DashInputEvent cast = (DashInputEvent)frameEvent;
                    Trace.WriteLine(string.Format("{0:00.000}\t {1}  \t{2}   \t\t{3}", cast.GameTime, cast.Frame, cast.PossibleDowndash ? "DownDash?" : "Dash     ", cast.HeldDir.ToString()));
                }
            }
        }
    }
}
