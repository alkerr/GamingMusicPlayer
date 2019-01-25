﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Windows;

namespace GamingMusicPlayer.SignalProcessing.Mouse
{
    public class MouseProcessor
    {
        private Object queueLock = new Object();
        //private bool queueLock = false;
        private List<short> sigDataList;
        private Thread mainThread;

        private int secsToProcess;
        private int minDistance;//minimum distance between last pos and current pos to capture mouse position
        private Point center;
        private Point lastPosition;

        private bool working;
        private Queue<double> mouseInputQueue; //queue of distances from the center point

        public event EventHandler onDataReady;

        public bool Processing { get { return this.working; } }

        public short[] Data
        {
            get
            {
                //normalizing - does not work well
                double average = 0, variance = 0;
                foreach(short s in sigDataList)
                {
                    average += (double)s;
                }
                average /= sigDataList.Count;
                foreach(short s in sigDataList)
                {
                    variance += Math.Pow(s - average, 2);
                }
                variance /= sigDataList.Count;
                variance = Math.Sqrt(variance);

                //Console.WriteLine("avg:" + average + ",variance:" + variance);
                for (int i=0; i < sigDataList.Count; i++)
                {
                    double s = (sigDataList[i] - average) / variance;
                    //sigDataList[i] = (short)(s);
                }

                List<short> speedsList = new List<short>();
                for(int i = sigDataList.Count - 1; i > 0; i--)
                {
                    short s = (short)(sigDataList[i] - sigDataList[i - 1]);
                    speedsList.Add(s);
                }
                return speedsList.ToArray();
            }
        }

        public MouseProcessor()
        {
            mouseInputQueue = new Queue<double>();
            MouseListener.HookMouse();
            MouseListener.OnMouseMoved  += OnMouseMoved;//this needs to happen only once!
            sigDataList = new List<short>();
            secsToProcess = -1;
            minDistance = -1;
            center = new Point(0, 0);
            lastPosition = new Point(-1, -1);
            working = false;
            onDataReady = done;
        }

        private void done(object sender, EventArgs e)
        {

        }

        public void record(int secs,int minDist, Point c)
        {
            if (!working)
            {
                secsToProcess = secs;
                minDistance = 10;
                center = c;
                mainThread = new Thread(new ThreadStart(process));
                mainThread.Start();
            }
            else
            {
                Console.WriteLine("mouse processor already recording..");
            }
        }

        private void OnMouseMoved(object sender, GlobalMouseEventArgs e)
        {

            if (working)
            {
                lock (queueLock)
                {
                    double distance = Point.Subtract(e.Position, lastPosition).Length;
                    if (distance >= minDistance)
                    {
                        mouseInputQueue.Enqueue(Point.Subtract(e.Position, center).Length);
                        //Console.WriteLine(e.Position.ToString());
                        lastPosition = e.Position;
                    }
                }
            }

        }

        private void process()
        {
            working = true;
            sigDataList.Clear();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed.TotalMilliseconds<secsToProcess*1000)
            {
                lock(queueLock)
                {
                    if (mouseInputQueue.Count > 0)
                    {
                        double d = mouseInputQueue.Dequeue();
                        sigDataList.Add((short)d);
                        //sigDataList.Add((short)-d);
                    }
                    
                }
            }
            //Console.WriteLine("Done processing");
            sw.Stop();
            working = false;
            onDataReady(null, null);
            
        }

    }
}
