﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PADIMapNoReduce
{
    public partial class SubmitJobForm : Form
    {

        GUIPuppetMaster pmGUI;

        public SubmitJobForm(GUIPuppetMaster pmGUI)
        {
            InitializeComponent();
            this.pmGUI = pmGUI;
        }

        private void SubmitJobForm_Load(object sender, EventArgs e)
        {
            checkCreateJob();
        }

        public bool checkCreateJob()
        {
            if (submitTaskEntryUrlMsgBox.Text == "" ||
                submitTaskSourceFileMsgBox.Text == "" ||
                submitTaskDestFileMsgBox.Text == "" ||
                submitJobMapTxtBox.Text == "" ||
                dllLocationMsgBox.Text == "")
            {
                submitTaskButton.Enabled = false;
                return false;
            }
            else
            {
                submitTaskButton.Enabled = true;
                return true;
            }
        }

        private void textChanged(object sender, EventArgs e)
        {
            checkCreateJob();
        }

        private void submitTaskNumberSplits_Leave(object sender, EventArgs e)
        {
            if (submitTaskNumberSplits.Text == "")
            {
                submitTaskNumberSplits.Text = "0";
            }
        }

        private void sourceFileBtn_Click(object sender, EventArgs e)
        {
            string filePath = FileUtil.FindSourceFile("Input files (*.in)|*.in | All files (*.*)|*.*", "Choose Source File");
            
            if(filePath != ""){
                submitTaskSourceFileMsgBox.Text = filePath;
            }
        }

        private void destFileBtn_Click(object sender, EventArgs e)
        {
            string dest = FileUtil.FindFolder();

            if (dest != ""){
                submitTaskDestFileMsgBox.Text = dest;
            }            
        }

        private void submitTaskButton_Click(object sender, EventArgs e)
        {
            string entryUrl = submitTaskEntryUrlMsgBox.Text;
            string source = submitTaskSourceFileMsgBox.Text;
            string destination = submitTaskDestFileMsgBox.Text;
            int numberSplits = Convert.ToInt32(submitTaskNumberSplits.Value);
            string mapper = submitJobMapTxtBox.Text;
            string mapperDllLocation = dllLocationMsgBox.Text;

            pmGUI.submitCommandAux(CommandsManager.generateCreateJob(entryUrl, source, destination, numberSplits, mapper, mapperDllLocation));
        }

        private void dllLocationChooseBtn_Click(object sender, EventArgs e)
        {

            string location = FileUtil.FindSourceFile("Dll File | *.dll|All files (*.*)|*.*", "Choose Dll for Mapper File");

            if(location != ""){
                dllLocationMsgBox.Text = location;
            }
        }
    }
}
