using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SiteWatcher
{
    public class ComboBoxUtils{
        public static bool GetProtectText(DependencyObject obj)
        {
            return (bool)obj.GetValue(ProtectTextProperty);
        }

        public static void SetProtectText(DependencyObject obj, bool value)
        {
            obj.SetValue(ProtectTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for ProtectText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProtectTextProperty =
            DependencyProperty.RegisterAttached("ProtectText", typeof(bool), typeof(ComboBoxUtils), new FrameworkPropertyMetadata(OnProtectTextChanged));

        private static void OnProtectTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as ComboBox;
            if (box == null) return;
            if (true.Equals(e.NewValue)){
                box.SelectionChanged += OnSelectionChanged;
            }else{
                box.SelectionChanged -= OnSelectionChanged;
            }
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ComboBox;
            if (box == null) return;
            if (box.SelectedIndex != -1){
                box.SelectedIndex = -1;
                box.SelectedItem = null;
            }
        }
    }

    public class TextBoxUtils{

        public static string GetCheckRegexText(DependencyObject obj){
            return (string)obj.GetValue(CheckRegexProperty);
        }

        public static void SetCheckRegexText(DependencyObject obj, string value){
            obj.SetValue(CheckRegexProperty, value);
        }
        public static readonly DependencyProperty CheckRegexProperty =
            DependencyProperty.RegisterAttached("CheckRegexText", typeof(string), typeof(TextBoxUtils), new FrameworkPropertyMetadata(OnCheckRegexChanged));

        private static void OnCheckRegexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e){
            var box = d as TextBox;
            if (box == null) return;
            string oldVal="";
            int oldPos=0;
            void OnTextChanged(object sender, TextChangedEventArgs e){
                if(!Regex.IsMatch(box.Text,(string)d.GetValue(CheckRegexProperty))){
                    e.Handled = true;
                    box.Text= oldVal;
                    box.SelectionStart=oldPos;
                }
                
            }
            void OnPreviewTextInput(object sender, TextCompositionEventArgs e){
                oldVal=box.Text;
                oldPos=box.SelectionStart;
            }
            void OnCommandExecuted(object sender, RoutedEventArgs e){
                if (((ExecutedRoutedEventArgs)e).Command != ApplicationCommands.Paste) return;
                oldVal=box.Text;
                oldPos=box.SelectionStart;
            }
            void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                oldVal=box.Text;
                oldPos=box.SelectionStart;
            }
            if (!String.IsNullOrWhiteSpace(e.NewValue as string)){
                d.SetValue(CheckRegexProperty,e.NewValue);
                box.PreviewTextInput += OnPreviewTextInput;
                box.TextChanged += OnTextChanged;
                box.AddHandler(CommandManager.PreviewExecutedEvent, new RoutedEventHandler(OnCommandExecuted), true);
                box.PreviewKeyDown += OnPreviewKeyDown;
            }else{
                d.ClearValue(CheckRegexProperty);
                box.TextChanged -= OnTextChanged;
                box.PreviewTextInput -= OnPreviewTextInput;
                box.RemoveHandler(CommandManager.PreviewExecutedEvent, new RoutedEventHandler(OnCommandExecuted));
                box.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }


        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            throw new NotImplementedException();
        }


        /*         private void TextBoxPasting(object sender, DataObjectPastingEventArgs e){
                    if (e.DataObject.GetDataPresent(typeof(String)))
                    {
                        String text = (String)e.DataObject.GetData(typeof(String));
                        if (!IsTextAllowed(text))
                        {
                            e.CancelCommand();
                        }
                    }
                    else
                    {
                        e.CancelCommand();
                    }
                } */
    }
}
