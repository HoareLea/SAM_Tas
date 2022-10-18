using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateBuildingElements(this string path_TBD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateBuildingElements(sAMTBDDocument, analyticalModel);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static bool UpdateBuildingElements(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel)
        {
            if (sAMTBDDocument == null)
                return false;

            return UpdateBuildingElements(sAMTBDDocument.TBDDocument, analyticalModel);
        }

        public static bool UpdateBuildingElements(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
                return false;

            Building building = tBDDocument.Building;
            if (building == null)
                return false;

            UpdateConstructions(tBDDocument, analyticalModel);

            List<buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
                return false;

            List<TBD.Construction> constructions = building.Constructions();
            if (constructions == null || constructions.Count == 0)
                return false;

            foreach (buildingElement buildingElement in buildingElements)
            {
                string name = Query.Name(buildingElement);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                TBD.Construction construction = constructions.Find(x => name.Equals(x.name));
                if(construction == null)
                {
                    List<TBD.Construction> constructions_Temp = constructions.FindAll(x => name.EndsWith(x.name));
                    if(constructions_Temp != null && constructions_Temp.Count != 0)
                    {
                        constructions_Temp.Sort((x, y) => System.Math.Abs(x.name.Length - name.Length).CompareTo(System.Math.Abs(y.name.Length - name.Length)));
                        construction = constructions_Temp.First();
                    }
                }

                if(construction == null)
                {
                    List<string> values = name.Split(' ')?.ToList();
                    values?.RemoveAll(x => string.IsNullOrWhiteSpace(x));
                    if (values != null && values.Count != 0)
                    {
                        foreach (TBD.Construction construction_Temp in constructions)
                        {
                            List<string> values_Temp = construction_Temp?.name?.Split(' ')?.ToList();
                            values_Temp?.RemoveAll(x => string.IsNullOrWhiteSpace(x));
                            if (values_Temp == null || values_Temp.Count == 0)
                            {
                                continue;
                            }

                            int count = 0;
                            foreach (string value in values)
                            {
                                if (values_Temp.Contains(value))
                                {
                                    count++;
                                }
                            }

                            if (count == values_Temp.Count)
                            {
                                construction = construction_Temp;
                                break;
                            }
                        }
                    }

                }
                    
                if (construction == null)
                    continue;

                switch((TBD.BuildingElementType)buildingElement.BEType)
                {
                    case TBD.BuildingElementType.GLAZING:
                        buildingElement.colour = Core.Convert.ToUint(Analytical.Query.Color(ApertureType.Window, AperturePart.Pane));
                        break;

                    case TBD.BuildingElementType.FRAMEELEMENT:
                        buildingElement.colour = Core.Convert.ToUint(Analytical.Query.Color(ApertureType.Window, AperturePart.Frame));
                        break;
                }

                buildingElement.AssignConstruction(construction);
            }

            return true;
        }
    }
}