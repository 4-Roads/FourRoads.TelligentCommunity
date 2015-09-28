using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FourRoads.TelligentCommunity.Emoticons.Interfaces
{
    public interface IEmoticonLogic
    {
        string UpdateMarkup(string renderedHtml,int smileyWidth , int smileyHeight);
        string GetFilestoreCssPath();
        void Reset();
    }
}
