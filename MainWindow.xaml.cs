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
        private const string LauncherVersion = "1.1.5";
        private readonly string launcherPath;
        private readonly string gameZip;
        private readonly string gameExe;
        private readonly string ZipName = "NewVersion";
        private readonly string GameFolderName = "Game";
        private readonly string GameName = "Billboard Shooter.exe";
        private const string CommitsURL = "https://api.github.com/repos/Poshy163/Billboard-Game/commits";
        private const string ZipURL = "https://api.github.com/repos/Poshy163/Billboard-Game/zipball";
        private string lastCommit;

        private readonly string Locationtxt;
        private readonly string InstallLocation;
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
            VersionText.Text = "";
            Progress.Visibility = Visibility.Hidden;
            InstallLocation = @$"C:\Users\{Environment.UserName}\Videos"; //Use this to change the install location
            launcherPath = Directory.GetCurrentDirectory();
            gameZip = Path.Combine(launcherPath, ZipName + ".zip");
            gameExe = Path.Combine(InstallLocation, GameFolderName, GameName);
            Locationtxt = Path.Combine(InstallLocation, GameFolderName, "MonoBleedingEdge", "Location.txt");
        }

        private void CheckForUpdates()
        {
            string localVersion = "0.0.0";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                string json = client.GetAsync(CommitsURL).Result.Content.ReadAsStringAsync().Result;
                dynamic commits;
                try { commits = JArray.Parse(json); } catch { MessageBox.Show(json + ""); return; }
                lastCommit = commits[0].commit.message;
                string OnlineVerion = lastCommit.Split("\n")[0].Split(" ")[1];
                try
                {
                    localVersion = File.ReadAllText(Path.Combine(InstallLocation, GameFolderName, "MonoBleedingEdge", "Version.txt"));
                }
                catch { }
                VersionText.Text = localVersion;
                try
                {
                    if (localVersion != OnlineVerion)
                    {
                        VersionText.Visibility = Visibility.Hidden;
                        Progress.Visibility = Visibility.Visible;
                        Status = LauncherStatus.downloadingGame;
                        InstallGameFiles(true);
                    }
                    else
                    {
                        SendCurrentLocation();
                        Status = LauncherStatus.ready;
                        return;
                    }
                }
                catch
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Unable to reach GitHub servers");
                }
            }
            catch
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Unable to reach Github servers");
            }
        }

        private void InstallGameFiles(bool _isUpdate)
        {
            if (Status == LauncherStatus.failed)
            {
                MessageBox.Show("BREAKING");
            }

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
                webClient.DownloadFileAsync(new Uri(ZipURL), ZipName + ".zip");
                Progress.Value++;
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
            }
            catch
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (Directory.Exists(Path.Combine(InstallLocation, GameFolderName)))
                {
                    Directory.Delete(Path.Combine(InstallLocation, GameFolderName), true);
                }
                Progress.Value++;
                ZipFile.ExtractToDirectory(gameZip, Path.Combine(InstallLocation, "TempFolder"), true);
                Progress.Value++;
                Directory.Move(Path.Combine(InstallLocation, "TempFolder", FindFolder()), Path.Combine(InstallLocation, GameFolderName));
                Progress.Value++;
                Directory.Delete(Path.Combine(InstallLocation, "TempFolder"));
                Progress.Value++;
                File.Delete(gameZip);
                Progress.Value++;
                Status = LauncherStatus.ready;
                try
                {
                    VersionText.Text = File.ReadAllText(Path.Combine(InstallLocation, GameFolderName, "MonoBleedingEdge", "Version.txt"));
                }
                catch { }
                SendCurrentLocation();
                Progress.Visibility = Visibility.Hidden;
                VersionText.Visibility = Visibility.Visible;
            }
            catch
            {
                Status = LauncherStatus.failed;
                MessageBox.Show("HIT");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void SendCurrentLocation()
        {
            if (File.Exists(Locationtxt))
            {
                File.Delete(Locationtxt);
                using StreamWriter outputFile = new StreamWriter(Locationtxt);
                outputFile.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "GameLauncher.exe"));
                outputFile.Close();
            }
            else
            {
                using StreamWriter outputFile = new StreamWriter(Locationtxt);
                outputFile.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), "GameLauncher.exe"));
                outputFile.Close();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                Process.Start(new ProcessStartInfo(gameExe));
                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }

        private string FindFolder()
        {
            string searchQuery = "*" + "Poshy163-Billboard-Game" + "*"; // Change the search query if needed to find the folder, This is normally the repo name
            string folderName = InstallLocation; // This is the name of the folder
            DirectoryInfo directory = new DirectoryInfo(folderName);
            DirectoryInfo[] directories = directory.GetDirectories(searchQuery, SearchOption.AllDirectories);
            foreach (DirectoryInfo d in directories)
            {
                string loction = d.ToString();
                string[] temp = loction.Split(char.Parse(@"\"));
                return temp[^1];
            }
            MessageBox.Show("Cannot Find Folder");
            return null;
        }

        private void Info(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Launcher Version {LauncherVersion} \nGame Install Location: {InstallLocation}\n");
        }
    }
}