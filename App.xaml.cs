global using static SiteWatcher.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;

namespace SiteWatcher
{
    public partial class App : Application
    {
        public static string generateGuid(string salt){
            using (MD5 md5 = MD5.Create()){
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(salt+Utils.AppPath));
                Guid result = new Guid(hash);
                return result.ToString();
            }
        }
        private string UniqueEventName = generateGuid("event");
        private string UniqueMutexName =  generateGuid("mutex");
        private EventWaitHandle eventWaitHandle;
        private Mutex mutex;

        private void AppOnStartup(object sender, StartupEventArgs e)
        {
            bool isOwned;
            this.mutex = new Mutex(true, UniqueMutexName, out isOwned);
            this.eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);
            GC.KeepAlive(this.mutex);
            if (isOwned){
                var thread = new Thread(() => {
	                while (this.eventWaitHandle.WaitOne()){
	                    Current.Dispatcher.BeginInvoke(
	                        (Action)(() => ((AppWindow)Current.MainWindow).BringToForeground()));
	                }
                });	
                thread.IsBackground = true;
                thread.Start();
                Start();
                return;
            }
            this.eventWaitHandle.Set();
            this.Shutdown();
        }

        public App(){
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var requestedName = new AssemblyName(args.Name);
            var chromiumDirectory = Path.Combine(AppPath,"libs");
            var assemblyFilename = Path.Combine(chromiumDirectory, requestedName.Name + ".dll");
            if (File.Exists(assemblyFilename)){
                return Assembly.LoadFrom(assemblyFilename);
            }
            return null;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e){
            MessageBox.Show(e.ExceptionObject.ToString(),"Критическая ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
        }

        public void Start()
        {
            CheckBrowser.Init();
            AppWindow appWindow = new AppWindow();
            AppWindowModel appModel = new AppWindowModel(appWindow);

            // Apply minimized setting if configured
            if (Utils.CurrentConfig.StartMinimized)
            {
                appWindow.WindowState = WindowState.Minimized;
                appWindow.Hide();
            }
            else
            { 
                appWindow.Show();    
            }

            
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        protected override void OnExit(ExitEventArgs e){
            if(mutex!=null){
                try{
                    mutex.ReleaseMutex();
                    mutex.Close();
                }catch{

                }
            }
            CheckBrowser.DeInit();
            base.OnExit(e);
        }

    }
}
