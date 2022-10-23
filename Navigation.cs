using BetterControls;
using BrightIdeasSoftware;
using Extensions.FontNS;
using Extensions.Paths;
using Extensions.String.Testing;
using Extensions.Strings.AdvancedToString;
using Extensions.WinForms;
using Extensions.WinForms.Layout;
using Extensions.WinForms.Sizing;
using SimpleUtilities.History;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ProrimorGUI
{
    //not all of these will be handled by the navigator, but are instead added so you can do what you want with them. if you want to know what is handled,
    //simply call NavigateByAction with its optional string param (for example for a path). if it doesnt handle it, it will return false.
    //one example is AddFavorite. The navigator doesnt support favorites out of the box, but you may do with it what you want so that, for example,
    //you only need one enum property in a modelobject that can support things that you do, like a favorite bar.
    public enum NavigationAction
    {
        Drives, Navigate, AddFavorite, HomeFolder
    }

    public class NavigationEventArgs : EventArgs
    {
        public bool Succeeded;
        public FileFolderModel FFM;
        public Navigation NavInstance;

        public NavigationEventArgs(FileFolderModel ffm, bool s, Navigation i)
        {
            Succeeded = s;
            FFM = ffm;
            NavInstance = i;
        }
    }

    public class Navigation : BetterPanel
    {
        public ObjectListView OLV = new ObjectListView();
        public SimpleHistory<string> Hist = new SimpleHistory<string>();
        public Exception LastNavigationError = null;
        public List<FileFolderModel> CurrentChildren = new List<FileFolderModel>();
        
        public bool IsFiltered = false;

        //yes 2 ways of getting the instance is reduntant, but some people will be used to using sender, but its nice to have the typed Instance there for you(me)
        public delegate void NavigationFinishedHandler(object sender, NavigationEventArgs e);
        public event NavigationFinishedHandler OnNavigationFinished;

        public Navigation()
        {
            OLV.Zero();
            OLV.BorderStyle = BorderStyle.None;
            OLV.BackColor = ENV.Settings.MyTheme.Back;
            OLV.ForeColor = ENV.Settings.MyTheme.Fore;
            OLV.HeaderStyle = ENV.Settings.ShowHeader ? ColumnHeaderStyle.Clickable : ColumnHeaderStyle.None;
            OLV.HeaderUsesThemes = false;
            HeaderFormatStyle hfs = new HeaderFormatStyle();
            hfs.SetBackColor(ENV.Settings.MyTheme.Back);
            hfs.SetForeColor(ENV.Settings.MyTheme.Fore);
            OLV.HeaderFormatStyle = hfs;
            OLV.RowHeight = 20;
            OLV.EmptyListMsg = "OOPS! Looks like theres nothing here!";
            OLV.EmptyListMsgFont = ENV.Settings.OpBTNFont.Size(20);
            OLV.Font = ENV.Settings.FileFolderFont;
            OLV.FullRowSelect = true;
            OLV.OwnerDraw = true;
            OLV.UseCustomSelectionColors = true;
            OLV.SelectedBackColor = ENV.Settings.MyTheme.Accent;
            OLV.UnfocusedSelectedBackColor = ENV.Settings.MyTheme.Accent;

            OLV.HotTracking = false;
            OLV.UseHotItem = true;
            OLV.HotItemStyle = new HotItemStyle();
            OLV.HotItemStyle.BackColor = ENV.Settings.MyTheme.Accent;

            TextOverlay to = new TextOverlay();
            to.Alignment = ContentAlignment.TopCenter;
            to.BackColor = ENV.Settings.MyTheme.Accent;
            to.TextColor = ENV.Settings.MyTheme.Fore;
            to.BorderColor = ENV.Settings.MyTheme.Fore;
            to.Text = OLV.EmptyListMsg;
            OLV.EmptyListMsgOverlay = to;

            OLV.SmallImageList = new ImageList();
            OLV.SmallImageList.Images.Add("dir", Icons.GetIcon(SimpleUtilities.SystemImagery.SHSTOCKICONID.SIID_FOLDER, false));
            OLV.View = View.Details;

            OLVColumn olvc = new OLVColumn("Name", "Name");
            olvc.FillsFreeSpace = true;
            olvc.UseInitialLetterForGroup = false;
            olvc.GroupKeyGetter += new GroupKeyGetterDelegate(GroupKeyGetter);
            OLV.Columns.Add(olvc);

            Controls.Add(OLV);
            Resize += Navigation_Resize;
            olvc.ImageGetter = new ImageGetterDelegate(FileFolderImageGetter);
            olvc.GroupFormatter = new GroupFormatterDelegate(FileFolderGroupFormatter);
            OLV.DoubleClick += OLV_DoubleClick;
            OLV.CellRightClick += OLV_CellRightClick;
            //OLV.CellToolTipShowing += OLV_CellToolTipShowing;
        }

        private void Navigation_Resize(object sender, EventArgs e)
        {
            OLV.Fill();
        }

        #region Keys support

        public void SelectNextCell()
        {
            if (OLV.SelectedIndex < 0)
                OLV.SelectedIndex = 0;
            else if (OLV.SelectedIndex == OLV.Items.Count - 1)
                OLV.SelectedIndex = 0;
            else
                OLV.SelectedIndex += 1;
        }

        public void SelectPrevCell()
        {
            if (OLV.SelectedIndex <= 0)
                OLV.SelectedIndex = OLV.Items.Count - 1;
            else
                OLV.SelectedIndex -= 1;
        }

        #endregion

        #region Object List View

        //https://stackoverflow.com/questions/56744886/how-to-group-and-sort-objects-in-objectlistview
        private void FileFolderGroupFormatter(OLVGroup group, GroupingParameters parms)
        {
            string k = (string)group.Key;

            group.Header = k == "Folders" ? "Folders" : "Files - " + k[0].ToString().ToUpperInvariant();
            group.Id = k == "Folders" ? -1 : k[0];

            parms.GroupComparer = Comparer<OLVGroup>.Create((x, y) => (x.GroupId.CompareTo(y.GroupId)));
        }

        //return any obj as key, gets converted to a string for the group title
        private object GroupKeyGetter(object rowObject)
        {
            FileFolderModel ff = rowObject as FileFolderModel;

            if (ff.IsDirectory) return "Folders";
            return ff.Name[0].ToString().ToUpperInvariant();
        }

        private void OLV_CellRightClick(object sender, CellRightClickEventArgs e) //right click menu
        {
            //http://objectlistview.sourceforge.net/cs/recipes.html
            //#24

            e.MenuStrip = ContextMenuProvider.GetMenu(OLV.SelectedObjects);
        }

        private void OLV_CellToolTipShowing(object sender, ToolTipShowingEventArgs e) //replace with custom hovering preview
        {
            FileFolderModel ff = e.Model as FileFolderModel;

            e.Text = ff.Path;
        }

        private object FileFolderImageGetter(object rowObject)
        {
            FileFolderModel ff = rowObject as FileFolderModel;

            //ff.IsFolder.Popup(ff.Path);

            if (ff.IsDirectory) return "dir";

            return GetImageEXTKEY(ff.Path);
        }

        private void OLV_DoubleClick(object sender, EventArgs e) { NavigateToSelectedRow(); }

        #endregion

        #region Navigation

        public bool ShowDrives()
        {
            MsgBox.Show("Drives");
            return false;
        }

        public bool NavigateByAction(NavigationAction na, string option)
        {
            switch (na)
            {
                case NavigationAction.Drives:
                    return ShowDrives();
                case NavigationAction.Navigate:
                    return NavigateToPath(option);
                case NavigationAction.HomeFolder:
                    return false;
                default:
                    return false;
            }
                
        }

        public bool NavigateToSelectedRow()
        {
            if (OLV.SelectedItem == null) return false;

            FileFolderModel ff = OLV.SelectedItem.RowObject as FileFolderModel;

            if (OLV.SelectedItems.Count > 0)
                return NavigateToPath(ff.Path);

            LastNavigationError = new Exception("No row was selected to navigate to!");
            return false;
        }

        public bool NavigateToPath(string pth, bool addhistory = true)
        {
            return NavigateToPath(new FileFolderModel(pth), addhistory);
        }

        public bool NavigateToPath(FileFolderModel ffm, bool addtohistory = true)
        {
            bool result = false;

            if (ffm.Path.IsEmpty())
            {
                LastNavigationError = new ArgumentNullException("Navigation path is null or empty!");
            }
            else
            {
                if (ffm.IsDirectory) result = NavDir(ffm, addtohistory);
                else if (ffm.IsFile) result = NavFile(ffm);

                if (OnNavigationFinished != null) OnNavigationFinished(this, new NavigationEventArgs(ffm, result, this));
            }

            return result;
        }

        private bool NavFile(FileFolderModel ffm)
        {
            try
            {
                Process.Start(ffm.Path);
                return true;
            }
            catch (Exception e)
            {
                LastNavigationError = e;
                return false;
            }
        }

        private bool NavDir(FileFolderModel ffm, bool addtohist = true)
        {
            try
            {
                //doing this makes it so we can search/filter without constantly re-querying the ssd/hdd. just use .where on the list
                CurrentChildren = GetFileFolders(ffm);
                ENV.CurrentDir = ffm;
                if (addtohist) Hist.Add(ffm.Path);
                //OLV.SetObjects(CurrentChildren); no need to do this, filter does it
                ResetFilter();

                return true;
            }
            catch (Exception e)
            {
                LastNavigationError = e;
                return false;
            }
        }

        public bool NavigateNext()
        {
            if (!Hist.CanNext()) return false;
            return NavigateToPath(Hist.Next(), false);
        }

        public bool NavigateBack()
        {
            if (!Hist.CanBack()) return false;
            return NavigateToPath(Hist.Back(), false);
        }

        public List<FileFolderModel> GetFileFolders(FileFolderModel ffm)
        {
            if (!ffm.IsDirectory) throw new ArgumentOutOfRangeException("This folder doesnt exist: " + ffm.Path);

            List<FileFolderModel> children = new List<FileFolderModel>();

            foreach (var item in Directory.GetDirectories(ffm.Path))
            {
                children.Add(new FileFolderModel(item, true));
            }


            foreach (string item in Directory.GetFiles(ffm.Path))
            {
                string ext = GetImageEXTKEY(item);

                if (!OLV.SmallImageList.Images.ContainsKey(ext))
                    OLV.SmallImageList.Images.Add(ext, Icon.ExtractAssociatedIcon(item).ToBitmap());

                children.Add(new FileFolderModel(item, false));
            }

            return children;
        }

        //dont use the pattern to reset
        public void ResetFilter(string pattern = "") //maybe use regex in future?
        {
            OLV.SetObjects(CurrentChildren.Where(x => x.Name.LooslyContains(pattern)));
            IsFiltered = pattern != ""; //filtering by "" shows everything anyways.. so no need to consider the displayed objs filtered
        }

        #endregion

        #region Helpers

        string[] uniqueextensions = { "exe", "url", "lnk" };

        public string GetImageEXTKEY(string path)
        {
            string ext = path.GetExtension();



            if (uniqueextensions.Contains(ext)) ext = path;
            //ext.Popup(path);
            return ext;
        }

        #endregion


    }
}
