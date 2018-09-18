﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Songify_Slim
{
    using System.Windows.Media;

    using Clipboard = System.Windows.Clipboard;

    public partial class MainWindow
    {
        private readonly string[] colors = new string[]
                                               {
                                                   "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald",
                                                   "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta",
                                                   "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve",
                                                   "Taupe", "Sienna"
                                               };

        private readonly FolderBrowserDialog fbd = new FolderBrowserDialog();

        public NotifyIcon NotifyIcon = new NotifyIcon();

        private readonly System.Windows.Forms.ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();

        private readonly System.Windows.Forms.MenuItem menuItem1 = new System.Windows.Forms.MenuItem();

        private readonly System.Windows.Forms.MenuItem menuItem2 = new System.Windows.Forms.MenuItem();

        private string currentsong;

        public static string Version;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void ThemeToggleSwitchIsCheckedChanged(object sender, EventArgs e)
        {
            Settings.SetTheme(this.ThemeToggleSwitch.IsChecked == true ? "BaseDark" : "BaseLight");
            ThemeHandler.ApplyTheme();
        }

        private void ComboBoxColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(this.ComboBoxColor.SelectedValue.ToString());
            ThemeHandler.ApplyTheme();
            if (Settings.GetColor() != "Yellow")
            {
                this.LblStatus.Foreground = Brushes.White;
                this.LblCopyright.Foreground = Brushes.White;
            }
            else
            {
                this.LblStatus.Foreground = Brushes.Black;
                this.LblCopyright.Foreground = Brushes.Black;
            }
        }

        private void MetroWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.menuItem1.Text = @"Exit";
            this.menuItem1.Click += this.MenuItem1Click;

            this.menuItem2.Text = @"Show";
            this.menuItem2.Click += this.MenuItem2Click;

            this.contextMenu.MenuItems.AddRange(new[] { this.menuItem2, this.menuItem1 });

            this.NotifyIcon.Icon = Properties.Resources.songify;
            this.NotifyIcon.ContextMenu = this.contextMenu;
            this.NotifyIcon.Visible = true;
            this.NotifyIcon.DoubleClick += this.MenuItem2Click;
            this.NotifyIcon.Text = @"Songify";

            foreach (var s in this.colors)
            {
                this.ComboBoxColor.Items.Add(s);
            }

            foreach (string s in this.ComboBoxColor.Items)
            {
                if (s != Settings.GetColor()) continue;
                this.ComboBoxColor.SelectedItem = s;
                Settings.SetColor(s);
            }

            this.ThemeToggleSwitch.IsChecked = Settings.GetTheme() == "BaseDark";
            ThemeHandler.ApplyTheme();
            this.TxtbxOutputdirectory.Text = Assembly.GetEntryAssembly().Location;
            if (!string.IsNullOrEmpty(Settings.GetDirectory()))
                this.TxtbxOutputdirectory.Text = Settings.GetDirectory();

            this.ChbxAutostart.IsChecked = Settings.GetAutostart();
            this.ChbxMinimizeSystray.IsChecked = Settings.GetSystray();

            if (this.WindowState == WindowState.Minimized) this.MinimizeToSysTray();

            this.CheckForUpdates();

            this.StartTimer(1000);
        }

        private void CheckForUpdates()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;
            try
            {
                Updater.CheckForUpdates(new Version(Version));
            }
            catch
            {
                this.LblStatus.Content = "Unable to check for newer version.";
            }
        }

        private void StartTimer(int ms)
        {
            var timer = new System.Timers.Timer();
            timer.Elapsed += this.OnTimedEvent;
            timer.Interval = ms;
            timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            this.GetCurrentSong();
        }

        private void GetCurrentSong()
        {
            var processes = Process.GetProcessesByName("Spotify");

            foreach (var process in processes)
            {
                if (process.ProcessName != "Spotify" || string.IsNullOrEmpty(process.MainWindowTitle)) continue;
                var wintitle = process.MainWindowTitle;
                if (wintitle == "Spotify") continue;
                if (this.currentsong == wintitle) continue;
                this.currentsong = wintitle;
                Console.WriteLine(wintitle);
                if (string.IsNullOrEmpty(Settings.GetDirectory()))
                {
                    File.WriteAllText(
                        Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Songify.txt",
                        this.currentsong + @"               ");
                }
                else
                {
                    File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", this.currentsong + @"               ");
                }

                this.TxtblockLiveoutput.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => { this.TxtblockLiveoutput.Text = this.currentsong; }));
            }
        }

        private void BtnOutputdirectoryClick(object sender, RoutedEventArgs e)
        {
            this.fbd.Description = @"Path where the text file will be located.";
            this.fbd.SelectedPath = Assembly.GetExecutingAssembly().Location;

            if (this.fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            this.TxtbxOutputdirectory.Text = this.fbd.SelectedPath;
            Settings.SetDirectory(this.fbd.SelectedPath);
        }

        private void ChbxAutostartChecked(object sender, RoutedEventArgs e)
        {
            var chbxAutostartIsChecked = this.ChbxAutostart.IsChecked;
            RegisterInStartup(chbxAutostartIsChecked != null && (bool)chbxAutostartIsChecked);
        }

        private static void RegisterInStartup(bool isChecked)
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                true);
            if (isChecked)
            {
                registryKey?.SetValue("Songify", Assembly.GetEntryAssembly().Location);
            }
            else
            {
                registryKey?.DeleteValue("Songify");
            }

            Settings.SetAutostart(isChecked);
        }

        private void MetroWindowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized) return;
            this.MinimizeToSysTray();
        }

        private void MinimizeToSysTray()
        {
            if (Settings.GetSystray())
            {
                this.Hide();
            }
        }

        private void MenuItem2Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void MenuItem1Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ChbxMinimizeSystrayChecked(object sender, RoutedEventArgs e)
        {
            var isChecked = this.ChbxMinimizeSystray.IsChecked;
            Settings.SetSystray(isChecked != null && (bool)isChecked);
        }

        private void MetroWindowClosed(object sender, EventArgs e)
        {
            this.NotifyIcon.Visible = false;
            this.NotifyIcon.Dispose();
        }

        private void BtnUpdatesClick(object sender, RoutedEventArgs e)
        {
            this.CheckForUpdates();
        }

        private void BtnDonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/inzaniity");
        }

        private void BtnDiscordClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void BtnGitHubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Inzaniity/Songify");
        }

        private void BtnAboutClick(object sender, RoutedEventArgs e)
        {
            this.FlyoutAbout.IsOpen = (!this.FlyoutAbout.IsOpen);
        }

        private void BtnCopyToClipClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Settings.GetDirectory()))
            {
                Clipboard.SetDataObject(Assembly.GetEntryAssembly().Location + "\\Songify.txt");
            }
            else
            {
                Clipboard.SetDataObject(Settings.GetDirectory() + "\\Songify.txt");
            }
            this.LblStatus.Content = @"Path copied to clipboard.";
        }
    }
}