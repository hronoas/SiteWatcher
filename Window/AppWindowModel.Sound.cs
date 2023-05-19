using System;
using System.Media;

namespace SiteWatcher
{
    public partial class AppWindowModel : BaseWindowModel<AppWindow>{
        private SoundPlayer soundPlayer = new SoundPlayer(ReadResource("notify.wav").BaseStream);
        private string notifySound = "";

        public string NotifySound { 
            get => notifySound; 
            set{
                notifySound = value;
                try{
                    SoundPlayer newPlayer = new SoundPlayer(notifySound);
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