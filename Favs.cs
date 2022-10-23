using Extensions.Paths;
using SimpleUtilities.Serialization.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace ProrimorGUI
{
    public class LauncherItem
    {
        [Category("General")]
        [Description("Path to an icon to be displayed on the button. Leave empty to use the name instead.")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string IconPath { get; set; }

        [Category("General")]
        [Description("The name to be used if no icon is selected.")]
        public string Name { get; set; }

        [Category("Folder")]
        [Description("This can be a folder, file, search, or even a program to start.")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string NavigationString { get; set; }

        [Category("General")]
        [Description("The action to take upon clicking")]
        public NavigationAction ClickAction { get; set; } = NavigationAction.Navigate;

        [Browsable(false)]
        public int Order { get; set; }
        [Browsable(false)]
        public Guid ID { get; private set; }

        public LauncherItem() { ID = Guid.NewGuid(); }

        public LauncherItem(string name, NavigationAction action, string iconpath = null) : this()
        {
            ClickAction = action;
            Name = name;
            IconPath = iconpath;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is LauncherItem)) return false;

            LauncherItem l = obj as LauncherItem;

            return l.ID == ID;
        }
    }





    public class Favs : Saveable<Favs>
    {
        public List<LauncherItem> Items = new List<LauncherItem>();

        public Favs()
        {
            //for (int i = 0; i < 5; i++)
            //{
            //    Items.Add(new LauncherItem(i.ToString(), @"c:\windows\system32\"));
            //}
        }

        public override string GetSavePath()
        {
            return ENV.Settings.SettingsStorageLocation.CombineWith("LauncherItems.json");
        }
    }
}
