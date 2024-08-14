using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SiteWatcher{
    public partial class CheckpointsWindow : Window{
        public CheckpointsWindow(){
            InitializeComponent();
        }
        private void CheckpointsList_DoubleClick(object sender, EventArgs e){
            if (CheckpointsList.SelectedItems.Count == 1) {
                ((CheckpointsWindowModel)DataContext).ToggleMarked(CheckpointsList.SelectedItems[0] as CheckpointDiff);
            }
        }
    }
}
