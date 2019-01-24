using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;

namespace CataSharp.Client.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase {
        public static string CataLogo => @"
   _________            __                   .__                                
   \_   ___ \ _____   _/  |_ _____     ____  |  |   ___.__   ______  _____      
   /    \  \/ \__  \  \   __\\__  \  _/ ___\ |  |  <   |  | /  ___/ /     \     
   \     \____ / __ \_ |  |   / __ \_\  \___ |  |__ \___  | \___ \ |  Y Y  \    
    \______  /(____  / |__|  (____  / \___  >|____/ / ____|/____  >|__|_|  /    
           \/      \/             \/      \/        \/          \/       \/     ";

        public static string CataLogo2 => @"
    ________                   .__      ________                                
    \______ \  _____   _______ |  | __  \______ \  _____    ___.__   ______     
     |    |  \ \__  \  \_  __ \|  |/ /   |    |  \ \__  \  <   |  | /  ___/     
     |    `   \ / __ \_ |  | \/|    <    |    `   \ / __ \_ \___  | \___ \      
    /_______  /(____  / |__|   |__|_ \  /_______  /(____  / / ____|/____  >     
            \/      \/              \/          \/      \/  \/          \/     ";

        public static string CataLogo3 => @"
                     _____   .__                         .___                   
                    /  _  \  |  |__    ____  _____     __| _/                   
                   /  /_\  \ |  |  \ _/ __ \ \__  \   / __ |                    
                  /    |    \|   Y  \\  ___/  / __ \_/ /_/ |                    
                  \____|__  /|___|  / \___  >(____  /\____ |                    
                          \/      \/      \/      \/      \/    ";

        public static string VersionNumber => "Version: 0.D-1858-gbdaff5";

  
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }
    }
}