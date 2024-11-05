using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingTower ToSAM(this CoolingTower coolingTower)
        {
            if (coolingTower == null)
            {
                return null;
            }

            dynamic @dynamic = coolingTower;

            SystemCoolingTower result = new SystemCoolingTower(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCoolingTower displaySystemCoolingTower = Systems.Create.DisplayObject<DisplaySystemCoolingTower>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemCoolingTower != null)
            {
                ITransform2D transform2D = ((IPlantComponent)coolingTower).Transform2D();
                if (transform2D != null)
                {
                    displaySystemCoolingTower.Transform(transform2D);
                }

                result = displaySystemCoolingTower;
            }

            return result;
        }
    }
}
