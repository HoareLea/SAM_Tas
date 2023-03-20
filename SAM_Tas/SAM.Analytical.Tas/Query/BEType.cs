
namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static int BEType(this string text)
        {
            string text_Temp = text;
            text_Temp = text_Temp.Trim();

            if (text_Temp == "No Type")
                return 0;
            if (text_Temp == "Air")
                return 0;
            if (text_Temp == "Internal Wall")
                return 1;
            if (text_Temp == "External Wall")
                return 2;
            if (text_Temp == "Roof")
                return 3;
            if (text_Temp == "Internal Floor")
                return 4;
            if (text_Temp == "Shade")
                return 5;
            if (text_Temp == "Underground Wall")
                return 6;
            if (text_Temp == "Underground Slab")
                return 7;
            if (text_Temp == "Internal Ceiling")
                return 8;
            if (text_Temp == "Underground Ceiling")
                return 9;
            if (text_Temp == "Raised Floor")
                return 10;
            if (text_Temp == "Slab on Grade")
                return 11;
            if (text_Temp == "Glazing")
                return 12;
            if (text_Temp == "Rooflight")
                return 13;
            if (text_Temp == "Door")
                return 14;
            if (text_Temp == "Frame")
                return 15;
            if (text_Temp == "Curtain Wall")
                return 16;
            if (text_Temp == "xxxxxcrushesxxxx")
                return 17;
            if (text_Temp == "Solar/PV panel")
                return 18;
            if (text_Temp == "Exposed Floor")
                return 19;
            if (text_Temp == "Vehicle Door")
                return 20;

            return -1;
        }

        public static int BEType(this ApertureType apertureType, bool frame = false)
        {
            switch(apertureType)
            {
                case Analytical.ApertureType.Door:
                    return BEType("Door");
                case Analytical.ApertureType.Window:
                    return frame ? BEType("Frame") : BEType("Glazing");
            }

            return -1;
        }

        public static int BEType(this AperturePart aperturePart)
        {
            switch (aperturePart)
            {
                case Analytical.AperturePart.Pane:
                    return BEType("Glazing");
                case Analytical.AperturePart.Frame:
                    return BEType("Frame");
            }

            return -1;
        }
    }
}