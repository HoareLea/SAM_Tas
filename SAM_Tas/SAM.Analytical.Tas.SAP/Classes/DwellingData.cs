using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class DwellingData : ISAP
    {
        private List<Dwelling> dwellings;
        private List<ColdArea> coldAreas;

        public DwellingData()
        {

        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_DWELLING_DATA");
            if(dwellings != null)
            {
                foreach(Dwelling dwelling in dwellings)
                {
                    List<string> strings = dwelling?.ToStrings();
                    if(strings == null)
                    {
                        continue;
                    }

                    result.AddRange(strings);
                }
            }
            if (coldAreas != null)
            {
                foreach (ColdArea coldArea in coldAreas)
                {
                    List<string> strings = coldArea?.ToStrings();
                    if (strings == null)
                    {
                        continue;
                    }

                    result.AddRange(strings);
                }
            }
            result.Add("END_DWELLING_DATA");

            return result;
        }

        public Dwelling FindDwelling(string name)
        {
            return dwellings?.Find(x => x.Name == name);
        }

        public ColdArea FindColdArea(string name)
        {
            return coldAreas?.Find(x => x.Name == name);
        }

        public bool Add(Dwelling dwelling)
        {
            if(dwellings == null)
            {
                dwellings = new List<Dwelling>();
            }

            return Modify.Add(dwellings, dwelling);
        }

        public bool Add(ColdArea coldArea)
        {
            if (coldAreas == null)
            {
                coldAreas = new List<ColdArea>();
            }

            return Modify.Add(coldAreas, coldArea);
        }

        public bool RemoveColdArea(string name)
        {
            if(coldAreas == null)
            {
                return false;
            }

            int index = coldAreas.FindIndex(x => x.Name == name);
            if(index == -1)
            {
                return false;
            }

            coldAreas.RemoveAt(index);
            return true;
        }

        public bool RemoveDwelling(string name)
        {
            if(dwellings == null)
            {
                return false;
            }

            int index = dwellings.FindIndex(x => x.Name == name);
            if(index == -1)
            {
                return false;
            }

            dwellings.RemoveAt(index);
            return true;
        }

        public List<Dwelling> Dwellings
        {
            get
            {
                return dwellings == null ? null : new List<Dwelling>(dwellings);
            }
        }

        public List<ColdArea> ColdAreas
        {
            get
            {
                return coldAreas == null ? null : new List<ColdArea>(coldAreas);
            }
        }
    }
}
