using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using log4net;
using System.Threading;
using System.Configuration;

namespace WinService
{
    partial class Service1 : ServiceBase
    {
        // 记录log4net日志
        private static ILog logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        System.Timers.Timer t;  // 计时器
        private bool isFinished = true;     // 标记上次同步服务是否执行完成

        public Service1()
        {
            InitializeComponent();
        }
         
        /// <summary>
        /// 线程开始执行
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            logger.Info("开始启动服务");
            // 计时器每秒执行一次
            t = new System.Timers.Timer(60000);
            // 委托执行
            t.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedExecute);
            // 一次轮询完成以后，自动将计时器重置
            t.AutoReset = true;
            //开始轮询
            t.Start();
            logger.Info("服务启动完成");
        }

        /// <summary>
        /// 定时器委托执行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ElapsedExecute(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                logger.Info("Timer SignalTime:" + e.SignalTime.ToString("yyyy-MM-dd HH:mm:ss"));
                // 注：如果计时器每分钟执行一次，可以不用判断秒，如果计时器每秒执行一次，则必须判断秒
                int iHour = e.SignalTime.Hour;
                int iMinute = e.SignalTime.Minute;

                /* 
                 * 每n分钟执行一次且上一次执行结束后开始执行本次操作（当然如果你能完全确保每次执行
                 * 都能在一个执行中完成，则可以不用判断上一次是否结束）
                 * 这种方式有一个要注意的地方：当n设置为7时，那么当前小时内最后一次执行时间为56分钟
                 * 紧接着到达下一小时的0分钟又会开始执行，也就是在下一小时的最后一次执行和当前小时的
                 * 第一次执行之间时间间隔是4分钟，而不是7分钟，同理只要n设置成不能被60整除的数值
                 * 任务都会有这种问题的存在，所以在设置n时，最好设置为能被60整除的数值
                 * 
                 * 如果要设置每小时03分的时候执行一次（如 01:03，02:03 ...）
                 * if (iMinute == 3)
                 * 如果要设置每两小时执行一次可设置如下判断
                 * if (iHour%2 == 1 && iMinute == 3)  // 表示每两小时03分执行一次（如 01:03，03:03 ...）
                 * 或者 if (iHour%2 == 0 && iMinute == 3)  // 表示每两小时03分执行一次（如 00:03，02:03 ...）
                 * 同理每三小时、每四小时...执行一次把iHour对n取余等于0便可，注意iMinute条件一定要设置，
                 * 否则会变成每n小时内每次定时器执行都会执行执行体
                 * 
                 * 如果要设置每天执行一次那么可以设置如下判断：
                 * if (iHour == 0 && iMinute == 0)      // 表示每天00:00执行一次
                */
                if (iMinute % 3 == 0 && isFinished)
                {
                    try
                    {
                        isFinished = false;      // 标记当前任务未完成状态
                        DateTime startTime = DateTime.Now;
                        logger.Info("======================== 开始执行服务 ========================");

                        int sleepTime = 0;
                        string strSleepTime = ConfigurationManager.AppSettings["SLEEPTIME"];
                        if (!int.TryParse(strSleepTime, out sleepTime))
                            sleepTime = 2;

                        // 测试
                        Execute(sleepTime * 60);

                        DateTime endTime = DateTime.Now;
                        double totalExeTime = (endTime - startTime).TotalMinutes;
                        logger.Info("========= 服务执行结束，本次共花费" + totalExeTime + "分钟，程序等待下一次执行！ =========");
                    }
                    catch (Exception exc)
                    {
                        logger.Error("Message:" + exc.Message + "    StackTrace:" + exc.StackTrace);
                    }
                    finally
                    {
                        // 在finally中执行保证本次执行即使出现异常也能标识本次执行结束，否则便无法再做下一次执行
                        isFinished = true;   // 标记当天本次任务已完成状态
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Message:" + ex.Message + "    StackTrace:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 执行sleepTime休眠
        /// </summary>
        /// <param name="sleepTime">休眠（秒）</param>
        private void Execute(int sleepTime)
        {
            Thread.Sleep(sleepTime * 1000);
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        protected override void OnStop()
        {
            logger.Info("停止服务");
            t.Enabled = false;
        }
    }
}
