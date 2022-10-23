using BetterControls;
using Extensions.FontNS;
using Extensions.Imagery;
using Extensions.String.Testing;
using Extensions.WinForms.Sizing;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProrimorGUI
{
    public static class CGen
    {
        public static ToolStripMenuItem MenuItem(string txt, EventHandler click)
        {
            ToolStripMenuItem mi = new ToolStripMenuItem();
            mi.Text = txt;
            mi.Font = ENV.Settings.OpBTNFont;
            mi.TextAlign = ContentAlignment.MiddleLeft;
            mi.ApplyTheme();
            mi.BackColor = ENV.Settings.MyTheme.Back;
            mi.Click += click;

            return mi;
        }

        public static BetterButton OperationalButton(int size, string text, EventHandler click)
        {
            BetterButton b = new BetterButton(false);

            b.Square(size);
            b.Font = ENV.Settings.OpBTNFont;
            b.Text = text;
            b.ApplyTheme(true);
            b.Click += click;

            return b;
        }

        public static FavButton FavButton(LauncherItem li, MouseEventHandler Lclick, int w, int h, BetterContextMenuStrip cm)
        {
            FavButton b = new FavButton(li);
            b.SetDimsAbs(w, h);
            b.MouseClick += Lclick;
            b.Font = ENV.Settings.FileFolderFont;
            b.ApplyTheme(true);

            if (cm != null)
            {
                b.Menu = cm;
                b.Menu.Enabled = true;
            }

            return b;
        }

        public static void SetToolTip(Control c, string text)
        {
            ToolTip t = new ToolTip();
            t.AutoPopDelay = 5000;
            t.InitialDelay = 1000;
            t.ReshowDelay = 500;
            t.ShowAlways = true;
            t.SetToolTip(c, text);
        }
    }

    public class FavButton : BetterButton
    {
        public static Bitmap FolderIcon;

        public LauncherItem LI;

        public FavButton(LauncherItem li) : base(false)
        {
            if (FolderIcon == null) FolderIcon = Icons.GetIcon(SimpleUtilities.SystemImagery.SHSTOCKICONID.SIID_FOLDER, true).ToBitmap();

            LI = li;

            BackgroundImageLayout = ImageLayout.Center;
        }

        public void GenBackground()
        {
            BackgroundImage = ExtractOrGenBackground(LI.IconPath);

            if (BackgroundImage == null)
                Text = LI.Name;
            else
                BackgroundImage = BackgroundImage.Fit(Bounds, ENV.Settings.FavButtonPadding);
        }

        public Bitmap ExtractOrGenBackground(string extractionpath = null)
        {
            try
            {
                if (!extractionpath.IsEmpty())
                    return new Bitmap(extractionpath);
                else
                    return null;
            }
            catch (Exception)
            {
                try { return Icon.ExtractAssociatedIcon(extractionpath).ToBitmap(); }
                catch { return FolderIcon; }
            }

            return null; //ill just use the name when no icon is provided

            //gen icon
            int len = Width < Height ? Width : Height;
            Bitmap b = new Bitmap(len, len);

            using (Graphics g = Graphics.FromImage(b))
            {
                string s = LI.Name[0].ToString().ToUpperInvariant(); //first char
                Font = MaxFontSize(len, g, s, Font);
                g.DrawString(s, Font, new SolidBrush(ENV.Settings.MyTheme.Fore), new RectangleF(0, 0, Width, Height));
            }

            return b;
        }

        public static Font MaxFontSize(int maxlen, Graphics g, string s, Font f)
        {
            SizeF sf = new SizeF(0, 0);

            do
            {
                f = f.ChangeSize(1);
                sf = g.MeasureString(s, f);
            } while (sf.Width < maxlen && sf.Height < maxlen);

            if (sf.Width > maxlen || sf.Height > maxlen) f = f.ChangeSize(-1);

            return f;
        }
    }
}
