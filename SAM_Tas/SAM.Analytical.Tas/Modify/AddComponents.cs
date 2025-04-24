using TPD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void AddComponents(this SystemZone systemZone, EnergyCentre energyCentre, HeatingSystem heatingSystem, CoolingSystem coolingSystem)
        {
            if (systemZone == null || energyCentre == null)
            {
                return;
            }

            PlantRoom plantRoom = energyCentre?.PlantRoom("Main PlantRoom");
            if (plantRoom == null)
            {
                return;
            }
            dynamic plantSchedule_System = energyCentre.PlantSchedule("System Schedule");

            dynamic electricalGroup_FanCoilUnits = plantRoom.ElectricalGroup("Electrical Group - FanCoil Units");
            dynamic electricalGroup_DXCoilUnits = plantRoom.ElectricalGroup("Electrical Group - DXCoil Units");

            dynamic heatingGroup = plantRoom.HeatingGroup("Heating Circuit Group");

            dynamic coolingGroup = plantRoom.CoolingGroup("Cooling Circuit Group");

            RefrigerantGroup refrigerantGroup = plantRoom.RefrigerantGroup("DXCoil Units Refrigerant Group");

            Query.ComponentTypes(heatingSystem, coolingSystem, out bool radiator, out bool fanCoil_Heating, out bool fanCoil_Cooling, out bool dXCoil_Heating, out bool dXCoil_Cooling, out bool chilledBeam_Heating, out bool chilledBeam_Cooling);

            if (radiator)  //TODO: 2023-09-25 allow other Zone component as Under Floor Heating Floor etc...read from Zone Internal Condition Heating Emitter Name
            {
                dynamic radiator_Group = systemZone.AddRadiator();
                radiator_Group.Name = heatingSystem.Name;
                radiator_Group.SetSchedule(plantSchedule_System);
                radiator_Group.Description = heatingSystem.Type?.Description;
                radiator_Group.Duty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                radiator_Group.Duty.AddDesignCondition(energyCentre.GetDesignCondition(1));
                radiator_Group.Duty.SizeFraction = 1.25;//per AHRAE
                radiator_Group.SetHeatingGroup(heatingGroup);
            }

            if (chilledBeam_Heating || chilledBeam_Cooling)
            {
                dynamic chilledBeam_Group = systemZone.AddChilledBeam();
                chilledBeam_Group.Flags = chilledBeam_Heating ? 1 : 0;

                chilledBeam_Group.SetSchedule(plantSchedule_System);

                if (chilledBeam_Cooling)
                {
                    chilledBeam_Group.Name = coolingSystem.Name;
                    chilledBeam_Group.Description = coolingSystem.Type?.Description;

                    chilledBeam_Group.SetCoolingGroup(coolingGroup);
                    chilledBeam_Group.CoolingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    chilledBeam_Group.CoolingDuty.SizeFraction = 1.15;//per AHRAE
                    chilledBeam_Group.CoolingDuty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                }

                if (chilledBeam_Heating)
                {
                    chilledBeam_Group.Name = heatingSystem.Name;
                    chilledBeam_Group.Description = heatingSystem.Type?.Description;

                    chilledBeam_Group.SetHeatingGroup(heatingGroup);
                    chilledBeam_Group.HeatingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    chilledBeam_Group.HeatingDuty.SizeFraction = 1.25;//per AHRAE
                    chilledBeam_Group.HeatingDuty.AddDesignCondition(energyCentre.GetDesignCondition(1));
                }
            }

            if (fanCoil_Heating || fanCoil_Cooling)
            {
                dynamic fanCoilUnit_Group = systemZone.AddFanCoilUnit();
                fanCoilUnit_Group.SetSchedule(plantSchedule_System);
                fanCoilUnit_Group.Name = "FanCoil Unit";
                fanCoilUnit_Group.Description = "FCU";
                fanCoilUnit_Group.SetElectricalGroup1(electricalGroup_FanCoilUnits);
                fanCoilUnit_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateSized;

                fanCoilUnit_Group.OverallEfficiency.Value = 0.9;
                fanCoilUnit_Group.HeatGainFactor = 0.9;
                fanCoilUnit_Group.Pressure = 150;

                fanCoilUnit_Group.DesignFlowRate.SizeFraction = 1.15;//per AHRAE
                for (int i = 1; i <= energyCentre.GetDesignConditionCount(); i++)
                {
                    fanCoilUnit_Group.DesignFlowRate.AddDesignCondition(energyCentre.GetDesignCondition(2));
                }

                if (fanCoil_Cooling)
                {
                    fanCoilUnit_Group.SetCoolingGroup(coolingGroup);
                    fanCoilUnit_Group.SetSchedule(plantSchedule_System);
                    fanCoilUnit_Group.CoolingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    fanCoilUnit_Group.CoolingDuty.SizeFraction = 1.15;//per AHRAE
                    fanCoilUnit_Group.CoolingDuty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                }
                else
                {
                    fanCoilUnit_Group.CoolingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableValue;
                    fanCoilUnit_Group.CoolingDuty.Value = 0;
                }

                if (fanCoil_Heating)
                {
                    fanCoilUnit_Group.SetHeatingGroup(heatingGroup);
                    fanCoilUnit_Group.HeatingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    fanCoilUnit_Group.HeatingDuty.SizeFraction = 1.15;//per AHRAE
                    fanCoilUnit_Group.HeatingDuty.AddDesignCondition(energyCentre.GetDesignCondition(1));
                }
                else
                {
                    fanCoilUnit_Group.HeatingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableValue;
                    fanCoilUnit_Group.HeatingDuty.Value = 0;
                }
            }

            if (dXCoil_Heating || dXCoil_Cooling)
            {
                dynamic dXCoilUnit_Group = systemZone.AddDXCoilUnit();
                dXCoilUnit_Group.SetSchedule(plantSchedule_System);
                dXCoilUnit_Group.Name = "DXCoil Unit";
                dXCoilUnit_Group.Description = "VRV";
                dXCoilUnit_Group.SetElectricalGroup1(electricalGroup_DXCoilUnits);
                dXCoilUnit_Group.DesignFlowType = TPD.tpdFlowRateType.tpdFlowRateSized;

                dXCoilUnit_Group.OverallEfficiency.Value = 0.9;
                dXCoilUnit_Group.HeatGainFactor = 0.9;
                dXCoilUnit_Group.Pressure = 150;
                //dXCoilUnit_Group.Refrigerant = refrigerantGroup;
                dXCoilUnit_Group.SetRefrigerantGroup(refrigerantGroup);

                dXCoilUnit_Group.DesignFlowRate.SizeFraction = 1.15;//per AHRAE
                dXCoilUnit_Group.DesignFlowRate.AddDesignCondition(energyCentre.GetDesignCondition(1));
                dXCoilUnit_Group.DesignFlowRate.AddDesignCondition(energyCentre.GetDesignCondition(2));

                if (dXCoil_Cooling)
                {
                    dXCoilUnit_Group.SetCoolingGroup(coolingGroup);
                    dXCoilUnit_Group.SetSchedule(plantSchedule_System);
                    dXCoilUnit_Group.CoolingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    dXCoilUnit_Group.CoolingDuty.SizeFraction = 1.15;//per AHRAE
                    dXCoilUnit_Group.CoolingDuty.AddDesignCondition(energyCentre.GetDesignCondition(2));
                }
                else
                {
                    dXCoilUnit_Group.CoolingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableValue;
                    dXCoilUnit_Group.CoolingDuty.Value = 0;
                }

                if (dXCoil_Heating)
                {
                    dXCoilUnit_Group.SetHeatingGroup(heatingGroup);
                    dXCoilUnit_Group.HeatingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableSize;
                    dXCoilUnit_Group.HeatingDuty.SizeFraction = 1.25;//per AHRAE
                    dXCoilUnit_Group.HeatingDuty.AddDesignCondition(energyCentre.GetDesignCondition(1));
                }
                else
                {
                    dXCoilUnit_Group.HeatingDuty.Type = TPD.tpdSizedVariable.tpdSizedVariableValue;
                    dXCoilUnit_Group.HeatingDuty.Value = 0;
                }
            }
        }
    }
}

