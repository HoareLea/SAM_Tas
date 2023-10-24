namespace SAM.Analytical.Tas
{
    public class LayerThicknessCalculationResult
    {
        private string constructionName;
        private int layerIndex;
        private double thremalTransmittance;
        private double thickness;

        public LayerThicknessCalculationResult(string constructionName, int layerIndex, double thickness, double thremalTransmittance)
        {
            this.constructionName = constructionName;
            this.layerIndex = layerIndex;
            this.thickness = thickness;
            this.thremalTransmittance = thremalTransmittance;
        }

        public double Thickness
        {
            get
            {
                return thickness;
            }
        }

        public string ConstructionName
        {
            get
            {
                return constructionName;
            }
        }

        public int LayerIndex
        {
            get
            {
                return layerIndex;
            }
        }

        public double ThremalTransmittance
        {
            get
            {
                return thremalTransmittance;
            }
        }

    }
}
