using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemExchanger ToSAM(this Exchanger exchanger)
        {
            if (exchanger == null)
            {
                return null;
            }

            dynamic @dynamic = exchanger;

            SystemExchanger result = new SystemExchanger(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.SensibleEfficiency = exchanger.SensibleEfficiency.Value;
            result.LatentEfficiency = exchanger.LatentEfficiency.Value;

            tpdExchangerFlags tpdExchangerFlags = (tpdExchangerFlags)exchanger.Flags;
            result.HeatingOnly = tpdExchangerFlags.HasFlag(tpdExchangerFlags.tpdExchangerFlagHeatingOnly);
            result.AdjustForOptimiser = tpdExchangerFlags.HasFlag(tpdExchangerFlags.tpdExchangerFlagAdjustForOptimiser);

            result.ScheduleName = ((dynamic)exchanger )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)exchanger);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemExchanger displaySystemExchanger = Systems.Create.DisplayObject<DisplaySystemExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemExchanger != null)
            {
                ITransform2D transform2D = ((ISystemComponent)exchanger).Transform2D();
                if (transform2D != null)
                {
                    displaySystemExchanger.Transform(transform2D);
                }

                result = displaySystemExchanger;
            }

            return result;
        }
    }
}