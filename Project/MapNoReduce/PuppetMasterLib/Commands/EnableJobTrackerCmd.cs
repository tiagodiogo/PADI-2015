﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce.Commands
{
    class EnableJobTrackerCmd : Command
    {
        string workerId;

        public EnableJobTrackerCmd(string line) : base(line) { }

        protected override bool ParseAux()
        {
            string[] args = line.Split(' ');
            if (args.Length == 2)
            {
                workerId = args[1];

                return true;
            }

            return false;

        }

        protected override bool ExecuteAux()
        {
            return EnableJobTracker(workerId);
        }


        public bool EnableJobTracker(string workerId)
        {

            commandResult = "[ENABLING] " + workerId + " Job traceker";

            return true;
        }
    }
}