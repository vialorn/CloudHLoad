using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;

namespace CloudHLoad
{
    public partial class Form1 : Form
    {
        //private NpgsqlConnection pg_conn;
        static string dsn ="Server=:server:;Database=:database:;User Id=:user_id:;Password=:password:;";
        private pgWorker[] workers;
        private int workerCount;

        public Form1()
        {
            InitializeComponent();
            try
            {
                tbServer.Text = CloudHLoad.Properties.Settings.Default["server_123"].ToString(); ;
            }
            catch (Exception e) { }
        }
        private void LogEvent(string msg)
        {
            tbLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + " # " + msg + Environment.NewLine);
        }

        private string getDSN()
        {
            string server = tbServer.Text;
            string database = CloudHLoad.Properties.Settings.Default.database;
            string user_id = CloudHLoad.Properties.Settings.Default.user_id;
            string password = CloudHLoad.Properties.Settings.Default.password;
            return dsn.Replace(":server:", server).Replace(":database:", database).Replace(":user_id:", user_id).Replace(":password:", password);
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            if (nudThreadCount.Value == 0)
            {
                LogEvent("Количество потоков должно быть больше нуля");
                return;
            }
            else if (nudThreadCount.Value > 20)
            {
                LogEvent("Количество потоков должно быть не более 20");
                return;
            }
            btStart.Enabled = false;
            nudThreadCount.Enabled = false;
            lvRep.Items.Clear();
            workers = new pgWorker[(int)nudThreadCount.Value];
            workerCount = 0;
            string dyn_dsn = getDSN();
            for (int i = 1; i <= nudThreadCount.Value; i++)
            {
                workers[workerCount] = new pgWorker(i, dyn_dsn, bwProgressChanged, bwRunWorkerCompleted);
                if (workers[workerCount].IsReady)
                {
                    LogEvent("Запуск потока " + i.ToString("00"));
                    workers[workerCount].runThread();
                    LogEvent("Поток " + i.ToString("00") + " запущен");
                    workerCount++;
                    string[] lvic = new string[5] {"",workerCount.ToString(),"0", "0", "n/a"};
                    ListViewItem lvi = new ListViewItem(lvic);
                    lvRep.Items.Add(lvi);
                }
                else
                {
                    LogEvent("Отказ запуска потока" + i.ToString("00"));
                }
            }
            if (workerCount > 0)
            {
                btStop.Enabled = true;
            }
            else
            {
                btStart.Enabled = true;
            }
        }

        private void bwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ThreadReport tr = (ThreadReport)e.UserState;
            //LogEvent(tr.No + " # " + tr.QueryCount + " # " + tr.AverageTime);
            int pos = int.Parse(tr.No) - 1;
            lvRep.Items[pos].SubItems[1].Text = tr.No;
            lvRep.Items[pos].SubItems[2].Text = tr.QueryCount;
            lvRep.Items[pos].SubItems[3].Text = tr.ErrorCount;
            lvRep.Items[pos].SubItems[4].Text = tr.AverageTime;
        }

        private void bwRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ThreadReport tr = (ThreadReport)e.Result;
            LogEvent("Поток " + tr.No + " остановлен");
            int pos = int.Parse(tr.No) - 1;
            workers[pos] = null;
            bool allStopped = true;
            for (int i = 0; i < workerCount; i++)
            {
                if (workers[i] != null) allStopped = false;
            }
            if (allStopped)
            {
                LogEvent("Все потоки остановлены");
                workers = null;
                nudThreadCount.Enabled = true;
                btStart.Enabled = true;
            }
        }

        private void btStop_Click(object sender, EventArgs e)
        {
            btStop.Enabled = false;
            for (int i = 0; i < workerCount; i++)
            {
                workers[i].stopThread();
            }
        }
    }
}
