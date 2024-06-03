namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static global::TPD.RefrigerantGroup RefrigerantGroup(this global::TPD.PlantRoom plantRoom, string name)
        {
            if (plantRoom is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            for (int i = 1; i <= plantRoom.GetRefrigerantGroupCount(); i++)
            {
                global::TPD.RefrigerantGroup refrigerantGroup = plantRoom.GetRefrigerantGroup(i);
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