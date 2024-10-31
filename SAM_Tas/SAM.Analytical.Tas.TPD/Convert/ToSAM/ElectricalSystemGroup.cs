﻿using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemGroup ToSAM(this ElectricalGroup electricalGroup, BoundingBox2D boundingBox2D = null)
        {
            if (electricalGroup == null)
            {
                return null;
            }

            tpdElectricalGroupType tpdElectricalGroupType = electricalGroup.ElectricalGroupType;

            dynamic @dynamic = electricalGroup;

            ElectricalSystemGroup result = new ElectricalSystemGroup(dynamic.Name, tpdElectricalGroupType.ToSAM());
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            if (boundingBox2D == null)
            {
                boundingBox2D = Query.BoundingBox2D((PlantGroup)electricalGroup);
            }

            DisplayElectricalSystemGroup displayElectricalSystemGroup = new DisplayElectricalSystemGroup(result, boundingBox2D);
            if(displayElectricalSystemGroup != null)
            {
                result = displayElectricalSystemGroup;
            }

            return result;
        }
    }
}

