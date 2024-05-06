using System;
using System.Media;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>{
        private SoundPlayer soundPlayer = new SoundPlayer(ReadResource("notify.wav").BaseStream);
        public string NotifySound { 
            get => CurrentConfig.NotifySound; 
            set{
                try{
                    SoundPlayer newPlayer = new SoundPlayer(CurrentConfig.NotifySound);
                    newPlayer.Play();
                    newPlayer.Stop();
                    soundPlayer.Dispose();
                    soundPlayer = newPlayer;
                }catch{
                    soundPlayer = new SoundPlayer(ReadResource("notify.wav").BaseStream);
                }
            }
        }

        public void PlaySound(Watch watch){
            if(soundPlayer!=null){
                soundPlayer.Stop();
                soundPlayer.Play();
            }
        }
    }

}