﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce.Commands
{
    class DelayWorkerCmd : Command
    {

        public DelayWorkerCmd(string line) : base(line) { }

        int sec;
        string workerId;

        protected override bool ParseAux()
        {
            string[] args = line.Split(' ');
            if (args.Length == 3)
            {
                workerId = args[1];


                System.Diagnostics.Debug.WriteLine(args[2]);
                
                sec = Convert.ToInt32(args[2]);

                return true;
            }

            return false;

        }

        protected override bool ExecuteAux()
        {
            return DelayWorker(workerId, sec);
        }


        public bool DelayWorker(string workerId, int seconds)
        {

            commandResult = "[DELAY] " + workerId + " for " + seconds + " seconds.";

            return true;
        }
    }
}