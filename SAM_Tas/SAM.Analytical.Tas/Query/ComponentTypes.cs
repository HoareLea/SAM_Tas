namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static void ComponentTypes(HeatingSystem heatingSystem, CoolingSystem coolingSystem, out bool radiator, out bool fanCoil_Heating, out bool fanCoil_Cooling, out bool dXCoil, out bool chilledBeam)
        {
            radiator = false;
            fanCoil_Heating = false;
            fanCoil_Cooling = false;
            dXCoil = false;
            chilledBeam = false;

            if(heatingSystem == null && coolingSystem == null)
            {
                return;
            }

            if(heatingSystem != null)
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
                    chilledBeam = true;
                }
            }

            if(coolingSystem != null)
            {
                if (coolingSystem.Name == "RP" || coolingSystem.Name == "CHB")
                {
                    chilledBeam = true;
                }
                else if (coolingSystem.Name == "TRC" || coolingSystem.Name == "FCU")
                {
                    fanCoil_Cooling = true;
                }
            }

        }
    }
}