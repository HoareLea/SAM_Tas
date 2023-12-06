using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class ThermalTransmittances : IJSAMObject
    {
        private Dictionary<string, double> values = new Dictionary<string, double>();

        public ThermalTransmittances(JObject jObject)
        {
            FromJObject(jObject);
        }
        
        public ThermalTransmittances(ThermalTransmittances thermalTransmittances)
        {
            if(thermalTransmittances.values != null)
            {
                values = new Dictionary<string, double>();
                foreach(KeyValuePair<string, double> keyValuePair in thermalTransmittances.values)
                {
                    values[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        public ThermalTransmittances()
        {
           
        }

        public ThermalTransmittances(double horizontalExternal, double upExternal, double downExternal, double horizontalInternal, double upInternal, double downInternal, double transparent)
        {
            SetValue(HeatFlowDirection.Horizontal, true, horizontalExternal);
            SetValue(HeatFlowDirection.Up, true, upExternal);
            SetValue(HeatFlowDirection.Down, true, downExternal);

            SetValue(HeatFlowDirection.Horizontal, false, horizontalInternal);
            SetValue(HeatFlowDirection.Up, false, upInternal);
            SetValue(HeatFlowDirection.Down, false, downInternal);

            SetTransarentValue(transparent);
        }

        public void SetValue(HeatFlowDirection heatFlowDirection, bool external, double value)
        {
            if(heatFlowDirection == HeatFlowDirection.Undefined)
            {
                return;
            }

            if(values == null)
            {
                values = new Dictionary<string, double>();
            }

            values[Query.Key(heatFlowDirection, external)] = value;
        }

        public double GetValue(HeatFlowDirection heatFlowDirection, bool external)
        {
            if(values == null || heatFlowDirection == HeatFlowDirection.Undefined)
            {
                return double.NaN;
            }

            if(values.TryGetValue(Query.Key(heatFlowDirection, external), out double result))
            {
                return result;
            }

            return double.NaN;
        }

        public double GetValue(PanelType panelType)
        {
            if (values == null || panelType == PanelType.Undefined)
            {
                return double.NaN;
            }

            switch (panelType)
            {
                case PanelType.Shade:
                case PanelType.SolarPanel:
                case PanelType.Undefined:
                    return double.NaN;

                case PanelType.UndergroundWall:
                case PanelType.WallExternal:
                case PanelType.Wall:
                    return GetValue(HeatFlowDirection.Horizontal, true);

                case PanelType.Roof:
                    return GetValue(HeatFlowDirection.Up, true);

                case PanelType.UndergroundCeiling:
                case PanelType.UndergroundSlab:
                case PanelType.SlabOnGrade:
                case PanelType.FloorExposed:
                    return GetValue(HeatFlowDirection.Down, true);

                case PanelType.WallInternal:
                    return GetValue(HeatFlowDirection.Horizontal, false);

                case PanelType.Ceiling:
                    return GetValue(HeatFlowDirection.Up, false);

                case PanelType.FloorInternal:
                case PanelType.FloorRaised:
                case PanelType.Floor:
                    return GetValue(HeatFlowDirection.Down, false);

                case PanelType.CurtainWall:
                    return GetTransparentValue();
            }

            return double.NaN;
        }

        public void SetTransarentValue(double value)
        {
            if(values == null)
            {
                values = new Dictionary<string, double>();
            }

            values[string.Empty] = value;
        }

        public double GetTransparentValue()
        {
            if (values == null)
            {
                return double.NaN;
            }

            if (values.TryGetValue(string.Empty, out double result))
            {
                return result;
            }

            return double.NaN;
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Values"))
            {
                JArray jArray = jObject.Value<JArray>("Values");
                if(jArray != null)
                {
                    foreach(JObject jObject_Value in jArray)
                    {
                        if(!jObject_Value.ContainsKey("Key"))
                        {
                            continue;
                        }

                        if (!jObject_Value.ContainsKey("Value"))
                        {
                            continue;
                        }

                        double value = jObject_Value.Value<double>("Value");

                        HeatFlowDirection heatFlowDirection = Query.HeatFlowDirection(jObject_Value.Value<string>("Key"), out bool external);
                        if(heatFlowDirection == HeatFlowDirection.Undefined)
                        {
                            SetTransarentValue(value);
                        }
                        else
                        {
                            SetValue(heatFlowDirection, external, value);
                        }
                    }
                }
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(values != null)
            {
                JArray jArray = new JArray();
                foreach(KeyValuePair<string, double> keyValuePair in values)
                {
                    if(double.IsNaN(keyValuePair.Value))
                    {
                        continue;
                    }

                    JObject jObject_Value = new JObject();
                    jObject_Value.Add("Key", keyValuePair.Key);
                    jObject_Value.Add("Value", keyValuePair.Value);

                    jArray.Add(jObject_Value);
                }

                jObject.Add("Values", jArray);
            }

            return jObject;
        }
    }
}
