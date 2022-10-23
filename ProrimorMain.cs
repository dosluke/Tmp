using BetterControls;
using Extensions.FontNS;
using Extensions.String.Manipulation;
using Extensions.WinForms.General;
using Extensions.WinForms.Layout;
using Extensions.WinForms.Sizing;
using Extensions.WinForms.KeysConvert;
using ProrimorGUI.Controls;
using System;
using System.Windows.Forms;
using Extensions.Strings.AdvancedToString;
using Extensions.Characters;
using Extensions.Paths;

namespace ProrimorGUI
{
    public partial class ProrimorMain : BetterForm
    {
        public NavigationBar NavBAR;
        public LauncherPanel LauncherPNL;
        public Navigation NavigationPNL;

        public ProrimorMain(string path = null)
        {
            LauncherPNL = new LauncherPanel();
            NavBAR = new NavigationBar();
            NavigationPNL = new Navigation();

            DoubleBuffered = true;

            Size = ENV.Settings.WindowSize;
            Size = ENV.Settings.WindowSize;

            Text = "Prorimor";

            Controls.AddAll(NavBAR, NavigationPNL, LauncherPNL);


            TitleBAR.ApplyHoverColor(ENV.Settings.MyTheme.Accent);
            TitleBAR.ApplyFont(ENV.Settings.OpBTNFont);

            AddOption("Edit Settings", EditSettings);
            AddOption("Edit Theme", EditTheme);
            AddOption("Edit Context Menu", ContextMenuProvider.EditRightClick);
            AddOption("Reset Menu", ResetMenu);

            ResizeBegin += ProrimorMain_ResizeBegin;
            SizeChanged += ProrimorMain_SizeChanged;
            ResizeEnd += ProrimorMain_ResizeEnd;

            this.Center();

            AutoLayout(true);

            LauncherPNL.OnFavClick += LauncherPNL_OnFavClick;
            NavBAR.Forward.Click += Forward_Click;
            NavBAR.Back.Click += Back_Click;
            NavBAR.Up.Click += Up_Click;
            NavigationPNL.OnNavigationFinished += NavigationPNL_OnNavigationFinished;
            NavigationPNL.NavigateToPath(path ?? "c:\\");
            NavigationPNL.OLV.Select();
        }

        private void ResetMenu(object sender, EventArgs e)
        {
            ENV.Settings.RightClickMenuItems = RCMenuEntry.Default();
            ContextMenuProvider.ReBuildMenu();
            ENV.Settings.Save();
        }

        private void EditTheme(object sender, EventArgs e)
        {
            ModelEditor<Theme> me = new ModelEditor<Theme>(ENV.Settings.MyTheme);

            if (me.CShow()) //if true (saved), save, else reverse
            {
                ENV.Settings.Save();
            }
            else
            {
                ENV.Settings.MyTheme = me.Model;
            }
        }

        private void NavigationPNL_OnNavigationFinished(object sender, NavigationEventArgs e)
        {
            if (e.Succeeded && e.FFM.IsDirectory)
            {
                NavBAR.Address.Text = e.FFM.Path;

                NavBAR.AddBackSlash();
            }
        }

        public void Up_Click(object sender, EventArgs e)
        {
            if (NavigationPNL.IsFiltered)
            {
                NavigationPNL.ResetFilter();
                NavBAR.Address.Text = ENV.CurrentDir.Path;
                NavBAR.AddBackSlash();
            }
            else if (ENV.CurrentDir.IsRoot)
                NavigationPNL.NavigateByAction(NavigationAction.Drives, null);
            else
                NavigationPNL.NavigateToPath(ENV.CurrentDir.Path.GetParentPath());
        }

        private void Forward_Click(object sender, EventArgs e) { NavigationPNL.NavigateNext(); }

        private void Back_Click(object sender, EventArgs e) { NavigationPNL.NavigateBack(); }

        private void LauncherPNL_OnFavClick(object sender, LauncherItem li)
        {
            NavigationPNL.NavigateByAction(li.ClickAction, li.NavigationString);
            NavigationPNL.OLV.Select();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return HandleCommonKeyPress(keyData) ||  base.ProcessCmdKey(ref msg, keyData);
        }

        private bool HandleCommonKeyPress(Keys keydata) //enter up down+tab isdigit escape
        {
            if (keydata.HasFlag(Keys.Control) || keydata.HasFlag(Keys.Alt))
            {
                if (keydata.HasFlag(Keys.Control) && keydata.HasFlag(Keys.D))
                {
                    LauncherItem li;

                    if (NavigationPNL.OLV.SelectedIndex >= 0)
                    {
                        FileFolderModel ffm = NavigationPNL.OLV.SelectedItem.RowObject as FileFolderModel;

                        li = new LauncherItem()
                        {
                            NavigationString = ffm.Path,
                            Name = ffm.Path.GetPathName(),
                            ClickAction = NavigationAction.Navigate,
                            IconPath = ffm.Path
                        };
                    }
                    else
                    {
                        li = new LauncherItem()
                        {
                            NavigationString = ENV.CurrentDir.Path,
                            Name = ENV.CurrentDir.Path.GetPathName(),
                            ClickAction = NavigationAction.Navigate
                        };
                    }

                    LauncherPNL.AddFav(li);
                    return true;
                }
                else return false; //dont want to handle ctrl c etc
            }

            switch (keydata)
            {
                //if selected nav, else if not selected, try naving to typed, if that fails, select first item and nav to that, assuming youve typed a filter
                case Keys.Enter:
                    if (NavigationPNL.OLV.SelectedIndex >= 0) //if you select, then type, this will still click instead of navpath
                        NavigationPNL.NavigateToSelectedRow();
                    else if (!NavigationPNL.NavigateToPath(NavBAR.Address.Text))
                    {
                        NavigationPNL.SelectNextCell();
                        NavigationPNL.NavigateToSelectedRow();
                    }
                    NavigationPNL.OLV.Focus();
                    return true;
                case Keys.Escape:
                    Up_Click(this, null);
                    return true;
                case Keys.Up:
                    NavigationPNL.SelectPrevCell();
                    return true;
                case Keys.Down:
                case Keys.Tab: //same for now//using this to enable tab intercepting on a single line TB
                    NavigationPNL.SelectNextCell();
                    return true;
                case Keys.Insert: //newfilefolder
                    return false;
                case Keys.Delete: //delfilefolder
                    return false;
                default:
                    break;
            }

            char ch;

            if (keydata.TryToChar(out ch))
            {
                if (ch.IsPrintable() || keydata == Keys.Back)//normal pressable letters and symbols
                {
                    NavBAR.AppendAndFocus(ch);
                    NavigationPNL.ResetFilter(NavBAR.Address.Text.SubstringAfterLast("\\"));
                    return true;
                }
            }

            return false;
        }

        public void EditSettings(object sender, EventArgs e)
        {
            ModelEditor<SettingsModel> me = new ModelEditor<SettingsModel>(ENV.Settings);
            me.PG.Font = ENV.Settings.OpBTNFont.Size(12);

            if (me.CShow()) //if true (saved)
            {
                ENV.Settings.Save();
            }
            else //reset model
            {
                ENV.Settings = me.Model;
            }
        }

        private void ProrimorMain_ResizeBegin(object sender, EventArgs e)
        {
            LauncherPNL.SuspendLayout();
        }

        private void ProrimorMain_ResizeEnd(object sender, EventArgs e)
        {
            //if (ENV.Settings.FastRedraw)
            AutoLayout(true);
            LauncherPNL.ResumeLayout(true);
        }

        private void ProrimorMain_SizeChanged(object sender, EventArgs e)
        {
            if (!ENV.Settings.FastRedraw) AutoLayout(false);
        }

        public void AutoLayout(bool launcher)
        {
            NavBAR.FillX().AlignBelow(TitleBAR);

            LauncherPNL.AlignBelow(NavBAR).SetDimsAbs(NavBAR.Up.Right, Height - NavBAR.Bottom);

            NavigationPNL.AlignBelow(NavBAR).AlignRightOf(LauncherPNL)
                .SetDimsAbs(Width - LauncherPNL.Right + SystemInformation.VerticalScrollBarWidth, Height - NavBAR.Bottom);

            if (launcher) LauncherPNL.ResetItems();
        }

    }
}
