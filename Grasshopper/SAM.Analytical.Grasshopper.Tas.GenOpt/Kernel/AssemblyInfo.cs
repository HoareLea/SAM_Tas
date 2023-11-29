using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SAM.Analytical.Grasshopper.Tas.GenOpt
{
    public class AssemblyInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "SAM";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "SAM Tas GenOpt Toolkit";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("5e5594d2-65e3-4aec-b9f2-105ffb1923ea");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Michal Dengusiak & Jakub Ziolkowski at Hoare Lea";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "Michal Dengusiak -> michaldengsiak@hoarelea.com and Jakub Ziolkowski -> jakubziolkowski@hoarelea.com";
            }
        }
    }
}
