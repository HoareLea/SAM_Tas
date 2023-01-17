using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class BuildingElementData : ISAP
    {
        private List<BuildingElement> buildingElements;

        public BuildingElementData()
        {

        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_BUILDING_ELEMENT_DATA");
            if(buildingElements != null)
            {
                foreach(BuildingElement buildingElement in buildingElements)
                {
                    List<string> strings = buildingElement?.ToStrings();
                    if(strings == null)
                    {
                        continue;
                    }

                    result.AddRange(strings);
                }
            }
            result.Add("END_BUILDING_ELEMENT_DATA");

            return result;
        }

        public BuildingElement Add(Guid guid, BuildingElementType buildingElementType, bool zero)
        {
            if(guid == Guid.Empty || buildingElementType == BuildingElementType.Undefined)
            {
                return null;
            }

            if(buildingElements == null)
            {
                buildingElements = new List<BuildingElement>();
            }

            int index = buildingElements.FindIndex(x => x.Guid == guid);
            if(index == -1)
            {
                BuildingElement result = new BuildingElement(guid, buildingElementType, zero);
                buildingElements.Add(result);
                return result;
            }

            buildingElements[index] = new BuildingElement(guid, buildingElementType, zero);
            return buildingElements[index];
        }

        public bool Remove(Guid guid)
        {
            if(buildingElements == null)
            {
                return false;
            }

            int index = buildingElements.FindIndex(x => x.Guid == guid);
            if(index == -1)
            {
                return false;
            }

            buildingElements.RemoveAt(index);
            return true;
        }

        public List<BuildingElement> BuildingElements
        {
            get
            {
                return buildingElements == null ? null : new List<BuildingElement>(buildingElements);
            }
        }

    }
}
