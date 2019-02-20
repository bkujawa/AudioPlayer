using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NewAudioPlayer
{
    public class SoundEngine
    {
        private MediaPlayer player = new MediaPlayer();
        private SoundState state;

        private string currentPath;
        

        // Event handlers used in OnStateChanged and OnError methods.
        public event EventHandler<SoundEngineEventArgs> StateChanged;
        public event EventHandler<SoundEngineErrorArgs> SoundError;

        public void Play(string path)
        {
            try
            {
                if (this.currentPath == path && (this.state == SoundState.Paused || this.state == SoundState.Stopped))
                {
                    this.player.Play();
                }
                else
                {
                    this.player.Open(new Uri(path));
                    this.currentPath = path;
                    this.player.Play();
                }
                OnStateChanged(SoundState.Playing);
            }
            catch(Exception ex)
            {
                OnStateChanged(SoundState.Unknown);
                OnError(ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                this.player.Stop();
                OnStateChanged(SoundState.Stopped);
            }
            catch(Exception ex)
            {
                OnStateChanged(SoundState.Unknown);
                OnError(ex.Message);
            }
        }

        public void Pause()
        {
            try
            {
                this.player.Pause();
                OnStateChanged(SoundState.Paused);
            }
            catch(Exception ex)
            {
                OnStateChanged(SoundState.Unknown);
                OnError(ex.Message);
            }
        }

        public void Volume(bool? up)
        {
            try
            {
                if (up == true)
                    this.player.Volume += 0.05;
                else if (up == false)
                    this.player.Volume -= 0.05;
                else
                {
                    if (this.player.IsMuted == true)
                        this.player.IsMuted = false;
                    else
                        this.player.IsMuted = true;
                }
            }
            catch(Exception ex)
            {
                OnError(ex.Message);
            }       
        }

        public double GetFilePosition()
        {
            if (state != SoundState.Playing && state != SoundState.Paused)
                return 0;
            if (this.player.NaturalDuration.HasTimeSpan)
                return Math.Min(100, 100 * this.player.Position.TotalSeconds / this.player.NaturalDuration.TimeSpan.TotalSeconds);
            else
                return 0;
        }

        public Tuple<TimeSpan, TimeSpan> GetTimePosition()
        {
            if (state != SoundState.Playing && state != SoundState.Paused)
                return null;
            if (this.player.NaturalDuration.HasTimeSpan)
                return new Tuple<TimeSpan, TimeSpan>(this.player.Position, this.player.NaturalDuration.TimeSpan);
            else
                return null;
        }

        // Method is called when state is changed.
        private void OnStateChanged(SoundState newState)
        {
            this.state = newState;
            if (StateChanged != null)
                StateChanged(this, new SoundEngineEventArgs(newState));
        }

        // Method is called when changing state causes errors.
        private void OnError(string error)
        {
            if (SoundError != null)
                SoundError(this, new SoundEngineErrorArgs(error));
        }
    }

    // Class representing error arguments.
    public class SoundEngineErrorArgs
    {
        public string ErrorDetails { get; private set; }
        public SoundEngineErrorArgs(string error)
        {
            ErrorDetails = error;
        }
    }

    // Class representing event arguments.
    public class SoundEngineEventArgs : EventArgs
    {
        public SoundState NewState { get; private set; }
        public SoundEngineEventArgs(SoundState newState)
        {
            NewState = newState;
        }
    }

    public enum SoundState
    {
        Unknown,
        Playing,
        Paused,
        Stopped,
        Muted
    }

}
