using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class SAPData : ISAP
    {
        public string Version { get; set; } = "9.5.4";
        public DwellingData DwellingData { get; set; } = new DwellingData();
        public BuildingElementData BuildingElementData { get; set; } = new BuildingElementData();
        public StoreyData StoreyData { get; set; } = new StoreyData();
        public LivingAreaData LivingAreaData { get; set; } = new LivingAreaData();
        public OptionsData OptionsData { get; set; } = new OptionsData();

        public SAPData()
        {

        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_SAP_DATA");
            result.Add(string.Format("VERSION = {0}", Version));
            if(DwellingData != null)
            {
                result.AddRange(DwellingData.ToStrings());
            }
            if(BuildingElementData != null)
            {
                result.AddRange(BuildingElementData.ToStrings());
            }
            if(StoreyData != null)
            {
                result.AddRange(StoreyData.ToStrings());
            }
            if (LivingAreaData != null)
            {
                result.AddRange(LivingAreaData.ToStrings());
            }

            if (OptionsData != null)
            {
                result.AddRange(OptionsData.ToStrings());
            }
            result.Add("END_SAP_DATA");
            return result;
        }

        public List<Dwelling> Dwellings
        {
            get
            {
                return DwellingData.Dwellings;
            }
        }

        public Dwelling AddDewlling(string name, Guid guid)
        {
            Dwelling dwelling = DwellingData.FindDwelling(name);
            if(dwelling == null)
            {
                dwelling = new Dwelling(name);
                DwellingData.Add(dwelling);
            }

            dwelling.Add(guid);

            return dwelling;
        }

        public bool RemoveDwelling(string name)
        {
            return DwellingData.RemoveDwelling(name);
        }

        public List<ColdArea> ColdAreas
        {
            get
            {
                return DwellingData.ColdAreas;
            }
        }

        public ColdArea AddColdArea(string name, Guid guid)
        {
            ColdArea coldArea = DwellingData.FindColdArea(name);
            if (coldArea == null)
            {
                coldArea = new ColdArea(name);
                DwellingData.Add(coldArea);
            }

            coldArea.Add(guid);

            return coldArea;
        }

        public bool RemoveColdArea(string name)
        {
            return DwellingData.RemoveColdArea(name);
        }

        public List<BuildingElement> BuildingElements
        {
            get
            {
                return BuildingElementData.BuildingElements;
            }
        }

        public BuildingElement AddBuildingElement(Guid guid, BuildingElementType buildingElementType, bool zero)
        {
            return BuildingElementData.Add(guid, buildingElementType, zero);
        }

        public bool RemoveBuildingElement(Guid guid)
        {
            return BuildingElementData.Remove(guid);
        }

        public List<Storey> Storeys
        {
            get
            {
                return StoreyData.Storeys;
            }
        }

        public Storey AddStorey(string name, Guid guid)
        {
            Storey storey = StoreyData.FindStorey(name);
            if (storey == null)
            {
                storey = new Storey(name);
                StoreyData.Add(storey);
            }

            storey.Add(guid);

            return storey;
        }

        public bool RemoveStorey(string name)
        {
            return StoreyData.Remove(name);
        }

        public List<Guid> LivingAreas
        {
            get
            {
                return LivingAreaData?.Guids;
            }
        }

        public bool AddLivingArea(Guid guid)
        {
            return LivingAreaData.Add(guid);
        }

        public bool RemoveLivingArea(Guid guid)
        {
            return LivingAreaData.Remove(guid);
        }

        public bool SummariseBridges
        {
            get
            {
                return OptionsData.SummariseBridges;
            }

            set
            {
                OptionsData.SummariseBridges = value;
            }
        }

        public bool ExtraBridgeInfo
        {
            get
            {
                return OptionsData.ExtraBridgeInfo;
            }

            set
            {
                OptionsData.ExtraBridgeInfo = value;
            }
        }

        public bool WireframeImages
        {
            get
            {
                return OptionsData.WireframeImages;
            }

            set
            {
                OptionsData.WireframeImages = value;
            }
        }

        public bool PlanAssessorXml
        {
            get
            {
                return OptionsData.PlanAssessorXml;
            }

            set
            {
                OptionsData.PlanAssessorXml = value;
            }
        }

        public bool JPADesignerXml
        {
            get
            {
                return OptionsData.JPADesignerXml;
            }

            set
            {
                OptionsData.JPADesignerXml = value;
            }
        }

        public bool Reexport
        {
            get
            {
                return OptionsData.Reexport;
            }

            set
            {
                OptionsData.Reexport = value;
            }
        }

    }
}
