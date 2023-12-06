using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static int StartHour(this TPDDoc tPDDoc)
        { 
            if(tPDDoc?.EnergyCentre == null)
            {
                return -1;
            }

            return tPDDoc.EnergyCentre.GetStartHour();
        }
    }
}