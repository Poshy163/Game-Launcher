using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Windows;

namespace GameLauncher
{
    internal enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class MainWindow : Window
    {
        private readonly string rootPath;
        private readonly string gameZip;
        private readonly string gameExe;
        private readonly string ZipName = "NewVersion";
        private readonly string GameFolderName = "Game";
        private readonly string GameName = "Billboard Shooter.exe";

        private string lastCommit;
        private LauncherStatus _status;

        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;

                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;

                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;

                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;

                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Progress.Visibility = Visibility.Hidden;
            rootPath = Directory.GetCurrentDirectory();
            gameZip = Path.Combine(rootPath, ZipName + ".zip");
            gameExe = Path.Combine(rootPath, GameFolderName, GameName);
        }

        private void CheckForUpdates()
        {
            string localVersion = "0.0.0";
            string OnlineVerion = "0.0.0.0";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                string json = client.GetAsync("https://api.github.com/repos/Poshy163/Billboard-Game/commits").Result.Content.ReadAsStringAsync().Result;
                dynamic commits;
                try { commits = JArray.Parse(json); } catch { MessageBox.Show(json + ""); return; }
                lastCommit = commits[0].commit.message;
                OnlineVerion = lastCommit.Split("\n")[0].Split(" ")[1];
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for game updates: {ex}");
            }
            try
            {
                localVersion = File.ReadAllText(Path.Combine(rootPath, GameFolderName, "Version.txt"));
            }
            catch { }
            VersionText.Text = localVersion;

            try
            {
                if (localVersion != OnlineVerion)
                {
                    Progress.Visibility = Visibility.Visible;
                    Status = LauncherStatus.downloadingGame;
                    InstallGameFiles(true);
                }
                else
                {
                    Status = LauncherStatus.ready;
                    return;
                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for game updates: {ex}");
            }
        }

        private void InstallGameFiles(bool _isUpdate)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                Progress.Value++;
                webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " + "Windows NT 5.2; .NET CLR 1.0.3705;)");
                Progress.Value++;
                webClient.DownloadFileAsync(new Uri("https://api.github.com/repos/Poshy163/Billboard-Game/zipball"), ZipName + ".zip");
                Progress.Value++;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (Directory.Exists(Path.Combine(rootPath, GameFolderName)))
                {
                    Directory.Delete(Path.Combine(rootPath, GameFolderName), true);
                }
                Progress.Value++;
                ZipFile.ExtractToDirectory(gameZip, Path.Combine(rootPath, "TempFolder"), true);
                Progress.Value++;
                Directory.Move(Path.Combine(rootPath, "TempFolder", FindFolder()), Path.Combine(rootPath, GameFolderName));
                Progress.Value++;
                Directory.Delete(Path.Combine(rootPath, "TempFolder"));
                Progress.Value++;
                File.Delete(gameZip);
                Progress.Value++;
                Status = LauncherStatus.ready;
                Progress.Visibility = Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                Process.Start(startInfo);
                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                MessageBox.Show("Launcher Failed, retrying");
                CheckForUpdates();
            }
        }

        private string FindFolder()
        {
            string searchQuery = "*" + "Poshy163-Billboard-Game" + "*";
            string folderName = rootPath;
            DirectoryInfo directory = new DirectoryInfo(folderName);
            DirectoryInfo[] directories = directory.GetDirectories(searchQuery, SearchOption.AllDirectories);
            foreach (DirectoryInfo d in directories)
            {
                string loction = d.ToString();
                string[] temp = loction.Split(char.Parse(@"\"));
                return temp[^1];
            }
            return null;
        }
    }
}