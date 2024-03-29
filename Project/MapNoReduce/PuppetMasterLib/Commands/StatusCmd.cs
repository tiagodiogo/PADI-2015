﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Runtime.Remoting;

namespace PADIMapNoReduce.Commands
{
    public class StatusCmd : Command
    {
        public static string COMMAND = "STATUS";

        public StatusCmd(PuppetMaster pm) : base(pm) { }


        protected override bool ParseAux()
        {
            return true;
        }

        protected override void ExecuteAux()
        {
            RefreshStatus();
        }

        public override string getCommandName()
        {
            return COMMAND;
        }

        public override Command CreateCopy()
        {
            return new StatusCmd(puppetMaster);
        }

        public void RefreshStatus()
        {

            Logger.LogInfo("[STATUS]");

            if (NetworkManager.GetRemoteWorkersObj().Count == 0)
            {
                Logger.LogWarn("No workers registered");
                return;
            }

            
            List<string> listBecameInactiveWorkers = new List<string>();
            foreach (KeyValuePair<string, IWorker> entry in NetworkManager.GetRemoteWorkersObj())
            {
                string id = entry.Key;
                try
                {
                    StatusIndividualCmd.RefreshStatus(id, true);
                }
                catch (Exception ex)
                {
                    if (ex is RemotingException || ex is SocketException)
                    {
                        Logger.LogErr("[" + id + " is down]: " + ex.Message);
                        listBecameInactiveWorkers.Add(id);
                    }                    
                }
            }

            foreach (string id in listBecameInactiveWorkers){
                NetworkManager.SetWorkerAsDown(id);
            }
            
            Logger.RefreshNetwork();
        }

    }
}
