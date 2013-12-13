using System;
//using System.Collections.Generic;
using System.Linq;
//using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace CloudHLoad
{
    class pgThread
    {
        public int thNo;
        public int thQueryCount;
        public int thErrorCount;
        public TimeSpan thAverageTime;
        
        private TimeSpan[] thTimes;
        private int thTimesPointer;
        private Random shift;

        private NpgsqlCommand pg_cmd;

        public string No { get { return this.thNo.ToString("00"); } }
        public string QueryCount { get { return this.thQueryCount.ToString("0000"); } }
        public string ErrorCount { get { return this.thErrorCount.ToString("0000"); } }
        public string AverageTime { get { return this.thAverageTime.ToString(@"mm\:s\:fff"); } }

        public pgThread(int pos, NpgsqlConnection conn)
        {
            this.thNo = pos;
            this.thQueryCount = 0;
            this.thErrorCount = 0;
            this.thAverageTime = new TimeSpan(0);
            this.thTimes = new TimeSpan[10];
            this.thTimesPointer = 0;
            this.shift = new Random();
            this.pg_cmd = new NpgsqlCommand();
            this.pg_cmd.Connection = conn;
            this.pg_cmd.CommandType = System.Data.CommandType.Text;
            this.pg_cmd.CommandText = "select * from cloud.bizcs (:sh);";
            this.pg_cmd.Parameters.Add(new NpgsqlParameter("sh", NpgsqlDbType.Integer));
            this.pg_cmd.Prepare();
        }

        public void doQuery()
        {
            this.pg_cmd.Parameters["sh"].Value = shift.Next(60);
            DateTime start = DateTime.Now;
            try
            {
                this.pg_cmd.ExecuteNonQuery();
                DateTime finish = DateTime.Now;
                this.thQueryCount++;
                TimeSpan time = finish - start;
                this.thTimes[this.thTimesPointer] = time;
                this.thTimesPointer++;
                if (this.thTimesPointer == this.thTimes.Length) this.thTimesPointer = 0;
                long averageTicks = Convert.ToInt64(this.thTimes.Average(el => el.Ticks));
                this.thAverageTime = new TimeSpan(averageTicks);
            }
            catch (Exception e)
            {
                this.thErrorCount++;
            }
        }
    }
}
