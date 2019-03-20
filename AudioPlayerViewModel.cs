using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;


namespace NewAudioPlayer
{
    class AudioPlayerViewModel : ViewModelBase
    {
        private SoundEngine engine;
        private SoundState currentState;
        private readonly DispatcherTimer timer;

        private double progress;
        public double Progress
        {
            get { return this.progress; }
            set
            {
                this.progress = value;
                OnPropertyChanged();
            }
        }

        private double volumePosition;
        public double VolumePosition
        {
            get { return this.volumePosition; }
            set
            {
                this.volumePosition = value;
                OnPropertyChanged();
            }
        }

        private string timeInfo;
        public string TimeInfo
        {
            get { return this.timeInfo; }
            set
            {
                this.timeInfo = value;
                OnPropertyChanged();
            }
        }

        private string currentSound;
        public string CurrentSound
        {
            get { return this.currentSound; }
            set
            {
                this.currentSound = $"Currently playing: [" + value + $"]";
                OnPropertyChanged();
            }
        }

        private string savedPlaylistName;

        public string SavedPlaylistName
        {
            get { return this.savedPlaylistName; }
            set
            {
                this.savedPlaylistName = value;
                OnPropertyChanged();
            }
        }

        public ICommand Play { get; private set; }
        public ICommand Pause { get; private set; }
        public ICommand Stop { get; private set; }
        public ICommand Next { get; private set; }
        public ICommand Previous { get; private set; }
        public ICommand Open { get; private set; }
        public ICommand Up { get; private set; }
        public ICommand Down { get; private set; }
        public ICommand Mute { get; private set; }
        public ICommand SavePlaylist { get; private set; }
        public ICommand OpenPlaylist { get; private set; }
        public ICommand DeletePlaylist { get; private set; }
        public ICommand DeleteSound { get; private set; }

        public ObservableCollection<Sound> Sounds { get; private set; }
        public ObservableCollection<Sound> Playlists { get; private set; }
        
        private Sound selectedSound;
        public Sound SelectedSound
        {
            get { return this.selectedSound; }
            set
            {
                this.selectedSound = value;
                OnPropertyChanged();
            }
        }

        private Sound selectedPlaylist;

        public Sound SelectedPlaylist
        {
            get { return this.selectedPlaylist; }
            set
            {
                this.selectedPlaylist = value;
                OnPropertyChanged();
            }
        }
        public AudioPlayerViewModel()
        {
            engine = new SoundEngine();
            Play = new RelayCommand(DoPlay, CanPlay);
            Open = new RelayCommand(DoOpen, CanOpen);
            Pause = new RelayCommand(DoPause, CanPause);
            Stop = new RelayCommand(DoStop, CanStop);
            Next = new RelayCommand(DoNext, CanDoNext);
            Previous = new RelayCommand(DoPrevious, CanDoPrevious);
            Up = new RelayCommand(DoUp, CanOpen);
            Down = new RelayCommand(DoDown, CanOpen);
            Mute = new RelayCommand(DoMute, CanOpen);
            SavePlaylist = new RelayCommand(DoSavePlaylist, CanSavePlaylist);
            OpenPlaylist = new RelayCommand(DoOpenPlaylist, CanOpenPlaylist);
            DeletePlaylist = new RelayCommand(DoDeletePlaylist, CanDeletePlaylist);
            DeleteSound = new RelayCommand(DoDeleteSound, CanDeleteSound);
            Sounds = new ObservableCollection<Sound>();
            Playlists = new ObservableCollection<Sound>();
            this.engine.StateChanged += OnStateChanged;
            this.engine.SoundError += OnSoundError;
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Tick += OnTick;
            this.timer.Start();
            VolumePosition = engine.GetVolumePosition();
            FillPlaylist();

            //var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //var dir = config.AppSettings.Settings["DirPath"]?.Value;
            //var play = config.AppSettings.Settings["PlayPath"]?.Value;
            //try
            //{
            //    if (!string.IsNullOrEmpty(dir))
            //        FillSoundsDirectory(dir);
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var play = config.AppSettings.Settings["PlayPath"]?.Value;
            try
            {
                if (!string.IsNullOrEmpty(play))
                {
                    SelectedPlaylist = new Sound()
                    {
                        Path = play.ToString()
                    };
                    DoOpenPlaylist(null);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Playlist loading error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnTick(object sender, EventArgs s)
        {
            Progress = this.engine.GetFilePosition(); //Check what Progress returns, check if Progress ends and then invoke DoNext
            UpdateTime();
            if (Progress == 100)
            {
                //if (PlayerState == RepeatSound)
                //{
                //      DoPlay(null);
                //}
                //else if (PlayerState == RepeatPlaylist)
                //{
                //      if (!CanDoNext(null))
                //      {
                //          SelectedSound = Sounds[0];
                //          DoPlay(null);
                //      }
                //      else
                //      {
                //          DoNext(null);
                //      }
                //}
                //else if (PlayerState == Shuffle)
                //{
                //      var index = new Random();
                //      SelectedSound = Sounds[index];
                //      DoPlay(null);
                //}
                if (CanDoNext(null))
                    DoNext(null);
            }
        }
        private void UpdateTime()
        {
            var time = this.engine.GetTimePosition();
            if (time == null)
                TimeInfo = "--/--";
            else
                TimeInfo = $"{time.Item1.ToString(@"hh\:mm\:ss")} / {time.Item2.ToString(@"hh\:mm\:ss")}";
        }

        private bool CanDoPrevious(object obj)
        {
            if (SelectedSound == null)
                return false;
            var index = Sounds.IndexOf(SelectedSound);
            return index > 0;
        }
        private void DoPrevious(object obj)
        {
            this.engine.Stop();
            var index = Sounds.IndexOf(SelectedSound);
            SelectedSound = Sounds[--index];
            this.engine.Play(SelectedSound.Path);
            CurrentSound = SelectedSound.Name;
        }

        private bool CanDoNext(object obj)
        {
            if (SelectedSound == null)
                return false;
            var index = Sounds.IndexOf(SelectedSound);
            return index < Sounds.Count;
        }
        private void DoNext(object obj)
        {
            this.engine.Stop();
            var index = Sounds.IndexOf(SelectedSound);
            SelectedSound = Sounds[++index];
            this.engine.Play(SelectedSound.Path);
            CurrentSound = SelectedSound.Name;
        }

        private bool CanStop(object obj) => this.currentState == SoundState.Playing || this.currentState == SoundState.Paused;
        private void DoStop(object obj) => this.engine.Stop();

        private bool CanPause(object obj) => this.currentState == SoundState.Playing;
        private void DoPause(object obj) => this.engine.Pause();

        private bool CanPlay(object obj) => SelectedSound != null && this.currentState != SoundState.Playing;
        private void DoPlay(object obj)
        {
            this.CurrentSound = SelectedSound.Name;
            this.engine.Play(SelectedSound.Path);
        }

        private void DoUp(object obj)
        {
            engine.Volume(true);
            VolumePosition = engine.GetVolumePosition();
        }
        private void DoDown(object obj)
        {
            engine.Volume(false);
            VolumePosition = engine.GetVolumePosition();
        }

        private void DoMute(object obj) => engine.Volume(null);

        private bool CanOpen(object obj) => true;
        private void DoOpen(object obj)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();

            var dirPath = dialog.SelectedPath;
            FillSoundsDirectory(dirPath);
            SetDefaultDirectory(dirPath);
        }

        //Fills ObservableCollection<Sound> Sounds with files found in directory pointed by dirPath.
        private void FillSoundsDirectory(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath))
            {
                var allFiles = Directory.GetFiles(dirPath);
                var soundList = new List<Sound>();
                foreach(var file in allFiles)
                {
                    var pathExtension = Path.GetExtension(file);
                    if (pathExtension?.ToUpper() == ".MP3")
                    {
                        soundList.Add(new Sound()
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file
                        });
                    }
                }
                Sounds.Clear();
                soundList.ForEach(c => Sounds.Add(c));
            }
        }

        //Fills ObservableCollection<Sound> Playlists with files found in /user/MyDocuments/AudioPlayer directory.
        private void FillPlaylist()
        {
            string docPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"AudioPlayer");
            var playlists = new List<Sound>();
            try
            {
                var allFiles = Directory.GetFiles(docPath);
                foreach (var file in allFiles)
                {
                    var pathExtension = Path.GetExtension(file);
                    if (pathExtension?.ToUpper() == ".TXT")
                    {
                        playlists.Add(new Sound()
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file
                        });
                    }
                }
                Playlists.Clear();
                playlists.ForEach(c => Playlists.Add(c));
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private bool CanSavePlaylist(object obj) => true;
        private void DoSavePlaylist(object obj)
        {         
            string playlistName = SavedPlaylistName + ".txt";

            // Playlist is saved in default folder /user/MyDocuments/AudioPlayer
            string docPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"AudioPlayer");
            Directory.CreateDirectory(docPath);
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, playlistName)))
            {
                foreach (var sound in Sounds)
                {
                    outputFile.WriteLine(sound.Path);
                    outputFile.WriteLine(sound.Name);
                }
            }
            FillPlaylist();
        }

        private bool CanOpenPlaylist(object obj) => true;
        private void DoOpenPlaylist(object obj)
        {
            if (!string.IsNullOrEmpty(SelectedPlaylist.Path))
            {
                var soundList = new List<Sound>();
                var allFiles = File.ReadAllLines(SelectedPlaylist.Path);
                for (int i = 0; i < allFiles.Length - 1; i = i + 2)
                {
                    soundList.Add(new Sound()
                    {
                        Path = allFiles[i],
                        Name = allFiles[i + 1]
                    });
                }
                Sounds.Clear();
                soundList.ForEach(c => Sounds.Add(c));
                SetDefaultPlaylist(SelectedPlaylist.Path);
            }
        }

        private bool CanDeletePlaylist(object obj) => SelectedPlaylist == null ? false : true;
        private void DoDeletePlaylist(object obj)
        {
            try
            {
                File.Delete(SelectedPlaylist.Path);
                Playlists.Remove(SelectedPlaylist);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private bool CanDeleteSound(object obj) => SelectedSound == null ? false : true;
        private void DoDeleteSound(object obj)
        {
            Sounds.Remove(SelectedSound);
        }


        private void SetDefaultDirectory(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath))
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["DirPath"] != null)
                {
                    config.AppSettings.Settings["DirPath"].Value = dirPath;
                }
                else
                {
                    config.AppSettings.Settings.Add(new KeyValueConfigurationElement("DirPath", dirPath));
                }
                ConfigurationManager.AppSettings["DirPath"] = dirPath;
                config.Save(ConfigurationSaveMode.Full);
            }
        }
        private void SetDefaultPlaylist(string listPath)
        {
            if (!string.IsNullOrEmpty(listPath))
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["PlayPath"] != null)
                {
                    config.AppSettings.Settings["PlayPath"].Value = listPath;
                }
                else
                {
                    config.AppSettings.Settings.Add(new KeyValueConfigurationElement("PlayPath", listPath));
                }
                ConfigurationManager.AppSettings["PlayPath"] = listPath;
                config.Save(ConfigurationSaveMode.Full);
            }
        }

        private void OnSoundError(object sender, SoundEngineErrorArgs e) => MessageBox.Show(e.ErrorDetails, "Sound error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        private void OnStateChanged(object sender, SoundEngineEventArgs e)
        {
            this.currentState = e.NewState;
            UpdateTime();
        }
    }

    public class Sound
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
