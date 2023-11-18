using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SiteWatcher
{
    public partial class AppWindow : Window{

        public void BringToForeground(){
            if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden){
                this.Show();
                this.WindowState = WindowState.Normal;
            }
            var b = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Left = Left>(b.Width-Width+50)?(b.Width-Width):Left;
            Top = Top > (b.Height-50)?(b.Height-Height):Top;

            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }
        public AppWindow(){
            InitializeComponent();
            Drag.MouseLeftButtonDown+=(e,o)=>DragMove();
            WatchList.MouseDoubleClick+=WatchList_DoubleClick;
            TagsList.SelectionChanged +=  UndoSelectedIndex;
            TagsList.DropDownOpened += TagsList_DropDown;
            //TagsList.PreviewLostKeyboardFocus += TagsList_LostFocus;
            TagsList.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, 
                      new System.Windows.Controls.TextChangedEventHandler(TagsList_TextChanged));                      
        }

        private void TagsList_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if(((AppWindowModel)DataContext).TextFilter=="")
            ((AppWindowModel)DataContext).TextFilter = null;
        }

        private void TagsList_DropDown(object? sender, EventArgs e)
        {
            AppWindowModel winModel = (AppWindowModel)DataContext;
            winModel.TagsUpdating=true;
            winModel.ShowNew=false;
            winModel.TagsUpdating=false;
            winModel.TextFilter=null;
        }

        private void TagsList_TextChanged(object sender, TextChangedEventArgs e){
            if (((AppWindowModel)DataContext).TagsUpdating) return;
            ComboBox cb = sender as ComboBox;
            if (cb.Text == ((AppWindowModel)DataContext).currentFilterText) return;
            ((AppWindowModel)DataContext).TextFilter = cb.Text;
        }

        private void WatchList_DoubleClick(object sender, EventArgs e){
            if (WatchList.SelectedItems.Count == 1) {
                //((AppWindowModel)DataContext).NavigateWatch(WatchList.SelectedItems[0] as Watch);
                ((AppWindowModel)DataContext).CheckpointsWatch(WatchList.SelectedItems[0] as Watch);
            }
        }
        private void UndoSelectedIndex(object sender, SelectionChangedEventArgs e){
            ComboBox cb = sender as ComboBox;
            if(cb==null) return;
            cb.SelectedIndex=-1;
            e.Handled = true;
        }
    }
}
