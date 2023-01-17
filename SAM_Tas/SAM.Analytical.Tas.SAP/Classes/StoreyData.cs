using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class StoreyData : ISAP
    {
        private List<Storey> storeys;

        public StoreyData()
        {

        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_STOREY_DATA");
            if(storeys != null)
            {
                foreach(Storey storey in storeys)
                {
                    List<string> strings = storey?.ToStrings();
                    if(strings == null)
                    {
                        continue;
                    }

                    result.AddRange(strings);
                }
            }
            result.Add("END_STOREY_DATA");

            return result;
        }

        public Storey FindStorey(string name)
        {
            return storeys?.Find(x => x.Name == name);
        }

        public bool Add(Storey storey)
        {
            if (storeys == null)
            {
                storeys = new List<Storey>();
            }

            return Modify.Add(storeys, storey);
        }

        public bool Remove(string name)
        {
            if(storeys == null)
            {
                return false;
            }

            int index = storeys.FindIndex(x => x.Name == name);
            if(index == -1)
            {
                return false;
            }

            storeys.RemoveAt(index);
            return true;
        }

        public List<Storey> Storeys
        {
            get
            {
                return storeys == null ? null : new List<Storey>(storeys);
            }
        }
    }
}
