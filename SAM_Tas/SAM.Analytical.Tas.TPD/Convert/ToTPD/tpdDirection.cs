using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdDirection ToTPD(this LabelDirection labelDirection)
        {
            switch (labelDirection)
            {
                case LabelDirection.TopBottom:
                    return tpdDirection.tpdTopBottom;

                case LabelDirection.BottomTop:
                    return tpdDirection.tpdBottomTop;

                case LabelDirection.RightLeft:
                    return tpdDirection.tpdRightLeft;

                case LabelDirection.LeftRight:
                    return tpdDirection.tpdLeftRight;

            }

            throw new System.NotImplementedException();
        }
    }
}
