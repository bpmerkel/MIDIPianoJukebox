using Commons.Music.Midi;
using Commons.Music.Midi.WinMM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace MIDIPianoJukebox.Data
{
    public partial class JukeboxService : IDisposable
    {
        private MidiPlayer currentplayer = null;
        private IMidiOutput outputDevice = null;
        public event EventHandler ReadyToPlayNext;
        readonly SynchronizationContext main = SynchronizationContext.Current;
        bool isStopping = false;

        private void PlayPlayer()
        {
            // spin wait until stopped, if pending a stop
            while (isStopping) Thread.Sleep(100);

            if (currentplayer == null)
            {
                var music = getMusic();
                if (music != null)
                {
                    outputDevice = getDevice();
                    currentplayer = new MidiPlayer(music, outputDevice);
                    jukebox.Current.TotalTime = currentplayer.GetTotalPlayTimeMilliseconds();
                    jukebox.Current.CurrentTime = TimeSpan.FromMilliseconds(0);
                    jukebox.Current.RemainingTime = TimeSpan.FromMilliseconds(jukebox.Current.TotalTime);
                    currentplayer.PlaybackCompletedToEnd += player_Finished;
                    currentplayer.EventReceived += player_EventReceived;
                    currentplayer.Play();
                    jukebox.Current.State = States.Playing;
                }
                else
                {
                    // found a missing/bad file; so signal move ahead
                    signal_next();
                }
            }
        }

        private void player_Finished()
        {
            // will need this to signal next tune in the queue
            jukebox.Current.Tune.Plays += 1;
            signal_next();
        }

        private void signal_next()
        {
            // will need this to signal next tune in the queue
            main.Post(state => ReadyToPlayNext?.Invoke(this, new EventArgs()), null);
        }

        private void player_EventReceived(MidiEvent m)
        {
            if (currentplayer == null) return;
            if (currentplayer.State != PlayerState.Playing) return;

            // be aware, if user does play next when event is firing, player may come back as null...
            // a.EventType, a.Channel, a.StatusByte, a.Value
            var prior = $"{jukebox.Current.Progress:P0}-{jukebox.Current.CurrentTime.TotalSeconds:#}";
            var time = currentplayer?.PositionInTime.TotalMilliseconds ?? 0;
            var progress = time / (jukebox.Current.TotalTime + 1);
            jukebox.Current.CurrentTime = TimeSpan.FromMilliseconds(time);
            jukebox.Current.RemainingTime = TimeSpan.FromMilliseconds(jukebox.Current.TotalTime - time);

            var newp = $"{progress:P0}-{jukebox.Current.CurrentTime.TotalSeconds:#}";
            if (prior != newp) jukebox.Current.Progress = progress;  // which also signals a UI update
        }

        private void StopPlayer()
        {
            if (currentplayer == null) return;
            if (isStopping)
            {
                // spin wait until stopped
                while (isStopping) Thread.Sleep(100);
            }
            else
            {
                isStopping = true;
                currentplayer.Stop();
                // spin wait until stopped
                while (currentplayer.State != PlayerState.Stopped) Thread.Sleep(100);
                currentplayer.EventReceived -= player_EventReceived;
                currentplayer.PlaybackCompletedToEnd -= player_Finished;
                jukebox.Current.Progress = 0d;
                jukebox.Current.CurrentTime = TimeSpan.FromMilliseconds(0);
                jukebox.Current.RemainingTime = TimeSpan.FromMilliseconds(jukebox.Current.TotalTime);
                jukebox.Current.State = States.Stopped;
                currentplayer.Dispose();
                outputDevice.Dispose();
                outputDevice = null;
                currentplayer = null;
                isStopping = false;
            }
        }

        public void PausePlayer()
        {
            if (currentplayer == null) return;
            if (currentplayer.State != PlayerState.Paused)
            {
                try
                {
                    currentplayer.Pause();
                    jukebox.Current.State = States.Paused;
                }
                catch (Win32Exception) { }
            }
        }

        public void ResumePlayer()
        {
            if (currentplayer == null) return;
            if (currentplayer.State != PlayerState.Playing)
            {
                currentplayer.Play();
                jukebox.Current.State = States.Playing;
            }
        }

        public void SkipPlayer(int ticks)
        {
            if (currentplayer == null) return;
            SkipPlayerTo(currentplayer.PlayDeltaTime + ticks);
        }

        // perform an absolute skip
        public void SkipPlayerTo(int ticks)
        {
            if (currentplayer == null || ticks < 0) return;
            currentplayer.Seek(ticks);
            ResumePlayer();
        }

        private MidiMusic getMusic()
        {
            try
            {
                using var sr = File.OpenRead(Path.Combine(jukebox.Settings.MIDIPath, jukebox.Current.Tune.Filepath));
                return MidiMusic.Read(sr);
            }
            catch
            {
                return null;
            }
        }

        public List<IMidiPortDetails> GetDevices()
        {
            var wmm = new WinMMMidiAccess();
            return wmm.Outputs.ToList();
        }

        public IMidiOutput getDevice()
        {
            var wmm = new WinMMMidiAccess();
            var dev = wmm.OpenOutputAsync(jukebox.Settings.OutputDevice ?? "0").Result;
            return dev;
        }

        public void Dispose()
        {
            if (currentplayer != null) StopPlayer();
            GC.SuppressFinalize(this);
        }
    }
}
