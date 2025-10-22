using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static global::TPD.HeatingCoil ToTPD(this DisplaySystemHeatingCoil displaySystemHeatingCoil, global::TPD.System system, global::TPD.HeatingCoil heatingCoil = null)
        {
            if(displaySystemHeatingCoil == null || system == null)
            {
                return null;
            }

            global::TPD.HeatingCoil result = heatingCoil;
            if(result == null)
            {
                result = system.AddHeatingCoil();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemHeatingCoil.Name;
            @dynamic.Description = displaySystemHeatingCoil.Description;

            PlantRoom plantRoom = system.GetPlantRoom();
            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemHeatingCoil.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemHeatingCoil.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemHeatingCoil.Duty, system);
            result.MaximumOffcoil?.Update(displaySystemHeatingCoil.MaximumOffcoil, energyCentre);

            result.SetpointControlBand = displaySystemHeatingCoil.ControlBand;

            //result.Setpoint.Value = 14;
            //result.Duty.Type = tpdSizedVariable.tpdSizedVariableSize;
            //result.Duty.SizeFraction = 1.0;
            //result.MaximumOffcoil.Value = 28;

            Modify.SetSchedule((SystemComponent)result, displaySystemHeatingCoil.ScheduleName);

            CollectionLink collectionLink = displaySystemHeatingCoil.GetValue<CollectionLink>(SystemHeatingColiParameter.HeatingCollection);
            if (collectionLink != null)
            {
                switch(collectionLink.CollectionType)
                {
                    case CollectionType.Heating:
                        HeatingGroup heatingGroup = system.GetPlantRoom()?.HeatingGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (heatingGroup != null)
                        {
                            @dynamic.SetHeatingGroup(heatingGroup);
                        }
                        break;

                    case CollectionType.Fuel:
                        FuelGroup fuelGroup = system.GetPlantRoom()?.FuelGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (fuelGroup != null)
                        {
                            @dynamic.SetFuelGroup(fuelGroup);
                        }
                        break;

                    case CollectionType.Electrical:
                        ElectricalGroup electricalGroup = system.GetPlantRoom()?.ElectricalGroups()?.Find(x => ((dynamic)x).Name == collectionLink.Name);
                        if (electricalGroup != null)
                        {
                            @dynamic.SetElectricalGroup1(electricalGroup);
                        }
                        break;
                }
            }

            //if (heatingGroup != null)
            //{
            //    @dynamic.SetHeatingGroup(heatingGroup);
            //}

            //if(designConditionLoad != null)
            //{
            //    @dynamic.Duty.AddDesignCondition(designConditionLoad);
            //}

            if(heatingCoil == null)
            {
                displaySystemHeatingCoil.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
