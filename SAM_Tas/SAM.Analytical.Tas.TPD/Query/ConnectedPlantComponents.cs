using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.PlantComponent> ConnectedPlantComponents(this global::TPD.PlantComponent plantComponent, Direction direction)
        {
            List<global::TPD.Pipe> pipes = Pipes(plantComponent, direction);
            if(pipes == null)
            {
                return null;
            }

            List<global::TPD.PlantComponent> result = new List<global::TPD.PlantComponent>();
            foreach (global::TPD.Pipe pipe in pipes)
            {
                global::TPD.PlantComponent plantComponent_Temp = null;

                plantComponent_Temp = pipe.GetDownstreamComponent();
                if(plantComponent_Temp != null && plantComponent_Temp != plantComponent && !result.Contains(plantComponent_Temp))
                {
                    result.Add(plantComponent_Temp);
                }

                plantComponent_Temp = pipe.GetUpstreamComponent();
                if (plantComponent_Temp != null && plantComponent_Temp != plantComponent && !result.Contains(plantComponent_Temp))
                {
                    result.Add(plantComponent_Temp);
                }
            }

            return result;
        }

        public static List<global::TPD.PlantComponent> ConnectedPlantComponents(this global::TPD.PlantComponent plantComponent, bool includeNested = false)
        {
            if(plantComponent == null)
            {
                return null;
            }

            if(includeNested)
            {
                List<global::TPD.PlantComponent> result = new List<global::TPD.PlantComponent>(); ;

                ConnectedPlantComponents(plantComponent, ref result);
                return result;
            }

            Dictionary<string, global::TPD.PlantComponent> dictionary = new Dictionary<string, global::TPD.PlantComponent>();

            foreach (Direction direction in new Direction[] { Direction.In, Direction.Out })
            {
                List<global::TPD.PlantComponent> plantComponents = ConnectedPlantComponents(plantComponent, direction);
                if (plantComponents != null)
                {
                    foreach (global::TPD.PlantComponent plantComponent_Temp in plantComponents)
                    {
                        string reference = plantComponent_Temp.Reference();
                        if (string.IsNullOrWhiteSpace(reference))
                        {
                            continue;
                        }

                        dictionary[reference] = plantComponent_Temp;
                    }
                }
            }

            return dictionary.Values.ToList();
        }

        public static bool ConnectedPlantComponents(this global::TPD.PlantComponent plantComponent, ref List<global::TPD.PlantComponent> plantComponents)
        {
            string reference = plantComponent?.Reference();
            if (string.IsNullOrWhiteSpace(reference))
            {
                return false;
            }

            if(plantComponents == null)
            {
                plantComponents = new List<global::TPD.PlantComponent>();
            }

            bool result = false;

            if(plantComponents.Find(x => x?.Reference() == reference) == null)
            {
                plantComponents.Add(plantComponent);
                result = true;
            }

            List<global::TPD.PlantComponent> plantComponents_Connected = ConnectedPlantComponents(plantComponent);
            if(plantComponents_Connected == null || plantComponents_Connected.Count == 0)
            {
                return result;
            }

            foreach(global::TPD.PlantComponent plantComponent_Connected in plantComponents_Connected)
            {
                string reference_Connected = plantComponent_Connected?.Reference();
                if (string.IsNullOrWhiteSpace(reference_Connected))
                {
                    return false;
                }

                if (plantComponents.Find(x => x?.Reference() == reference_Connected) != null)
                {
                    continue;
                }

                if(ConnectedPlantComponents(plantComponent_Connected, ref plantComponents))
                {
                    result = true;
                }
            }

            return result;


            //List<global::TPD.PlantComponent> result = new List<global::TPD.PlantComponent>() { plantComponent };

            //List<global::TPD.PlantComponent> plantComponents = ConnectedPlantComponents(plantComponent);
            //if(plantComponents == null)
            //{
            //    return null;
            //}

            //if (plantComponents.Count == 0)
            //{
            //    return result;
            //}

            //foreach (global::TPD.PlantComponent plantComponent_Temp in plantComponents)
            //{
            //    reference = plantComponent_Temp?.Reference();
            //    if (string.IsNullOrWhiteSpace(reference))
            //    {
            //        continue;
            //    }

            //    if(references.Contains(reference))
            //    {
            //        continue;
            //    }

            //    result.Add(plantComponent_Temp);
            //    references.Add(reference);

            //    List<global::TPD.PlantComponent> plantComponents_Temp = ConnectedPlantComponents(plantComponent_Temp, references);
            //    if(plantComponents_Temp == null || plantComponents_Temp.Count == 0)
            //    {
            //        continue;
            //    }

            //    foreach(global::TPD.PlantComponent plantComponent_Temp_Temp in plantComponents_Temp)
            //    {
            //        reference = plantComponent_Temp_Temp?.Reference();
            //        if (string.IsNullOrWhiteSpace(reference))
            //        {
            //            continue;
            //        }

            //        if (references.Contains(reference))
            //        {
            //            continue;
            //        }

            //        references.Add(reference);
            //        result.Add(plantComponent_Temp_Temp);
            //    }
            //}

            //return result;
        }

        public static List<List<global::TPD.PlantComponent>> ConnectedPlantComponents(this global::TPD.PlantRoom plantRoom)
        {
            List<global::TPD.PlantComponent> plantComponents = plantRoom?.PlantComponents<global::TPD.PlantComponent>(true);
            if(plantComponents == null)
            {
                return null;
            }

            Dictionary<string, global::TPD.PlantComponent> dictionary = new Dictionary<string, global::TPD.PlantComponent>();
            foreach(global::TPD.PlantComponent plantComponent in plantComponents)
            {
                string reference = plantComponent?.Reference();
                if(string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                dictionary[reference] = plantComponent;

            }

            List<List<global::TPD.PlantComponent>> result = new List<List<global::TPD.PlantComponent>>();

            while (dictionary.Count > 0)
            {
                global::TPD.PlantComponent plantComponent = dictionary.Values.First();

                dictionary.Remove(plantComponent.Reference());

                List<global::TPD.PlantComponent> plantComponents_Temp = ConnectedPlantComponents(plantComponent, true);
                if(plantComponents_Temp == null || plantComponents_Temp.Count == 0)
                {
                    continue;
                }

                result.Add(plantComponents_Temp);
                foreach(global::TPD.PlantComponent plantComponent_Temp in plantComponents_Temp)
                {
                    dictionary.Remove(plantComponent_Temp.Reference());
                }
            }

            return result;

        }
    }
}