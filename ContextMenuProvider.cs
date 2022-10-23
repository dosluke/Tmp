using Extensions.WinForms.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Extensions.Enums;
using System.Drawing;
using BetterControls;
using SimpleUtilities.Serialization.Persistence.Internals;
using System.ComponentModel;
using BetterControls.Dialogs;
using System.Collections;
using Extensions.WinForms.General;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Extensions.String.Manipulation;
using System.CodeDom.Compiler;
using System.IO;
using Extensions.Paths;

namespace ProrimorGUI
{
    [Flags]
    public enum SelectionType
    {
        None = 1,
        File = 2,
        Folder = 4,
        Drive = 8,
        Always = 16
    }

    public class SelectedInfo
    {
        public SelectionType Type = SelectionType.None;
        public List<FileFolderModel> Paths = new List<FileFolderModel>();
    }

    public class RCMenuEntry
    { //an entry can have a click action, a hover menu, or both
        [Editor(typeof(Utils.FlagEnumUIEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public SelectionType Type { get; set; } = SelectionType.None;
        public string Text { get; set; } = "Default Text";
        public string Command { get; set; } = "none"; //use ps: or cmd:rrrrrrrrrrrrrrrrrr or sys: (run box) or no : for cmds like [open] //maybe add registry reading stuff in? proc: for starting a proc with args OR menu:name or cs:code
        public Icon Icon { get; set; } = null; //glyph
        public List<RCMenuEntry> SubItems { get; set; } = new List<RCMenuEntry>();

        [NonSerialized]
        public BetterContextMenuStrip Menu = null;

        public RCMenuEntry() { }

        public static RCMenuEntry Default()
        {
            //foreach (SelectionType item in Enum.GetValues(typeof(SelectionType)))
            RCMenuEntry result = new RCMenuEntry();

            result.Type = SelectionType.None;
            result.Text = "Edit the subitems collection to add, remove, or edit items.";
            result.Command = "none";

            #region new: folder, text, bmp, batch, ps1


            RCMenuEntry newmenu = new RCMenuEntry()
            {
                Text = "New",
                Command = "none",
                Type = SelectionType.None
            };

            RCMenuEntry newdir = new RCMenuEntry()
            {
                Text = "Folder",
                Command = "new-dir",
                Type = SelectionType.None
            };

            RCMenuEntry ntxt = new RCMenuEntry()
            {
                Text = "Text File",
                Command = "new-txt",
                Type = SelectionType.None
            };

            RCMenuEntry nbmp = new RCMenuEntry()
            {
                Text = "Bitmap File",
                Command = "new-bmp",
                Type = SelectionType.None
            };

            RCMenuEntry nbatch = new RCMenuEntry()
            {
                Text = "Batch Script File",
                Command = "new-bat",
                Type = SelectionType.None
            };

            RCMenuEntry nps1 = new RCMenuEntry()
            {
                Text = "Powershell Script File",
                Command = "new-txt",
                Type = SelectionType.None
            };

            newmenu.SubItems.AddAll(newdir, ntxt, nbmp, nbatch, nps1);

            #endregion

            #region file manage: copy path, take ownership, props

            RCMenuEntry fmanmenu = new RCMenuEntry()
            {
                Text = "File Management",
                Command = "none",
                Type = SelectionType.Always
            };

            RCMenuEntry takeown = new RCMenuEntry()
            {
                Text = "Take Ownership",
                Command = "takeown",
                Type = SelectionType.Always
            };

            RCMenuEntry copypth = new RCMenuEntry()
            {
                Text = "Copy Path",
                Command = "copypth",
                Type = SelectionType.Always
            };

            RCMenuEntry props = new RCMenuEntry()
            {
                Text = "Properties",
                Command = "props",
                Type = SelectionType.Always
            };

            fmanmenu.SubItems.AddAll(copypth, takeown, props);

            #endregion

            //open in terminal

            result.SubItems.AddAll(newmenu, fmanmenu);
            return result;
        }
    }

    internal class ContextMenuProvider
    {
        private static BetterContextMenuStrip MyMainMenu = null;
        //private static RCMenuEntry Config = null;

        public static ContextMenuStrip GetMenu(IList selectedItems)
        {
            SelectedInfo si = BuildSI(selectedItems);

            //if (Config is null)
            //    Config = RCMenuEntry.Default();

            if (MyMainMenu is null)
                ReBuildMenu();

            SetVisibility(si);

            return MyMainMenu;
        }

        public static SelectedInfo BuildSI(IList selectedItems)
        {
            SelectedInfo si = new SelectedInfo();

            foreach (var item in selectedItems)
            {
                FileFolderModel ffs = item as FileFolderModel;

                if (ffs == null) continue;

                si.Paths.Add(ffs);
                //putting isfile second should cut down on disk reads im assuming
                if (!si.Type.HasFlag(SelectionType.File) && ffs.IsFile) si.Type = si.Type | SelectionType.File;
                if (!si.Type.HasFlag(SelectionType.Folder) && ffs.IsDirectory) si.Type = si.Type | SelectionType.Folder;
                if (!si.Type.HasFlag(SelectionType.Drive) && ffs.IsRoot) si.Type = si.Type | SelectionType.Drive;
            }

            if (selectedItems.Count > 0)
                si.Type = si.Type & ~SelectionType.None;

            return si;
        }

        public static void ReBuildMenu()
        {
            MyMainMenu = new BetterContextMenuStrip();

            MyMainMenu.Renderer = new BetterToolStripMenuRenderer();

            GenMenu(ENV.Settings.RightClickMenuItems, MyMainMenu);
        }

        private static void GenMenu(RCMenuEntry config, BetterContextMenuStrip menu)
        {
            foreach (var item in config.SubItems)
            {
                menu.Items.Add(GenItem(item));
            }
        }

        private static ToolStripMenuItem GenItem(RCMenuEntry entry)
        {
            //("depth: " + depth).PopupBlocking();
            ToolStripMenuItem ti = new ToolStripMenuItem();
            ti.Click += MenuClick;
            //ti.MouseHover += Ti_MouseHover;
            ti.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            ti.TextImageRelation = TextImageRelation.ImageBeforeText;
            ti.Text = entry.Text;
            ti.Tag = entry;

            if (entry.SubItems != null)
            {
                //entry.SubItems.Count.PopupBlocking();
                foreach (var sub in entry.SubItems)
                {
                    ti.DropDownItems.Add(GenItem(sub));
                }
            }
            //"returning".PopupBlocking();
            return ti;
        }

        private static void SetVisibility(SelectedInfo selectedinfo)
        {
            for (int i = 0; i < MyMainMenu.Items.Count; i++)
            {
                RCMenuEntry me = MyMainMenu.Items[i].Tag as RCMenuEntry;

                if (me.Type.HasFlag(SelectionType.Always))
                {
                    MyMainMenu.Items[i].Visible = true;
                    continue;
                }

                MyMainMenu.Items[i].Visible = false;

                foreach (SelectionType sit in Enum.GetValues(typeof(SelectionType)))
                {
                    if (selectedinfo.Type.HasFlag(sit)
                        && me.Type.HasFlag(sit))
                        MyMainMenu.Items[i].Visible = true;
                }
            }
        }

        private static void MenuClick(object sender, EventArgs e)
        {
            ToolStripMenuItem ti = sender as ToolStripMenuItem;
            RCMenuEntry item = GetEntry(sender);

            string cmd = item.Command;
            string args = null;

            if (cmd.Contains("-"))
            {
                cmd = cmd.SubstringBeforeFirst("-");
                args = item.Command.SubstringAfterFirst("-");
            }

            switch (cmd)
            {
                case "none":
                    break;
                case "new":
                    Commands.NewFF(args);
                    break;
                default:
                    Commands.Unkown(cmd);
                    break;
            }
        }

        public static void EditRightClick(object sender, EventArgs e)
        {
            ModelEditor<RCMenuEntry> me = new ModelEditor<RCMenuEntry>(ENV.Settings.RightClickMenuItems);

            if (me.CShow()) //if true (saved), save, else reverse
            {
                try
                {
                    ReBuildMenu(); //do this first in order to ensure menu works
                }
                catch
                {
                    ENV.Settings.RightClickMenuItems = me.Model;
                    MsgBox.Show("Something went wrong. The menu has not been changed.");
                }
                ENV.Settings.Save();
            }
            else
            {
                ENV.Settings.RightClickMenuItems = me.Model;
            }
        }

        //private static void Ti_MouseHover(object sender, EventArgs e)
        //{
        //    return;
        //    ToolStripMenuItem tsitem = sender as ToolStripMenuItem;
        //    RCMenuEntry rcentry = tsitem.Tag as RCMenuEntry;

        //    if (tsitem.HasDropDownItems)
        //        tsitem.ShowDropDown();
        //}

        private static RCMenuEntry GetEntry(object ob)
        {
            return (ob as ToolStripMenuItem).Tag as RCMenuEntry;
        }
    }

    public static class Commands
    {
        internal static void NewFF(string args)
        {
            string newname = InputBox.Show("Name:");
            string newpth = ENV.CurrentDir.Path.CombineWith(newname);

            if (newname is null) return;

            if (args == "dir")
                Directory.CreateDirectory(newpth);
            else
                File.Create(newpth + "." + args);
        }

        internal static void Unkown(string cmd)
        {
            string msg = "UNKOWN COMMAND: " + cmd;
            msg.Popup();
        }
    }
}
