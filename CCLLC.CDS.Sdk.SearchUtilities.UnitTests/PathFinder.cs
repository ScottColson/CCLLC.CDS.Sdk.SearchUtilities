using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLaB.Xrm.Test;

namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest
{
    public class PathFinder : IPathFinder
    {
        string fileName;

        public PathFinder(string fileName)
        {
            this.fileName = fileName;
        }
        public string GetPath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            return $"{path}\\..\\..\\{fileName}";
        }
    }
}
