using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DXCoil ToTPD(this DisplaySystemDXCoil displaySystemDXCoil, global::TPD.System system, DXCoil dXCoil = null)
        {
            if(displaySystemDXCoil == null || system == null)
            {
                return null;
            }

            DXCoil result = dXCoil;
            if(result == null)
            {
                result = system.AddDXCoil();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDXCoil.Name;
            @dynamic.Description = displaySystemDXCoil.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.CoolingSetpoint?.Update(displaySystemDXCoil.CoolingSetpoint, energyCentre);
            result.HeatingSetpoint?.Update(displaySystemDXCoil.HeatingSetpoint, energyCentre);
            result.MinimumOffcoil?.Update(displaySystemDXCoil.MinOffcoilTemperature, energyCentre);
            result.MaximumOffcoil?.Update(displaySystemDXCoil.MaxOffcoilTemperature, energyCentre);
            result.BypassFactor?.Update(displaySystemDXCoil.BypassFactor, energyCentre);
            result.CoolingDuty?.Update(displaySystemDXCoil.CoolingDuty, system);
            result.HeatingDuty?.Update(displaySystemDXCoil.HeatingDuty, system);

            Modify.SetSchedule((SystemComponent)result, displaySystemDXCoil.ScheduleName);

            //result.ExchLatType = tpdExchangerLatentType.tpdExchangerLatentHumRat;
            //result.Setpoint.Value = 14;
            //result.Flags = tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser;

            CollectionLink collectionLink = displaySystemDXCoil.GetValue<CollectionLink>(SystemDXColiParameter.RefrigerantCollection);
            if (collectionLink != null)
            {
                RefrigerantGroup refrigerantGroup = plantRoom?.RefrigerantGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                if (refrigerantGroup != null)
                {
                    @dynamic.SetRefrigerantGroup(refrigerantGroup);
                }
            }

            SystemComponent systemComponent = result as SystemComponent;

            if(dXCoil == null)
            {
                displaySystemDXCoil.SetLocation(systemComponent);
            }

            return result;
        }
    }
}
