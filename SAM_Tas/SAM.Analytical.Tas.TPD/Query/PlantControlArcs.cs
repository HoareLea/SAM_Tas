using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantControlArc> PlantControlArcs(this PlantController controller) 
        { 
            if(controller == null)
            {
                return null;
            }

            List<PlantControlArc> result = new List<PlantControlArc>();

            int count = controller.GetControlArcCount();
            for (int i = 1; i <= count; i++)
            {
                PlantControlArc plantControlArc = controller.GetControlArc(i);
                if (plantControlArc == null)
                {
                    continue;
                }

                result.Add(plantControlArc);
            }

            return result;
        }

        public static List<PlantControlArc> PlantControlArcs(this PlantComponent plantComponent)
        {
            if (plantComponent == null)
            {
                return null;
            }

            dynamic @dynamic = plantComponent;

            List<PlantControlArc> result = new List<PlantControlArc>();

            int portCount = @dynamic.GetControlPortCount();
            for (int i = 1; i <= portCount; i++)
            {
                int arcCount = @dynamic.GetControlArcCount(i);
                for (int j = 1; j <= arcCount; j++)
                {
                    PlantControlArc plantControlArc = dynamic.GetControlArc(i, j);
                    if (plantControlArc == null)
                    {
                        continue;
                    }

                    result.Add(plantControlArc);
                }
            }

            return result;
        }

        public static List<PlantControlArc> PlantControlArcs(this PlantController plantController, PlantComponentGroup plantComponentGroup, bool control = true, bool chain = true)
        {
            if(plantController == null || (!control && !chain))
            {
                return null;
            }

            List<PlantControlArc> result = new List<PlantControlArc>();

            if(chain)
            {
                List<PlantControlArc> plantControlArcs = plantController.ChainPlantControlArcs();
                if (plantControlArcs != null)
                {
                    foreach (PlantControlArc plantControlArc_Temp in plantControlArcs)
                    {
                        PlantComponentGroup plantGomponentGroup_Temp = plantControlArc_Temp.GetGroup();
                        if (plantGomponentGroup_Temp != null && plantGomponentGroup_Temp.Reference() == plantComponentGroup.Reference())
                        {
                            result.Add(plantControlArc_Temp);
                        }
                    }
                }
            }

            if(control)
            {
                List<PlantControlArc> plantControlArcs = plantController.PlantControlArcs();
                if (plantControlArcs != null)
                {
                    foreach (PlantControlArc plantControlArc_Temp in plantControlArcs)
                    {
                        PlantComponentGroup plantComponentGroup_Temp = plantControlArc_Temp.GetGroup();
                        if (plantComponentGroup_Temp != null && plantComponentGroup_Temp.Reference() == plantComponentGroup.Reference())
                        {
                            result.Add(plantControlArc_Temp);
                        }
                    }
                }
            }



            return result;
        }
    }
}