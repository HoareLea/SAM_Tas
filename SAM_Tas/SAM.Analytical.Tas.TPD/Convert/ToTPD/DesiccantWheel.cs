using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DesiccantWheel ToTPD(this DisplaySystemDesiccantWheel displaySystemDesiccantWheel, global::TPD.System system)
        {
            if(displaySystemDesiccantWheel == null || system == null)
            {
                return null;
            }

            DesiccantWheel result = system.AddDesiccantWheel();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDesiccantWheel.Name;
            @dynamic.Description = displaySystemDesiccantWheel.Description;

            result.SensibleEfficiency?.Update(displaySystemDesiccantWheel.SensibleEfficiency);
            result.Reactivation?.Update(displaySystemDesiccantWheel.Reactivation);
            result.MinimumRH?.Update(displaySystemDesiccantWheel.MinimumRH);
            result.MaximumRH?.Update(displaySystemDesiccantWheel.MaximumRH);
            result.SensibleHEEfficiency?.Update(displaySystemDesiccantWheel.SensibleHEEfficiency);
            result.HESetpointMethod = displaySystemDesiccantWheel.HESetpointMethod.ToTPD();
            result.HESetpoint?.Update(displaySystemDesiccantWheel.HESetpoint);
            result.ElectricalLoad?.Update(displaySystemDesiccantWheel.ElectricalLoad);

            displaySystemDesiccantWheel.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
