using AvdanyuScraper.Services;
using System;
using System.IO;
using System.Linq;
using Serilog;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace AvdanyuScraper
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            ConfigureLogging();
            var movieInfoSrv = new MovieInformationService();
            var nfoSrv = new NfoService();
            var fbd = new FolderBrowserDialog();

            while (fbd.ShowDialog() == DialogResult.OK)
            {
                var folders = Directory.GetDirectories(fbd.SelectedPath);
                Log.Debug($"已选定 {fbd.SelectedPath} ，开始获取元数据...");
                Task[] taskArray = new Task[1];
                var runningTasks = 0;
                for(int i = 0; i < folders.Length; ++i)
                {
                    var folder = folders[i];
                    if (runningTasks < taskArray.Length)
                    {
                        taskArray[runningTasks] = Task.Factory.StartNew(() =>
                        {
                            var searchName = folder.Split("\\").Last();
                            if (searchName != ".actors")
                            {
                                Log.Debug($"Thread {Thread.CurrentThread.ManagedThreadId}: 开始获取 {searchName} 元数据...");
                                var movieInfomation = movieInfoSrv.GetMovieInformation(searchName);
                                nfoSrv.UpdateNfoData(movieInfomation, folder);
                                Log.Debug($"Thread {Thread.CurrentThread.ManagedThreadId}: 完成获取 {searchName} 元数据！\n\r");
                            }
                        });
                        runningTasks++;
                    }
                    if (runningTasks == taskArray.Length)
                    {
                        Task.WaitAll(taskArray);
                        runningTasks = 0;
                    }
                }
                Task.WaitAll(taskArray);
                Log.Debug($"{fbd.SelectedPath} 的元数据已全部获取！");
            }
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"logs/logs-{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt")
                .CreateLogger();
        }
    }
}
