using BetterControls;
using Extensions.Paths;
using Newtonsoft.Json;
using SimpleUtilities.Serialization.Persistence;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ProrimorGUI
{
    //Color.FromArgb(51, 162, 161)
    public class SettingsModel : Saveable<SettingsModel>
    {
        [Browsable(false)]
        public Theme MyTheme { get; set; } //init in constructor

        [Browsable(false)]
        public RCMenuEntry RightClickMenuItems { get; set; } = null;




        [JsonIgnore]
        [Browsable(false)]
        public string SettingsStorageLocation { get; set; } = SimpleUtilities.CommonLocationsNS.CommonLocations.AppData().CombineWith("Prorimor");

        [JsonIgnore]
        [Browsable(false)]
        public Theme DefaultTheme { get { return Theme.Custom(Color.Black, Color.White, Color.FromArgb(40, 120, 120)); } }




        public Font OpBTNFont { get; set; } = new Font("Calibri", 15, FontStyle.Bold);
        public Font FileFolderFont { get; set; } = new Font("Calibri", 10, FontStyle.Regular);

        public int BarHeight { get; set; } = 30; //title,navigation

        public Size WindowSize { get; set; } = new Size(1000, 1000);

        public bool SaveAdjustedWindowSize { get; set; } = true;

        public bool ShowHeader { get; set; } = false;

        public bool FastRedraw { get; set; } = false;

        public bool ConfirmFavDeletion { get; set; } = true;

        public int FavDragDelay { get; set; } = 100;

        public int FavButtonPadding { get; set; } = 10;
        public int FavButtonMinImgSize { get; set; } = 10;

        public bool KeepLastWindowSize = false;


        public SettingsModel() : base()
        {
            MyTheme = DefaultTheme;//may cause override loading custom theme
        } 

        public override string GetSavePath()
        {
            return SettingsStorageLocation.CombineWith("Settings.json");
        }
    }
}
