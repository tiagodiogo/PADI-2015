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
using System.Net.Sockets;

namespace PADIMapNoReduce
{
    //Job tracker portion of the node
    partial class Node
    {
        private static int PingTimeout = 4000;
        private JobTrackerInformation jtInformation;
        IWorker primaryJobTracker;
        IWorker secondaryJT = null;

        //to use when secondary server fails
        IWorker backupSecondaryServerIfFails;
        bool primaryDidStartedJob = false;

        string backupSecondaryServerIfFailsUrl;
        private bool receivedAliveFromServer;

        bool isPrimary = false;
        bool didStartedPrimaryProcess = false;

        public bool PingJT()
        {
            /* wait until if I am unfrozen and revert to Worker if needed */
            if (WaitForUnfreezeAndCheckChanges())
                return false;

            Logger.LogInfo("Answering Ping");
            return true;
        }

        public string SetUpAsSecondaryServer(string clientUrl, string primaryJTurl, long fileSize, long numSplits, string mapperName, byte[] mapperCode)
        {
            this.clientURL = clientUrl;
            firstRequest = false;
            client = (IClient)Activator.GetObject(typeof(IClient), clientURL);
            isPrimary = false;
            this.fileSize = fileSize;
            primaryJobTracker = (IWorker)Activator.GetObject(typeof(IWorker), primaryJTurl);
            jtInformation = new JobTrackerInformation(this, numSplits);
            this.mapperName = mapperName;
            this.mapperCode = mapperCode;
            Thread SendIAmAliveThread = new Thread(() =>
            {
                while (!isPrimary && !jtInformation.DidFinishJob())
                {
                    /* wait until if I am unfrozen and revert to Worker if needed */
                    if (WaitForUnfreezeAndCheckChanges())
                        break;
                    /* --------------------------- */

                    //Logger.LogInfo("Sending I am alive");
                    try
                    {
                        primaryJobTracker.PingJT();
                        receivedAliveFromServer = true;
                    }
                    catch (Exception)
                    {
                        Logger.LogErr("TIMEOUT");
                    }


                    if (!receivedAliveFromServer)
                    {
                        Logger.LogErr("PRIMARY JOB TRACKER IS DOWN");
                        nodeDown();
                        PrintUpdateNetwork();
                        jtInformation.AlertChangeOfJobTracker(myURL);
                        this.StartPrimaryJobTrackerProcess(clientUrl, fileSize, jtInformation.numSplits, mapperName, mapperCode);
                        break;
                    }

                    receivedAliveFromServer = false;
                    Thread.Sleep(PingTimeout);
                    
                }
            });
            SendIAmAliveThread.Start();

            return backURL;
        }

        private void StartPrimaryJobTrackerProcess(string clientUrl, long fileSize, long numSplits, string mapperName, byte[] mapperCode)
        {
            currentJobTrackerUrl = this.myURL;
            serverRole = ServerRole.JOB_TRACKER;
            status = ExecutionState.WORKING;
            isPrimary = true;
            jtInformation = new JobTrackerInformation(this, numSplits);
            this.clientURL = clientUrl;
            this.fileSize = fileSize;
            client = (IClient)Activator.GetObject(typeof(IClient), clientURL);
            this.mapperName = mapperName;
            this.mapperCode = mapperCode;
            Thread trackWorkersThread = new Thread(() =>
            {
                while (!jtInformation.DidFinishJob() && isPrimary && serverRole == ServerRole.JOB_TRACKER)
                {
                    /* wait until if I am unfrozen and revert to Worker if needed */
                    if (WaitForUnfreezeAndCheckChanges())
                        break;
                    /* --------------------------- */

                    Logger.LogInfo("[CHECKING SLOW WORKERS]");
                    SplitInfo slowSplit = jtInformation.FindSlowSplit();
                    if (slowSplit != null)
                    {
                        //Logger.LogWarn("There is a slow split - " + slowSplit.splitId);
                        IWorker freeWorker = jtInformation.GetFirstFreeWorker();
                        if (freeWorker != null)
                        {
                            Logger.LogWarn("[SLOWWWWWWW SPLIT] RESENDING " + slowSplit.remainingSplits);
                            ResendSplitToNextWorker(freeWorker, slowSplit.fileSize, slowSplit.totalSplits, slowSplit.remainingSplits);
                        }
                    }
                    Thread.Sleep(4000);
                }

                serverRole = ServerRole.NONE;
                status = ExecutionState.WAITING;
            });
            trackWorkersThread.Start();

            Thread ConfigureSecondaryServerThread = new Thread(() =>
            {
                //wait for an available backUrl, then pings it then sets up as primary Server
                while (isPrimary && serverRole == ServerRole.JOB_TRACKER && !jtInformation.DidFinishJob())
                {
                    if (backURL == myURL)
                    {
                        continue;
                    }

                    //Logger.LogInfo("Waiting for an available url to be the secondary JT");
                    secondaryJT = (IWorker)Activator.GetObject(typeof(IWorker), backURL);

                    try
                    {
                        secondaryJT.PingJT();
                        backupSecondaryServerIfFailsUrl = secondaryJT.SetUpAsSecondaryServer(this.clientURL, this.myURL, fileSize, numSplits, mapperName, mapperCode);

                        backupSecondaryServerIfFails = (IWorker)Activator.GetObject(typeof(IWorker), backupSecondaryServerIfFailsUrl);

                        Logger.LogInfo("Success setting setup backupserver");
                        break;
                    }
                    catch(Exception)
                    {
                        Logger.LogInfo("There is still no backUrl available to become backup server");
                    }

                    Thread.Sleep(1000);
                }

            });
            ConfigureSecondaryServerThread.Start();

            if (!primaryDidStartedJob && !firstRequest)
            {
                primaryDidStartedJob = true;
                ReceiveWork(clientUrl, fileSize, numSplits, mapperName, mapperCode);
            }

            firstRequest = false;

        }

        public void PrimaryStartedJob() {
            primaryDidStartedJob = true;
        }

        bool firstRequest = true;

        public void ReceiveWork(string clientURL, long fileSize, long splits, string mapperName, byte[] mapperCode)
        {
            try
            {
                Logger.LogInfo("Received: " + clientURL + " with " + splits + " splits fileSize =" + fileSize);


                if (!didStartedPrimaryProcess)
                {
                    StartPrimaryJobTrackerProcess(clientURL, fileSize, splits, mapperName, mapperCode);
                    didStartedPrimaryProcess = true;
                }

                /* wait until if I am unfrozen and revert to Worker if needed */
                Logger.LogWarn("Testing in freeze state");
                WaitForUnfreezeAndCheckChanges();
                /* --------------------------- */


                IWorker nextWorker = (IWorker)Activator.GetObject(typeof(IWorker), nextURL);
                if (splits > fileSize)
                    splits = fileSize;
                long splitSize = fileSize / splits;

                FetchWorkerAsyncDel RemoteDel = new FetchWorkerAsyncDel(nextWorker.FetchWorker);
                IAsyncResult RemAr = RemoteDel.BeginInvoke(clientURL, myURL, mapperName, mapperCode, fileSize, splits, splits, myURL, true, null, null);

                if (secondaryJT == null) {
                    Logger.LogErr("Job tracker backup hasn't been configured yet");
                    secondaryJT.PrimaryStartedJob();
                }
                
                return;
            }
            catch (RemotingException e)
            {
                Logger.LogErr("Remoting Exception: " + e.Message);
            }
        }

        public void RegisterWorker(string workerId, string workerUrl)
        {
            /* wait until if I am unfrozen and revert to Worker if needed */
            WaitForUnfreezeAndCheckChanges();
            /* --------------------------- */

            if (jtInformation.didFinishedCurrentJob())
            {
                return;
            }

            if (isPrimary)
            {
                //Logger.LogInfo("Resending register to secondary JT");
                if (!CheckSecondaryJTAlive())
                    return;
                secondaryJT.RegisterWorker(workerId, workerUrl);
            }
            //Logger.LogInfo("REGISTERING WORKER");
            IWorker worker = (IWorker)Activator.GetObject(typeof(IWorker), workerUrl);
            jtInformation.RegisterWorker(workerId, worker);
        }

        public bool CanContinueProcessSplit(string workerId, long splitId)
        {
            return jtInformation.CanContinueProcessSplit(workerId, splitId);
        }

        public void LogStartedSplit(string workerId, long fileSize, long totalSplits, long remainingSplits)
        {
            /* wait until if I am unfrozen and revert to Worker if needed */
            WaitForUnfreezeAndCheckChanges();
            /* --------------------------- */

            if (jtInformation.didFinishedCurrentJob())
            {
                return;
            }

            if (isPrimary)
            {
                //Logger.LogInfo("RESENDING STARTSPLIT TO SECONDARY SERVER");
                if (!CheckSecondaryJTAlive())
                    return;
                secondaryJT.LogStartedSplit(workerId, fileSize, totalSplits, remainingSplits);
            }
            jtInformation.LogStartedSplit(workerId, fileSize, totalSplits, remainingSplits);
        }
        
        public void LogFinishedSplit(string workerId, long totalSplits, long remainingSplits)
        {
            /* wait until if I am unfrozen and revert to Worker if needed */
            WaitForUnfreezeAndCheckChanges();
            /* --------------------------- */

            if (jtInformation.didFinishedCurrentJob())
            {
                return;
            }

            if (isPrimary)
            {
                //Logger.LogInfo("RESENDING ENDSPLIT TO SECONDARY SERVER");
                if(!CheckSecondaryJTAlive())
                    return;
                secondaryJT.LogFinishedSplit(workerId, totalSplits, remainingSplits);
            }
            jtInformation.LogFinishedSplit(workerId, totalSplits, remainingSplits);
        }
        public bool IsPrimary()
        {
            return isPrimary;
        }

        public bool  WaitForUnfreezeAndCheckChanges()
        {
            WaitForUnfreeze();
            if (secondaryJT != null && secondaryJT.IsPrimary())
            {
                Logger.LogWarn("REVERTING BACK TO WORKER");
                this.serverRole = ServerRole.WORKER;
                isPrimary = false;
                didStartedPrimaryProcess = false;
                return true;
            }

            return false;

        }

        public void RevertToNoneState() {
            Logger.LogErr("REVERTING BACK TO NONE STATE");

            //just notify once
            if (isPrimary)
                client.NotifyFinishedJob();

            
            //currentJobTrackerUrl = null;
            serverRole = ServerRole.NONE;
            status = ExecutionState.WAITING;
            
            this.clientURL = null;
            this.fileSize = 0;

            primaryDidStartedJob = false;
            isPrimary = false;
            firstRequest = true;
            didStartedPrimaryProcess = false;
            client = null;
        }

        public bool CheckSecondaryJTAlive()
        {
            try
            {

                secondaryJT.PingJT();
                return true;
            }
            catch (Exception)
            {
                Logger.LogWarn("SECONDARY SERVER DOWN.");
                backupSecondaryServerIfFails.nodeDown();
                backupSecondaryServerIfFails.SetUpAsSecondaryServer(clientURL, myURL, fileSize, jtInformation.numSplits, mapperName, mapperCode);
            }

            return false;
        }

        private void ResendSplitToNextWorker(IWorker worker, long fileSize, long totalSplits, long remainingSplits)
        {

            FetchWorkerAsyncDel RemoteDel = new FetchWorkerAsyncDel(worker.FetchWorker);
            Thread liveCheck = new Thread(this.liveCheck);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(clientURL, myURL, mapperName, mapperCode, fileSize, totalSplits, remainingSplits, myURL, false, null, null);
            liveCheck.Start(RemAr);
        }
    }
}
