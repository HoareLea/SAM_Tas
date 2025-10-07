namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {

        public static TBD.FeatureShade ToTBD(this FeatureShade featureShade)
        {
            if(featureShade == null)
            {
                return null;
            }

            TBD.FeatureShade result = new TBD.FeatureShade();
            result.name = featureShade.Name;
            result.description = featureShade.Description;
            result.surfaceHeight = System.Convert.ToSingle(featureShade.SurfaceHeight);
            result.surfaceWidth = System.Convert.ToSingle(featureShade.SurfaceWidth);
            result.leftFinDepth = System.Convert.ToSingle(featureShade.LeftFinDepth);
            result.leftFinOffset = System.Convert.ToSingle(featureShade.LeftFinOffset);
            result.leftFinTransmittance = System.Convert.ToSingle(featureShade.LeftFinTransmittance);
            result.rightFinDepth = System.Convert.ToSingle(featureShade.RightFinDepth);
            result.rightFinOffset = System.Convert.ToSingle(featureShade.RightFinOffset);
            result.rightFinTransmittance = System.Convert.ToSingle(featureShade.RightFinTransmittance);
            result.overhangDepth = System.Convert.ToSingle(featureShade.OverhangDepth);
            result.overhangOffset = System.Convert.ToSingle(featureShade.OverhangOffset);
            result.overhangTransmittance = System.Convert.ToSingle(featureShade.OverhangTransmittance);

            return result;
        }
    }
}
