using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>
    {
        private readonly Forms.NotifyIcon Icon=new();
        private WatchStatus iconState = WatchStatus.NoChanges;
        private readonly Icon defaultIcon=System.Drawing.Icon.ExtractAssociatedIcon(AppExe);

        private Dictionary<WatchStatus, Icon> cacheIcons = new();
        public Icon GetIcon(WatchStatus status){
            if(!cacheIcons.ContainsKey(status)){
                Icon ico;
                switch (status){
                    case WatchStatus.Fail:
                        ico = new System.Drawing.Icon(ReadResource("iFail.ico").BaseStream);
                        break;
                    case WatchStatus.Checking:
                        ico = new System.Drawing.Icon(ReadResource("iChecking.ico").BaseStream);
                        break;
                    case WatchStatus.New:
                        ico = new System.Drawing.Icon(ReadResource("iNew.ico").BaseStream);
                        break;
                    case WatchStatus.Off:
                        ico = new System.Drawing.Icon(ReadResource("iOff.ico").BaseStream);
                        break;
                    default:
                        ico = defaultIcon;
                        break;
                }
                cacheIcons[status]=ico;
            }
            return cacheIcons[status];
        }
        public WatchStatus IconState{
            get=>iconState;
            set{
                if(iconState==value && Icon.Icon!=null) return;
                iconState=value;
                Icon.Icon = GetIcon(value);
            }}
        public void InitIcon(){
            ShowIcon();
            //Watches.CollectionChanged+=(e,o)=>RefreshIcon();
            Watches.ListChanged+=(e,o)=>RefreshIcon();
        }
        public void ShowIcon(){
            IconState=WatchStatus.NoChanges;
            Icon.Visible = true;
            Icon.Text=window.Title;
            window.Closed+=(o,e)=>{Icon.Dispose();};
            Icon.MouseClick+=(object sender, Forms.MouseEventArgs e)=>{
                if(e.Button==Forms.MouseButtons.Left){
                    if(window.WindowState==System.Windows.WindowState.Normal){
                        window.WindowState=System.Windows.WindowState.Minimized;
                        window.Hide();
                    }else{
                        window.Show();
                        window.WindowState=System.Windows.WindowState.Normal;
                        window.Activate();
                    }
                    
                }
            };

            void beforeClose(object? o, CancelEventArgs e){
                window.WindowState=System.Windows.WindowState.Minimized;
                window.Hide();
                e.Cancel=true;
            };
            window.Closing+=beforeClose;

            Forms.ContextMenuStrip cm = new Forms.ContextMenuStrip();
            cm.Items.Add("Проверить все",null,(o,e)=>{
                CheckAll();
            });

            cm.Items.Add("О приложении",null,(o,e)=>{
                OpenUrl(Utils.Description);
            });
            
            cm.Items.Add("Выход",null,(o,e)=>{
                window.Closing-=beforeClose;
                window.Close();
            });
            Icon.ContextMenuStrip = cm;
        }

    }
}