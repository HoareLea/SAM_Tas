using SAM.Analytical.Systems;
using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static LabelDirection ToSAM(this tpdDirection tpdDirection)
        {
            switch(tpdDirection)
            {
                case tpdDirection.tpdTopBottom:
                    return LabelDirection.TopBottom;

                case tpdDirection.tpdBottomTop:
                    return LabelDirection.BottomTop;

                case tpdDirection.tpdLeftRight:
                    return LabelDirection.LeftRight;

                case tpdDirection.tpdRightLeft:
                    return LabelDirection.RightLeft;
            }

            return LabelDirection.Undefined;
        }
    }
}