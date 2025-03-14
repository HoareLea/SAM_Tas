namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static void ComponentTypes(HeatingSystem heatingSystem, CoolingSystem coolingSystem, out bool radiator, out bool fanCoil_Heating, out bool fanCoil_Cooling, out bool dXCoil_Heating, out bool dXCoil_Cooling, out bool chilledBeam_Heating, out bool chilledBeam_Cooling)
        {
            radiator = false;
            fanCoil_Heating = false;
            fanCoil_Cooling = false;
            dXCoil_Heating = false;
            dXCoil_Cooling = false;
            chilledBeam_Heating = false;
            chilledBeam_Cooling = false;

            if (heatingSystem == null && coolingSystem == null)
            {
                return;
            }

            if (heatingSystem != null)
            {
                if (heatingSystem.Name == "RAD" || heatingSystem.Name == "THR" || heatingSystem.Name == "UFH")
                {
                    radiator = true;
                }
                else if (heatingSystem.Name == "FCU")
                {
                    fanCoil_Heating = true;
                }
                else if (heatingSystem.Name == "RP" || heatingSystem.Name == "CHB")
                {
                    chilledBeam_Heating = true;
                }
                else if (heatingSystem.Name == "VRV")
                {
                    dXCoil_Heating = true;
                }
            }

            if (coolingSystem != null)
            {
                if (coolingSystem.Name == "RP" || coolingSystem.Name == "CHB" || coolingSystem.Name == "UFC")
                {
                    chilledBeam_Cooling = true;
                }
                else if (coolingSystem.Name == "TRC" || coolingSystem.Name == "FCU")
                {
                    fanCoil_Cooling = true;
                }
                else if (coolingSystem.Name == "VRV")
                {
                    dXCoil_Cooling = true;
                }
            }

        }
    }
}