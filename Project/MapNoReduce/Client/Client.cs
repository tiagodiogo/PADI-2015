﻿using System;
using System.Collections.Generic;
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

        public void submitJob(string filePath, string entryUrl, int splits)
        {
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, true);

            RemoteClient rmClient = new RemoteClient(filePath);
            RemotingServices.Marshal(rmClient, "IClient" , typeof(RemoteClient));

            worker = (IWorker)Activator.GetObject(typeof(IWorker), entryUrl);
            if (worker == null)
            {
                //bruno mete na consola que não conseguimos localizar o worker pretendido
            } else worker.receiveWork(clientURL, splits);
        }
    }
}
