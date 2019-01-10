using Microsoft.Win32;
using MyRide;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;


namespace MyRideApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static List<SynchronizedDirectory> _directories = MyRideApp.AppManagement.settings.synchronizedDirectories;

        NotifyIcon notifyIcon = new NotifyIcon();

        public MainWindow()
        {
            notifyIcon.Click += new EventHandler(Maximize);
            InitializeComponent();
            if(AppManagement.backgroundProcessing)
                ChangeAutoSynchronizeStatus();
            if(AppManagement.settings.windowMinimized)
                Minimize();
        }

        private void RefreshListBox()
        {
            directoriesListBox.Items.Clear();

            foreach (var directory in _directories)
            {
                AddToListBox(directory.name);
            }
        }

        private void AddToListBox(string item)
        {
            directoriesListBox.Items.Add(item);
        }

        private void directoriesListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = directoriesListBox.SelectedIndex;
            if (index != -1)
            {
                SynchronizedDirectory dir = _directories[index];

                nameTextBox.Text = dir.name;
                locationTextBox.Text = dir.path;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            RefreshListBox();
        }

        private void AddButtonClicked(object sender, RoutedEventArgs e)
        {
            SynchronizedDirectory dir = new SynchronizedDirectory();
            dir.name = nameTextBox.Text;
            dir.path = locationTextBox.Text;
            
            _directories.Add(dir);
            AppManagement.SaveSettings();

            AddToListBox(dir.name);
        }

        private void DeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            var index = directoriesListBox.SelectedIndex;
            if (index != -1)
            {
                _directories.RemoveAt(index);
                AppManagement.SaveSettings();
                RefreshListBox();
            }
        }

        private void PushButtonClicked(object sender, RoutedEventArgs e)
        {
            Push();
        }

        private void Push()
        {
            statusListBox.Items.Clear();
            string result = AppManagement.Push(0);
            statusListBox.Items.Add(result);
        }

        private void PullButtonClicked(object sender, RoutedEventArgs e)
        {
            Pull();
        }

        private void Pull()
        {
            statusListBox.Items.Clear();
            string result = AppManagement.Pull();
            statusListBox.Items.Add(result);
        }

        private void AutoSynchronizeButtonClicked(object sender, RoutedEventArgs e)
        {
            ChangeAutoSynchronizeStatus();
        }

        private void ChangeAutoSynchronizeStatus()
        {
            if (!AppManagement.backgroundProcessing)
            {
                AppManagement.AutoSynchronize(notifyIcon);
                autoSynchronizeButton.Content = "Disable Auto Synchronize";
            }
            else
            {
                AppManagement.backgroundProcessing = false;
                autoSynchronizeButton.Content = "Enable Auto Synchronize";
            }
        }

        private void Minimize()
        {
            Hide();
            notifyIcon.Text = "MyRaid";
            if (AppManagement.backgroundProcessing)
            {
                notifyIcon.Icon = new Icon(@"iconOn.ico");
            }
            else
            {
                notifyIcon.Icon = new Icon(@"iconOff.ico");
            }
            notifyIcon.Visible = true;
            AppManagement.settings.windowMinimized = true;
        }

        private void Maximize(object sender, EventArgs e)
        {
            Show();
            this.WindowState = WindowState.Normal;
            notifyIcon.Visible = false;
            AppManagement.settings.windowMinimized = false;
        }

        private void OnStateChange(object sender, EventArgs e)
        {
            if(this.WindowState == WindowState.Minimized)
                Minimize();
        }

       private void WindowClosing(object sender, CancelEventArgs e)
       {
           AppManagement.backgroundProcessing = false;
       }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                locationTextBox.Text = dlg.SelectedPath;
            }
        }

    }
}
