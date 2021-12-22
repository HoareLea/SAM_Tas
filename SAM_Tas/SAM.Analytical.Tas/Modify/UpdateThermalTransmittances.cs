using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static Dictionary<Panel, double> UpdateThermalTransmittances(this AdjacencyCluster adjacencyCluster, TBD.Building building)
        {
            List<TBD.Construction> constructions_TBD = building?.Constructions();
            if(constructions_TBD == null)
            {
                return null;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null)
            {
                return null;
            }

            Dictionary<Panel, double> result = new Dictionary<Panel, double>();
            foreach(Panel panel in panels)
            {
                Construction construction = panel?.Construction;
                if(construction == null)
                {
                    continue;
                }

                TBD.Construction construction_TBD = constructions_TBD.Find(x => x.name == construction.Name);
                if(construction_TBD == null)
                {
                    continue;
                }

                double thermalTransmittance = Query.ThermalTransmittance(construction_TBD, panel.PanelType);

                panel.SetValue(PanelParameter.ThermalTransmittance, thermalTransmittance);
                adjacencyCluster.AddObject(panel);
                result[panel] = thermalTransmittance;
            }

            return result;
        }
    }
}