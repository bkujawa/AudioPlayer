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
        private SoundEngine engine = new SoundEngine();
        private SoundState currentState;
        private DispatcherTimer timer;

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
                this.currentSound = $"Currently playing: " + value;
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

        public ObservableCollection<Sound> Sounds { get; private set; }
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
        public AudioPlayerViewModel()
        {
            Play = new RelayCommand(DoPlay, CanPlay);
            Open = new RelayCommand(DoOpen, CanOpen);
            Pause = new RelayCommand(DoPause, CanPause);
            Stop = new RelayCommand(DoStop, CanStop);
            Next = new RelayCommand(DoNext, CanDoNext);
            Previous = new RelayCommand(DoPrevious, CanDoPrevious);
            Up = new RelayCommand(DoUp, CanOpen);
            Down = new RelayCommand(DoDown, CanOpen);
            Mute = new RelayCommand(DoMute, CanOpen);
            Sounds = new ObservableCollection<Sound>();
            this.engine.StateChanged += OnStateChanged;
            this.engine.SoundError += OnSoundError;
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1);
            this.timer.Tick += OnTick;
            this.timer.Start();

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var dir = config.AppSettings.Settings["DirPath"]?.Value;
            if (!string.IsNullOrEmpty(dir))
                FillSounds(dir);
        }

        private void OnTick(object sender, EventArgs s)
        {
            Progress = this.engine.GetFilePosition();
            UpdateTime();
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

        private void DoUp(object obj) => engine.Volume(true);
        private void DoDown(object obj) => engine.Volume(false);
        private void DoMute(object obj) => engine.Volume(null);

        private bool CanOpen(object obj) => true;
        private void DoOpen(object obj)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();

            var dirPath = dialog.SelectedPath;
            FillSounds(dirPath);
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

        private void FillSounds(string dirPath)
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
