﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PADIMapNoReduce
{
    public class SplitInfo
    {
        Stopwatch stopWatch = new Stopwatch();
        int elapsedMiliSeconds = int.MaxValue;
        bool finished = false;
        public long splitId;
        public long splitSize;
        public long beginSplit = 222;
        public long endPlit = 100;
        public long fileSize;
        public long totalSplits;
        public long remainingSplits;

        public SplitInfo(long splitId, long fileSize, long totalSplits, long remainingSplits)
        {
            this.splitId = splitId;
            this.fileSize = fileSize;
            this.totalSplits = totalSplits;
            this.remainingSplits = remainingSplits;
            stopWatch.Start();
        }

        public void EndedSplit()
        {
            stopWatch.Stop();
            elapsedMiliSeconds = SplitTime();
            finished = true;
        }

        public bool DidFinished()
        {
            return finished;
        }

        public int SplitTime()
        {
            return stopWatch.Elapsed.Milliseconds;
        }
    }
}