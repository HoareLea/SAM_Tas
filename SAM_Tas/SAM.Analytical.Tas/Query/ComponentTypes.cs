namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static void ComponentTypes(HeatingSystem heatingSystem, CoolingSystem coolingSystem, out bool radiator, out bool fanCoil, out bool dXCoil, out bool chilledBeam)
        {
            radiator = false;
            fanCoil = false;
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
                    fanCoil = true;
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
                else if (heatingSystem.Name == "TRC" || heatingSystem.Name == "FCU")
                {
                    fanCoil = true;
                }
            }

        }
    }
}