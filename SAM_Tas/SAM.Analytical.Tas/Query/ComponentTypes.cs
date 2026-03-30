// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static void ComponentTypes(IEnumerable<HeatingSystem> heatingSystems, IEnumerable<CoolingSystem> coolingSystems, out bool radiator, out bool fanCoil_Heating, out bool fanCoil_Cooling, out bool dXCoil_Heating, out bool dXCoil_Cooling, out bool chilledBeam_Heating, out bool chilledBeam_Cooling)
        {
            radiator = false;
            fanCoil_Heating = false;
            fanCoil_Cooling = false;
            dXCoil_Heating = false;
            dXCoil_Cooling = false;
            chilledBeam_Heating = false;
            chilledBeam_Cooling = false;

            if (heatingSystems == null && coolingSystems == null)
            {
                return;
            }

            if(heatingSystems is not null)
            {
                foreach (HeatingSystem heatingSystem in heatingSystems)
                {
                    if (heatingSystem != null)
                    {
                        if (heatingSystem.Name == "RAD" || heatingSystem.Name == "TRH" || heatingSystem.Name == "UFH")
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
                }
            }

            if(coolingSystems is not null)
            {
                foreach (CoolingSystem coolingSystem in coolingSystems)
                {
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
    }
}