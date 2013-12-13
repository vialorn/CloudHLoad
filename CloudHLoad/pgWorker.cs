using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
using System.ComponentModel;
using Npgsql;

namespace CloudHLoad
{

    public class ThreadReport
    {
        public string No;
        public string QueryCount;
        public string ErrorCount;
        public string AverageTime;
    }

    class pgWorker
    {
        private int thNo;
        private BackgroundWorker bw;
        private NpgsqlConnection pg_conn;
        private string pg_error;
        public bool IsReady { get { return (this.pg_conn != null); } }
        public string ConnectionError { get { return this.pg_error; } }
        public int No { get { return this.thNo; } }

        public pgWorker(int pos, string dsn, 
            ProgressChangedEventHandler onProgress,
            RunWorkerCompletedEventHandler onComplete)
        {
            this.thNo = pos;
            this.pg_conn = new NpgsqlConnection(dsn);
            try
            {
                pg_conn.Open();
                pg_error = "";
            }
            catch (Exception ex)
            {
                pg_conn = null;
                pg_error = ex.Message;
            }
            if (pg_conn != null)
            {
                this.bw = new BackgroundWorker();
                this.bw.WorkerReportsProgress = true;
                this.bw.WorkerSupportsCancellation = true;
                this.bw.DoWork += this.bwDoWork;
                this.bw.ProgressChanged += onProgress;
                this.bw.RunWorkerCompleted += onComplete;
            }
        }

        private void bwDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker work = (BackgroundWorker)sender;
            pgThread thread = new pgThread(this.thNo, this.pg_conn);
            while (!work.CancellationPending)
            {
                thread.doQuery();
                ThreadReport tr = new ThreadReport();
                tr.No = thread.No;
                tr.QueryCount = thread.QueryCount;
                tr.ErrorCount = thread.ErrorCount;
                tr.AverageTime = thread.AverageTime;
                work.ReportProgress(0, tr);
            }
            ThreadReport res = new ThreadReport();
            res.No = thread.No;
            res.QueryCount = thread.QueryCount;
            res.ErrorCount = thread.ErrorCount;
            res.AverageTime = thread.AverageTime;
            e.Result = res;
        }

        public void runThread()
        {
            this.bw.RunWorkerAsync();
        }

        public void stopThread()
        {
            this.bw.CancelAsync();
        }

    }
}
