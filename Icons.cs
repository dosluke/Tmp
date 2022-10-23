using System.Collections.Generic;
using System.Drawing;
using SimpleUtilities.SystemImagery;

namespace ProrimorGUI
{
    public static class Icons
    {
        public static Dictionary<SHSTOCKICONID, Icon> Cache = new Dictionary<SHSTOCKICONID, Icon>();
        public static Icon GetIcon(SHSTOCKICONID ID, bool large = true)
        {
            if (!Cache.ContainsKey(ID)) Cache.Add(ID, SystemImages.GetStockIcon(ID, large ? SystemImages.SHGSI_LARGEICON : SystemImages.SHGSI_SMALLICON));
            return Cache[ID];
        }
    }
}
