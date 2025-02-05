using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDesiccantWheel ToSAM(this DesiccantWheel desiccantWheel)
        {
            if (desiccantWheel == null)
            {
                return null;
            }

            dynamic @dynamic = desiccantWheel;

            SystemDesiccantWheel result = new SystemDesiccantWheel(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            result.ScheduleName = ((dynamic)desiccantWheel )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)desiccantWheel);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDesiccantWheel displaySystemDesiccantWheel = Systems.Create.DisplayObject<DisplaySystemDesiccantWheel>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemDesiccantWheel != null)
            {
                ITransform2D transform2D = ((ISystemComponent)desiccantWheel).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDesiccantWheel.Transform(transform2D);
                }

                result = displaySystemDesiccantWheel;
            }

            return result;
        }
    }
}
