﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PADIMapNoReduce.Commands
{
    public class DelayWorkerCmd : Command
    {
        public static string COMMAND = "SLEEPP";


        int sec;
        string workerId;

        public DelayWorkerCmd(PuppetMaster pm) : base(pm) { }

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

        public override string getCommandName()
        {
            return COMMAND;
        }

        protected override void ExecuteAux()
        {
            DelayWorker(workerId, sec);
        }


        public void DelayWorker(string workerId, int seconds)
        {

            try
            {
                IWorker w = puppetMaster.GetRemoteWorker(workerId);
                Logger.LogInfo("[DELAY] " + workerId + " for " + seconds + " seconds.");
                w.Slow(seconds);
            }
            catch (Exception ex)
            {
                Logger.LogErr(ex.Message);
            }
        }
    }
}
