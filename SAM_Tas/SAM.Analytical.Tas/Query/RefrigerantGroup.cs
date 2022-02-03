namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TPD.RefrigerantGroup RefrigerantGroup(this TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetRefrigerantGroupCount(); i++)
            {
                TPD.RefrigerantGroup refrigerantGroup = plantRoom.GetRefrigerantGroup(i);
                if (refrigerantGroup == null)
                {
                    continue;
                }

                if (name.Equals((refrigerantGroup as dynamic).Name))
                {
                    return refrigerantGroup;
                }
            }

            return null;
        }
    }
}