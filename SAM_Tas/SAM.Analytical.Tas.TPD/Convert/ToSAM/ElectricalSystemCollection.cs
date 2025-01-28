using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemCollection ToSAM(this ElectricalGroup electricalGroup)
        {
            if (electricalGroup == null)
            {
                return null;
            }

            dynamic @dynamic = electricalGroup;

            ElectricalSystemCollection result = new ElectricalSystemCollection(dynamic.Name, electricalGroup.ElectricalGroupType.ToSAM());
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            List<FuelSource> fuelSources = Query.FuelSources(electricalGroup as PlantComponent);
            if(fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.ElectricalEnergySourceName, fuelSources[0]?.Name);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplayElectricalSystemCollection displayElectricalSystemCollection = Systems.Create.DisplayObject<DisplayElectricalSystemCollection>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displayElectricalSystemCollection != null)
            {
                ITransform2D transform2D = ((IPlantComponent)electricalGroup).Transform2D();
                if (transform2D != null)
                {
                    displayElectricalSystemCollection.Transform(transform2D);
                }

                result = displayElectricalSystemCollection;
            }

            return result;
        }
    }
}