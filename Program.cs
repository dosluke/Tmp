using BetterControls;
using Extensions.WinForms.Messaging;
using SimpleUtilities.Serialization.Persistence;
using System;
using System.Windows.Forms;

namespace ProrimorGUI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            //settings is a singleton and can be acccessed statically <- not true rn
            //and automatically loads from disk
            ENV.Settings = new SettingsModel().LoadOrDFLT();
            ENV.Settings.MyTheme.Apply();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new ProrimorMain());
            }
            catch (Exception e)
            {
                e.PopupBlocking();
            }
        }
    }

}
