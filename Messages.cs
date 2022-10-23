using Extensions.WinForms.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProrimorGUI
{
    public static class Messages
    {
        internal static void BadPath(string p)
        {
            (p ?? "Path is empty.").PopupBlocking("This is not a valid path.");
        }
    }
}
