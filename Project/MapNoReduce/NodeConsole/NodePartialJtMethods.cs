﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;

namespace PADIMapNoReduce
{
    //Job tracker portion of the node
    partial class Node
    {

        private JobTrackerInformation jtInformation;

        public void ReceiveWork(string clientURL, long fileSize, long splits, string mapperName, byte[] mapperCode)
        {
            try
            {
                /* wait until if I am unfrozen */
                WaitForUnfreeze();
                /* --------------------------- */


                Logger.LogInfo("Received: " + clientURL + " with " + splits + " splits fileSize =" + fileSize);

                currentJobTrackerUrl = this.id;
                serverRole = ServerRole.JOB_TRACKER;
                status = ExecutionState.WORKING;
                this.clientURL = clientURL;
                client = (IClient)Activator.GetObject(typeof(IClient), clientURL);

                /******* FOR DETECTING SLOW WORKERS *********/

                jtInformation = new JobTrackerInformation(this, splits);

                Thread runThread3 = new Thread(() =>
                {
                    while (!jtInformation.DidFinishJob())
                    {
                        Logger.LogInfo("---- Checking for slow workers ------");
                        SplitInfo slowSplit = jtInformation.FindSlowSplit();
                        if (slowSplit != null)
                        {
                            Logger.LogWarn("Slow worker... sending split elsewhere");
                            ResentSplitToNextWorker(slowSplit.totalSplits, slowSplit.remainingSplits);
                        }
                        Thread.Sleep(2000);
                    }

                    //TIAGO SANTOS PODES POR AQUI O QUE QUISERES PARA NOTIFICAR O CLIENTE QUE ACABOU
                });
                runThread3.Start();

                /*******************************************/


                IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), nextURL);
                if (splits > fileSize)
                    splits = fileSize;
                long splitSize = fileSize / splits;



                FetchWorkerAsyncDel RemoteDel = new FetchWorkerAsyncDel(worker.FetchWorker);
                IAsyncResult RemAr = RemoteDel.BeginInvoke(clientURL, myURL, mapperName, mapperCode, fileSize, splits, splits, myURL, true, null, null);

                return;
            }
            catch (RemotingException e)
            {
                Logger.LogErr("Remoting Exception: " + e.Message);
            }
        }

        public void RegisterWorker(string url)
        {
            Logger.LogInfo("REGISTERING WORKER");
            IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), url);
            jtInformation.RegisterWorker(url, worker);
        }

        public void UnregisterWorker(string url)
        {
            jtInformation.UnregisterWorker(url);
        }

        public void LogStartedSplit(string workerId, long fileSize, long totalSplits, long remainingSplits)
        {
            jtInformation.LogStartedSplit(workerId, fileSize, totalSplits, remainingSplits);
        }


        
        public void LogFinishedSplit(string workerId, long totalSplits, long remainingSplits)
        {
            jtInformation.LogFinishedSplit(workerId, totalSplits, remainingSplits);
        }


        public void ResentSplitToNextWorker(long totalSplits, long remainingSplits)
        {
            Logger.LogWarn("RESENDING SLOW SPLIT TO NEXT URL: " + nextURL);
            IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), nextURL);
            FetchWorkerAsyncDel RemoteDel = new FetchWorkerAsyncDel(worker.FetchWorker);
            Thread liveCheck = new Thread(this.liveCheck);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(clientURL, myURL, mapperName, mapperCode, fileSize, totalSplits, remainingSplits, myURL, false, null, null);
            liveCheck.Start(RemAr);
        }
    }
}
