using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class LightingDetail : UKBRElement
    {
        public override string UKBRName => "LightingDetail";

        public LightingDetail(XElement XElement)
            : base(XElement)
        {

        }

        public bool bBackSpaceSensor
        {
            get
            {
                return bool.Parse(xElement.Attribute("bBackSpaceSensor").Value);
            }
            set
            {
                xElement.Attribute("bBackSpaceSensor").Value = value.ToString().ToLower();
            }
        }

        public bool bEfficacyLightingFunc
        {
            get
            {
                return bool.Parse(xElement.Attribute("bEfficacyLightingFunc").Value);
            }
            set
            {
                xElement.Attribute("bEfficacyLightingFunc").Value = value.ToString().ToLower();
            }
        }

        public double LampEfficiencyPer100Lux
        {
            get
            {
                return double.Parse(xElement.Attribute("LampEfficiencyPer100Lux").Value);
            }
            set
            {
                xElement.Attribute("LampEfficiencyPer100Lux").Value = value.ToString();
            }
        }

        public double LampGeneralPowerDensity
        {
            get
            {
                return double.Parse(xElement.Attribute("LampGeneralPowerDensity").Value);
            }
            set
            {
                xElement.Attribute("LampGeneralPowerDensity").Value = value.ToString();
            }
        }

        public double LampEfficacy
        {
            get
            {
                return double.Parse(xElement.Attribute("LampEfficacy").Value);
            }
            set
            {
                xElement.Attribute("LampEfficacy").Value = value.ToString();
            }
        }

        public void SetDaylightControl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                DaylightControl = 0;
                return;
            }

            string value_Temp = value.ToUpper().Trim();
            value_Temp = value_Temp.Replace(" ", "");
            if (string.IsNullOrEmpty(value_Temp))
            {
                DaylightControl = 0;
                return;
            }
            // modified 4.12.2017 at 00:36
            if (value_Temp.Equals("NONE"))
            {
                DaylightControl = 0;
                return;
            }

            if (value_Temp.Contains("MANUAL"))
            {
                DaylightControl = 1;
                return;
            }

            // modified 4.12.2017 at 00:36
            if (value_Temp.Contains("PHOTOCELL") && value_Temp.Contains("OFF"))
            {
                DaylightControl = 2;
                return;
            }

            if (value_Temp.Contains("PHOTOCELL") && value_Temp.Contains("DIMMING"))
            {
                DaylightControl = 3;
                return;
            }

            DaylightControl = 0;
        }

        public int DaylightControl
        {
            get
            {
                return int.Parse(xElement.Attribute("DaylightControl").Value);
            }
            set
            {
                xElement.Attribute("DaylightControl").Value = value.ToString();
            }
        }

        public int LampType
        {
            get
            {
                return int.Parse(xElement.Attribute("LampType").Value);
            }
            set
            {
                xElement.Attribute("LampType").Value = value.ToString();
            }
        }

        public void SetAPD(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                APD = 0;
                return;
            }

            string value_Temp = value.ToUpper().Trim();
            value_Temp = value_Temp.Replace(" ", "");
            if (string.IsNullOrEmpty(value_Temp))
            {
                APD = 0;
                return;
            }

            if (value_Temp.Contains("MANUAL") && value_Temp.Contains("ON") && value_Temp.Contains("OFF"))
            {
                APD = 0;
                return;
            }

            if (value_Temp.Contains("MANUAL") && value_Temp.Contains("ON") && value_Temp.Contains("OFF") && value_Temp.Contains("AUTO"))
            {
                APD = 5;
                return;
            }

            if (value_Temp.Contains("MANUAL") && value_Temp.Contains("AUTO") && value_Temp.Contains("OFF"))
            {
                APD = 4;
                return;
            }

            if (value_Temp.Contains("MANUAL") && value_Temp.Contains("DIMMED"))
            {
                APD = 3;
                return;
            }

            if (value_Temp.Contains("AUTO") && value_Temp.Contains("OFF") && value_Temp.Contains("ON"))
            {
                APD = 2;
                return;
            }

            if (value_Temp.Contains("AUTO") && value_Temp.Contains("DIMMED"))
            {
                APD = 1;
                return;
            }


            APD = 0;
        }

        public int APD
        {
            get
            {
                return int.Parse(xElement.Attribute("APD").Value);
            }
            protected set
            {
                xElement.Attribute("APD").Value = value.ToString();
            }
        }

        public double MinimumPercentage
        {
            get
            {
                return double.Parse(xElement.Attribute("MinimumPercentage").Value);
            }
            set
            {
                xElement.Attribute("MinimumPercentage").Value = value.ToString();
            }
        }

        public double DesignIlluminance
        {
            get
            {
                return double.Parse(xElement.Attribute("DesignIlluminance").Value);
            }
            set
            {
                xElement.Attribute("DesignIlluminance").Value = value.ToString();
            }
        }

        //public int Country
        //{
        //    get
        //    {
        //        return int.Parse(xElement.Attribute("Country").Value);
        //    }
        //    set
        //    {
        //        xElement.Attribute("Country").Value = value.ToString();
        //    }
        //}

        //public void SetCountry(string value)
        //{
        //    if (string.IsNullOrEmpty(value))
        //    {
        //        Country = 0;
        //        return;
        //    }

        //    string value_Temp = value.ToUpper().Trim();
        //    value_Temp = value_Temp.Replace(" ", "");
        //    if (string.IsNullOrEmpty(value_Temp))
        //    {
        //        Country = 0;
        //        return;
        //    }

        //    if (value_Temp.Contains("ENGLAND"))
        //    {
        //        Country = 0;
        //        return;
        //    }

        //    if (value_Temp.Contains("SCOTLAND"))
        //    {
        //        Country = 1;
        //        return;
        //    }

        //    if (value_Temp.Contains("NORTHERN"))
        //    {
        //        Country = 2;
        //        return;
        //    }

        //    if (value_Temp.Contains("REPUBLIC"))
        //    {
        //        Country = 3;
        //        return;
        //    }

        //    if (value_Temp.Contains("WALES"))
        //    {
        //        Country = 4;
        //        return;
        //    }


        //    Country = 0;
        //}

        public double TargetIlluminance
        {
            get
            {
                return double.Parse(xElement.Attribute("TargetIlluminance").Value);
            }
            set
            {
                xElement.Attribute("TargetIlluminance").Value = value.ToString();
            }
        }

        public double MinIlluminance
        {
            get
            {
                return double.Parse(xElement.Attribute("MinIlluminance").Value);
            }
            set
            {
                xElement.Attribute("MinIlluminance").Value = value.ToString();
            }
        }

        public double ParasiticPower
        {
            get
            {
                return double.Parse(xElement.Attribute("ParasiticPower").Value);
            }
            set
            {
                xElement.Attribute("ParasiticPower").Value = value.ToString();
            }
        }

        public bool PhotocellClock
        {
            get
            {
                return bool.Parse(xElement.Attribute("PhotocellClock").Value);
            }
            set
            {
                xElement.Attribute("PhotocellClock").Value = value.ToString().ToLower();
            }
        }

        public double AreaCutoff
        {
            get
            {
                return double.Parse(xElement.Attribute("AreaCutoff").Value);
            }
            set
            {
                xElement.Attribute("AreaCutoff").Value = value.ToString();
            }
        }

        //public double AirPermeability
        //{
        //    get
        //    {
        //        return double.Parse(xElement.Attribute("AirPermeability").Value);
        //    }
        //    set
        //    {
        //        xElement.Attribute("AirPermeability").Value = value.ToString();
        //    }
        //}

        public bool bUserDaylightFactor
        {
            get
            {
                return bool.Parse(xElement.Attribute("bUserDaylightFactor").Value);
            }
            set
            {
                xElement.Attribute("bUserDaylightFactor").Value = value.ToString().ToLower();
            }
        }

        public bool UserDaylightFactor
        {
            get
            {
                return bool.Parse(xElement.Attribute("UserDaylightFactor").Value);
            }
            set
            {
                xElement.Attribute("UserDaylightFactor").Value = value.ToString().ToLower();
            }
        }

        public double DesignDaylightFactor
        {
            get
            {
                return double.Parse(xElement.Attribute("DesignDaylightFactor").Value);
            }
            set
            {
                xElement.Attribute("DesignDaylightFactor").Value = value.ToString();
            }
        }

        public double NotionalDaylightFactor
        {
            get
            {
                return double.Parse(xElement.Attribute("NotionalDaylightFactor").Value);
            }
            set
            {
                xElement.Attribute("NotionalDaylightFactor").Value = value.ToString();
            }
        }

        public double ReferenceDaylightFactor
        {
            get
            {
                return double.Parse(xElement.Attribute("ReferenceDaylightFactor").Value);
            }
            set
            {
                xElement.Attribute("ReferenceDaylightFactor").Value = value.ToString();
            }
        }

        public double DisplayLightingLampEfficacy
        {
            get
            {
                return double.Parse(xElement.Attribute("DisplayLightingLampEfficacy").Value);
            }
            set
            {
                xElement.Attribute("DisplayLightingLampEfficacy").Value = value.ToString();
            }
        }

        public bool DisplayLightingATS
        {
            get
            {
                return bool.Parse(xElement.Attribute("DisplayLightingATS").Value);
            }
            set
            {
                xElement.Attribute("DisplayLightingATS").Value = value.ToString().ToLower();
            }
        }

        //public bool IsMainsGasAvailable
        //{
        //    get
        //    {
        //        return bool.Parse(xElement.Attribute("IsMainsGasAvailable").Value);
        //    }
        //    set
        //    {
        //        xElement.Attribute("IsMainsGasAvailable").Value = value.ToString().ToLower();
        //    }
        //}

        public double MaintenanceFactor
        {
            get
            {
                return double.Parse(xElement.Attribute("MaintenanceFactor").Value);
            }
            set
            {
                xElement.Attribute("MaintenanceFactor").Value = value.ToString();
            }
        }

        public bool bConstantIlluminanceControl
        {
            get
            {
                return bool.Parse(xElement.Attribute("bConstantIlluminanceControl").Value);
            }
            set
            {
                xElement.Attribute("bConstantIlluminanceControl").Value = value.ToString().ToLower();
            }
        }

        public double NotionalParasiticPower
        {
            get
            {
                return double.Parse(xElement.Attribute("NotionalParasiticPower").Value);
            }
            set
            {
                xElement.Attribute("NotionalParasiticPower").Value = value.ToString();
            }
        }
    }
}
