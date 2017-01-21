using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;

namespace DemoQueryStore
{
    class Program
    {
        const string connectionstring = "Server=1.11.1.1;database=DemoQueryStore;user id=xx;password=xxx;Application Name=DemoQueryStore";
       
        public Program()
        {
        }

        static void Main(string[] args)
        {
            var p = new Program();
            p.Start();
        }

        #region private method

        private void Start()
        {
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = 2;

            var t1 = Task.Factory.StartNew(() =>
            {
                Parallel.For(0, 3, options, (i) => { WorkA(); });
            });

            var t2 = Task.Factory.StartNew(() =>
            {
                Parallel.For(0, 3, options, (i) => { WorkB(); });
            });
            Task.WaitAll(new Task[] { t1, t2 });
            Console.WriteLine("All Tasks Done...");
            Console.ReadLine();
        }

        private void WorkA()
        {
            string taskid = Task.CurrentId.ToString();
            Console.WriteLine("TaskID:{0}....Starting Query", taskid);
            Random ran = new Random();
            int pa1, pa2;
            SqlCommand cmd;
            string sqlstatement = "select * from testA where c1=@pa1 and c2=@pa2";
            using (SqlConnection cn = new SqlConnection(connectionstring))
            {
                for (int i = 0; i < 100000; i++)
                {
                    pa1 = ran.Next(100);
                    pa2 = pa1;
                    cmd = new SqlCommand(sqlstatement, cn);
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.Add("@pa1", System.Data.SqlDbType.Int);
                    cmd.Parameters["@pa1"].Value = pa1;
                    cmd.Parameters.Add("@pa2", System.Data.SqlDbType.Int);
                    cmd.Parameters["@pa2"].Value = pa2;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    if (cn.State == System.Data.ConnectionState.Closed)
                        cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        //return all
                    }
                    dr.Close();
                    sw.Stop();
                    Console.WriteLine("TaskID:{3} ,Serial:{4} 查詢執行完成時間:{0} ,@pa1={1} ,@pa2={2}",
                        sw.Elapsed, pa1, pa2, taskid, i.ToString());

                    if (ran.Next(100) <= 2)//從計畫快取移除所有快取物件，保證下一次執行，強制產生新執行計畫
                    {
                        cmd = new SqlCommand("dbcc freeproccache", cn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("TaskID:{0} ,Serial:{1}...dbcc freeproccache",
                            taskid, i.ToString());
                        Task.Delay(100);
                        //await Task.Delay(100);
                    }
                }
            }
            Console.WriteLine("TaskID:{0}.....Finish", taskid);
        }

        private void WorkB()
        {
            string taskid = Task.CurrentId.ToString();
            Console.WriteLine("TaskID:{0}....Starting Query using SP", taskid);
            Random ran = new Random();
            int pa1;
            SqlCommand cmd;
            string sqlstatement = "usp_TestQueryStore";
            using (SqlConnection cn = new SqlConnection(connectionstring))
            {
                for (int i = 0; i < 20000; i++)
                {
                    pa1 = ran.Next(100);                  
                    cmd = new SqlCommand(sqlstatement, cn);
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add("@inval", System.Data.SqlDbType.Int);
                    cmd.Parameters["@inval"].Value = pa1;                 

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    if (cn.State == System.Data.ConnectionState.Closed)
                        cn.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        //return all
                    }
                    dr.Close();
                    sw.Stop();
                    Console.WriteLine("TaskID:{2} ,Serial:{3} SP執行完成時間:{0} ,@pa1={1}",
                        sw.Elapsed, pa1, taskid, i.ToString());

                    if (ran.Next(100) <= 2)//從計畫快取移除所有快取物件，保證下一次執行，強制產生新執行計畫
                    {
                        cmd = new SqlCommand("dbcc freeproccache", cn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("TaskID:{0} ,Serial:{1}...dbcc freeproccache",
                            taskid, i.ToString());
                        Task.Delay(100);
                        //await Task.Delay(100);
                    }
                }
            }
            Console.WriteLine("TaskID:{0}.....Finish", taskid);
        }

        #endregion
    }
}
