﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;


namespace PADIMapNoReduce
{
    public class Client
    {
        private IWorker worker = null;
        private string clientURL = "tcp://localhost:8086/IClient";


        public void submitJob(string jobFilePath, string destPath, string entryUrl, int splits, string mapperName, string mapperPath)
        {

            try
            {
                TcpChannel channel = new TcpChannel(8086);
                ChannelServices.RegisterChannel(channel, true);

                RemoteClient rmClient = new RemoteClient(jobFilePath, mapperName, mapperPath);
                RemotingServices.Marshal(rmClient, "IClient" , typeof(IClient));

                worker = (IWorker)Activator.GetObject(typeof(IWorker), entryUrl);
                if (worker == null)
                {
                    Logger.LogInfo("Could not find specified worker");
                }
                else worker.ReceiveWork(clientURL, splits);
            }
            //catched when node is not responding
            catch (RemotingTimeoutException timeException)
            {
                System.Diagnostics.Debug.WriteLine("time exception: " + timeException.Message);
            }
        }
    }
}
