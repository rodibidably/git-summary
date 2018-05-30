﻿using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
namespace git_tools
{
    public partial class Git_Tools : Form
    {
        GitTools gt = new GitTools();
        string stdError = "";
        string stdOutput = "";
        public Git_Tools()
        {
            InitializeComponent();
        }
        // Load Page | Browse for Git Location
        private void GitTools_Load(object sender, EventArgs e)
        {
            // Set One-Time form values that can't be set through designer and never change
            toolTips.SetToolTip(chkLocalSummary, "Checks only local changes (no Fetch first), which is faster.");
            toolTips.SetToolTip(chkDeepLookup, "Will look for Git repos recursivly within the directory tree (does not search sub folders under a Git repo). Can be slow for large trees.");
            dgvGitSummary.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvGitSummary.AutoGenerateColumns = false;
            // Determine if Git is installed in default location
            LoadGitTools("C:\\Program Files\\Git");
        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Display the Open File Dialog
            ofdGit.InitialDirectory = gt.Path;
            DialogResult result = ofdGit.ShowDialog();
            if (result == DialogResult.OK)
            {
                // Determine if Git is installed in User selected location
                LoadGitTools(Path.GetDirectoryName(ofdGit.FileName));
            }
        }
        private void LoadGitTools(string pathGit)
        {
            // Cleanup form (to defaults) before processing
            lblStatus.Text = "Validating Git is installed/configured...";
            lblStatus.ForeColor = System.Drawing.Color.Black;
            lnkGitInstall.Visible = false;
            lnkGitLocation.Text = pathGit;
            lblGitVersion.Text = "";
            btnGitSummary.Enabled = false;
            btnGitBranchStatus.Enabled = false;
            if (tabNav.TabPages.Contains(tabGitSummary))
            {
                tabNav.TabPages.Remove(tabGitSummary);
                tabNav.TabPages.Remove(tabGitBranchStatus);
            }
            // Validate Git is installed
            if (!gt.IsGitInstalled(pathGit))
            {
                // Prompt User for alternate Git installation location
                lblStatus.Text = "Git is not installed/configured, or installed in a non-standard location. Please browse to the Git install location (where git-cmd.exe is located).";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                lnkGitInstall.Visible = true;
            }
            else
            {
                // Git Installed, now let's test that we can run a simple command
                gt.RunCommand("--version", pathGit, ref stdOutput, ref stdError);
                lblGitVersion.Text = stdOutput;
                if (stdError != "")
                {
                    // Unable to determine Git version. Something may be wrong with the installation
                    lblStatus.Text = "Git is installed, but appears to not be configured properly (unable to determine Git version). Please check your Git installation / configuration.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    lnkGitInstall.Visible = true;
                }
                else
                {
                    // Everything appears to be working... Woohoo!
                    lblStatus.Text = "Git appears to be installed. If you would like to use a different location, please Browse to that path.";
                    btnGitSummary.Enabled = true;
                    btnGitBranchStatus.Enabled = true;
                }
            }
        }
        // Clickable Links
        private void lnkGitLocation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open Windows Explorer to the Git Install Location
            if (!Directory.Exists(gt.Path))
            {
                lnkGitLocation.Text = "";
                MessageBox.Show("Path does not exist. Please Browse for the Git installation directory.", "Path does not exist.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                Process.Start(gt.Path);
            }
        }
        private void lnkGitTools_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open Default Browser to the GitHub Project
            System.Diagnostics.Process.Start("https://github.com/rodibidably/git-tools");
        }
        private void lnkGitSummaryRoot_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Open Windows Explorer to the selected Git Repository root
            if (!Directory.Exists(lnkGitSummaryRoot.Text))
            {
                lnkGitSummaryRoot.Text = "";
                MessageBox.Show("Path does not exist. Please Browse for another Folder.", "Path does not exist.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                Process.Start(lnkGitSummaryRoot.Text);
            }
        }
        // git-summary
        private void btnGitSummary_Click(object sender, EventArgs e)
        {
            // Do not allow new process to begin before old process has ended
            if (bwGitSummary.IsBusy != true)
            {
                // Set Default Path to the last path used
                if (ConfigurationManager.AppSettings["LastPath"] != "")
                {
                    fbdPath.SelectedPath = ConfigurationManager.AppSettings["LastPath"];
                }
                // Display the Open Folder Dialog
                if (fbdPath.ShowDialog() == DialogResult.OK)
                {
                    // Write last path used to app.config
                    Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    configuration.AppSettings.Settings["LastPath"].Value = fbdPath.SelectedPath;
                    configuration.Save(ConfigurationSaveMode.Full, true);
                    ConfigurationManager.RefreshSection("appSettings");
//                    ConfigurationManager.AppSettings["LastPath"] = fbdPath.SelectedPath;
                    // Cleanup form (to default / selected values) before processing
                    lnkGitSummaryRoot.Text = fbdPath.SelectedPath;
                    lblGitSummaryOptions.Text = chkLocalSummary.Text + "=" + chkLocalSummary.Checked;
                    lblGitSummaryOptions.Text += " | " + chkDeepLookup.Text + "=" + chkDeepLookup.Checked;
                    if (!tabNav.TabPages.Contains(tabGitSummary))
                    {
                        tabNav.TabPages.Add(tabGitSummary);
                    }
                    tabNav.SelectedTab = tabGitSummary;
                    lblGitSummaryProgress.Visible = true;
                    lblGitSummaryProgress.Text = ("0%");
                    dgvGitSummary.Visible = false;
                    // Start the asynchronous operation (essentially: bwGitSummary_DoWork)
                    bwGitSummary.RunWorkerAsync();
                }
            }
        }
        private void bwGitSummary_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // This event handler is where the time-consuming work is done
            BackgroundWorker worker = sender as BackgroundWorker;
            // Recursively run through selected path to build List<>
            gt.GetRepos(ref worker, lnkGitSummaryRoot.Text, chkLocalSummary.Checked, chkDeepLookup.Checked);
        }
        private void bwGitSummary_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // This event handler updates the progress
            lblGitSummaryProgress.Text = (e.ProgressPercentage.ToString() + "%");
        }
        private void bwGitSummary_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // This event handler deals with the results of the background operation
            if (e.Cancelled == true)
            {
                lblGitSummaryProgress.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                lblGitSummaryProgress.Text = "Error: " + e.Error.Message;
            }
            else
            {
                // Cleanup form after processing, to display results
                lblGitSummaryProgress.Visible = false;
                dgvGitSummary.Visible = true;
                // After List<> has been built, now load DataGrid with results
                dgvGitSummary.DataSource = gt.Repos;
            }
        }
        private void dgvGitSummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                // Open Windows Explorer to the selected Git Repository
                Process.Start(lnkGitSummaryRoot.Text + dgvGitSummary.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
            }
        }
        // git-branch-status
        private void btnGitBranchStatus_Click(object sender, EventArgs e)
        {
            // 
            if (!tabNav.TabPages.Contains(tabGitBranchStatus))
            {
                tabNav.TabPages.Add(tabGitBranchStatus);
                tabNav.SelectedTab = tabGitBranchStatus;
            }
        }
    }
}
