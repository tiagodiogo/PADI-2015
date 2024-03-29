﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

using PADIMapNoReduce.Commands;
using System.Threading;

namespace PADIMapNoReduce
{
    public class CommandsManager
    {
        private static string COMMENT_CHAR = "%";

        List<Command> listCommands = new List<Command>();
        List<Command> supportedCommands = new List<Command>();

        PuppetMaster pm = null;

        public CommandsManager(PuppetMaster pm)
        {
            this.pm = pm;
            supportedCommands.Add(new CreateWorkerCmd(pm));
            supportedCommands.Add(new SleepCmd(pm));
            supportedCommands.Add(new FreezeJobTrackerCmd(pm));
            supportedCommands.Add(new UnfreezeJobTrackerCmd(pm));
            supportedCommands.Add(new FreezeWorkerCmd(pm));
            supportedCommands.Add(new WaitCmd(pm));
            supportedCommands.Add(new StatusCmd(pm));
            supportedCommands.Add(new SubmitJobCmd(pm));
            supportedCommands.Add(new UnfreezeWorkerCmd(pm));
            supportedCommands.Add(new StatusIndividualCmd(pm));
        }

        public void LoadFile(string file)
        {
            StreamReader reader = null;
            try
            {
                listCommands.Clear();
                reader = File.OpenText(file);

                string line;
                int numLines = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith(COMMENT_CHAR) && !line.StartsWith("\r") && !line.StartsWith("\n") && line != "")
                    {
                        listCommands.Add(ParseCommand(line));
                    }

                    numLines++;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }

            }
        }

        public void ExecuteScript()
        {
            if (listCommands.Count == 0)
            {
                Logger.LogErr("No pending commands to be executed.");
            }

            while (listCommands.Count != 0)
            {
                Command c = listCommands.First();
                listCommands.Remove(c);
                
                try
                {
                    ExecuteCommand(c);
                    if (PuppetMaster.Run_Script_Step_By_Step_Opt)
                    {
                        return;
                    }
                }catch (Exception ex)
                {
                    Logger.LogWarn("ERROR IN COMMAND: " + ex.Message + " IGNORING...");
                }
                finally
                {
                    if (PuppetMaster.Run_Script_Step_By_Step_Opt &&  listCommands.Count != 0)
                        Logger.LogInfo("[CMD MASTER] Waiting for STEP");
                }
            }

            if (listCommands.Count == 0)
            {
                Logger.LogInfo("SCRIPT EXECUTION COMPLETED");
            }
        }

        public void ExecuteCommand(string line)
        {
            ParseCommand(line).Execute();      
        }

        private void ExecuteCommand(Command c)
        {
            c.Execute();
        }

        private Command ParseCommand(string line) {
            string commandType = line.Split(' ')[0];

            foreach (Command c in supportedCommands)
            {
                if (c.getCommandName() == commandType)
                {
                    Command copy = c.CreateCopy();
                    copy.Parse(line);
                    return copy;
                }
            }

            throw new Exception("No command found");
        }


        public static string generateCreateWorkProcess(string workerId, string puppetMasterUrl, string serviceUrl, string entryUrl)
        {
            return CreateWorkerCmd.COMMAND + " " + workerId + " " + puppetMasterUrl + " " + serviceUrl + " " + entryUrl;
        }

        public static string generateCreateJob(string entryUrl, string sourceFile, string destFile, int numberSplits, string mapper, string mapperDll)
        {
            return SubmitJobCmd.COMMAND + " " + entryUrl + " " + sourceFile + " " + destFile + " " + numberSplits + " " + mapper + " " + mapperDll;
        }

        public static string generateFreezeWorker(string workerId)
        {
            return FreezeWorkerCmd.COMMAND + " " + workerId;
        }

        public static string generateUnfreezeWorker(string workerId)
        {
            return UnfreezeWorkerCmd.COMMAND + " " + workerId;
        }

        public static string generateDisableJobTracker(string workerId)
        {
            return FreezeJobTrackerCmd.COMMAND + " " + workerId;
        }

        public static string generateEnableJobTracker(string workerId)
        {
            return UnfreezeJobTrackerCmd.COMMAND + " " + workerId;
        }

        public static string generateSlowWorker(string workerId, int seconds)
        {
            return SleepCmd.COMMAND + " " + workerId + " " + seconds;
        }

        public static string generateRefreshStatus()
        {
            return StatusCmd.COMMAND;
        }

        public static string generateStatusIndividual(string workerId)
        {
            return StatusIndividualCmd.COMMAND + " " + workerId;
        }
    }


}
