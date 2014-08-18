using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;

namespace Generator
{
    public class TimeControl : IDisposable
    {
        public enum TimeControlTypes
        {
            SecondsPerMove,
            BaseAndIncrement
        }

        public delegate void VoidDelegate();
        public delegate void MoveDelegate(Move move);
        public event VoidDelegate TimeToMove;
        public event MoveDelegate MoveMade;

        TimeControl(TimeControlTypes type)
        {
            _Type = type;

            _Timer = new System.Timers.Timer();
            _Timer.Interval = 100;
            _Timer.Elapsed += new System.Timers.ElapsedEventHandler(_Timer_Elapsed);

            _LastMoveTimeUsed = 0;
        }

        public TimeControl(TimeControlTypes type, double secondsPerMove)
            : this(type)
        {
            _SecondsPerMove = secondsPerMove;
        }

        public TimeControl(TimeControlTypes type, int baseTime, int increment)
            : this(type)
        {
            _BaseTime = baseTime;
            _Increment = increment;
        }

        public void Dispose()
        {
            _Timer.Stop();
            _Timer.Dispose();

            _OurStopWatch.Stop();
        }

        void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_OurStopWatch.Elapsed.TotalSeconds >= _TimeForThisMove)
            {
                _Timer.Stop();

                if (TimeToMove != null)
                {
                    TimeToMove();
                }
            }
        }

        void UpdateTimeForThisMove(double timeUsed)
        {
            switch (_Type)
            {
                case TimeControlTypes.SecondsPerMove:
                    {
                        _TimeForThisMove = _SecondsPerMove;

                        break;
                    }
                case TimeControlTypes.BaseAndIncrement:
                    {
                        _BaseTime -= timeUsed;
                        _BaseTime += _Increment;

                        _TimeForThisMove = _BaseTime / 40;

                        break;
                    }
            }
        }

        public void OurTurn()
        {
            UpdateTimeForThisMove(_LastMoveTimeUsed);

            _OurStopWatch.Stop();
            _OurStopWatch.Reset();
            _OurStopWatch.Start();

            _Timer.Start();
        }

        public void WeMoved(Move move)
        {
            _OurStopWatch.Stop();
            _Timer.Stop();

            _LastMoveTimeUsed = _OurStopWatch.Elapsed.TotalSeconds;

            if (MoveMade != null)
            {
                MoveMade(move);
            }
        }


        public double LastMoveTimeUsed
        {
            get
            {
                return _LastMoveTimeUsed;
            }
        }

        Stopwatch _OurStopWatch = new Stopwatch();
        System.Timers.Timer _Timer;
        TimeControlTypes _Type;

        double _SecondsPerMove;
        double _BaseTime;
        double _Increment;

        double _TimeForThisMove;
        double _LastMoveTimeUsed;
    }
}
