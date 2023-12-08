using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemModel : SAMModel
    {
        private RelationCluster relationCluster;

        public SystemModel(string name)
            :base(name)
        { 

        }

        public bool Add(SystemSpace systemSpace)
        {
            if(systemSpace == null)
            {
                return false;
            }

            if(relationCluster == null)
            {
                relationCluster = new RelationCluster();
            }

            return relationCluster.AddObject(new SystemSpace(systemSpace));
        }

        public bool Add(ISystemEquipment systemEquipment, SystemSpace systemSpace = null)
        {
            ISystemEquipment systemEquipment_Temp = systemEquipment?.Clone();
            if(systemEquipment_Temp == null)
            {
                return false;
            }

            if (relationCluster == null)
            {
                relationCluster = new RelationCluster();
            }

            bool result = relationCluster.AddObject(systemEquipment_Temp);
            if(!result)
            {
                return result;
            }

            if(systemSpace == null)
            {
                return result;
            }

            if(!relationCluster.Contains(systemSpace))
            {
                Add(systemSpace);
            }

            relationCluster.AddRelation(systemSpace, systemEquipment_Temp);
            return true;
        }

        public bool Add(SystemSpaceResult systemSpaceResult, SystemSpace systemSpace = null)
        {
            SystemSpaceResult systemSpaceResult_Temp = systemSpaceResult?.Clone();
            if (systemSpaceResult_Temp == null)
            {
                return false;
            }

            if (relationCluster == null)
            {
                relationCluster = new RelationCluster();
            }

            bool result = relationCluster.AddObject(systemSpaceResult_Temp);
            if (!result)
            {
                return result;
            }

            if (systemSpace == null)
            {
                return result;
            }

            if (!relationCluster.Contains(systemSpace))
            {
                Add(systemSpace);
            }

            relationCluster.AddRelation(systemSpace, systemSpaceResult_Temp);
            return true;
        }

        public List<SystemSpace> GetSystemSpaces()
        {
            return relationCluster?.GetObjects<SystemSpace>()?.ConvertAll(x => new SystemSpace(x));
        }

        public List<T> GetSystemEquipments<T>() where T : ISystemEquipment
        {
            return relationCluster?.GetObjects<ISystemEquipment>()?.ConvertAll(x => Core.Query.Clone(x)).FindAll(x => x is T).ConvertAll(x => (T)(object)x);
        }

        public List<ISystemEquipment> GetSystemEquipments()
        {
            return GetSystemEquipments<ISystemEquipment>();
        }

        public List<ISystemResult> GetSystemResults(ISystemObject systemObject)
        {
            return GetSystemResults<ISystemResult>(systemObject);
        }

        public List<T> GetSystemResults<T>(ISystemObject systemObject) where T : ISystemResult
        {
            if(relationCluster == null || systemObject == null)
            {
                return null;
            }

            return relationCluster.GetRelatedObjects<T>(systemObject)?.ConvertAll(x => Core.Query.Clone(x));
        }

        public List<T> GetSystemResults<T>() where T : ISystemResult
        {
            return relationCluster?.GetObjects<T>()?.ConvertAll(x => Core.Query.Clone(x));
        }

        public List<ISystemResult> GetSystemResults()
        {
            return GetSystemResults<ISystemResult>();
        }

        public List<ISystemObject> GetRelatedObjects(ISystemObject systemObject, System.Type type = null)
        {
            if(systemObject == null)
            {
                return null;
            }

            List<object> objects = type == null ? relationCluster?.GetRelatedObjects(systemObject, typeof(ISystemObject)) : relationCluster.GetRelatedObjects(systemObject, type);
            if(objects == null)
            {
                return null;
            }

            List<ISystemObject> result = new List<ISystemObject>();
            foreach(object @object in objects)
            {
                ISystemObject systemObject_Temp = (@object as ISystemObject)?.Clone();
                if(systemObject_Temp == null)
                {
                    continue;
                }

                result.Add(systemObject_Temp);
            }

            return result;
        }

        public List<T> GetRelatedObjects<T>(ISystemObject systemObject) where T : ISystemObject
        {
            if (systemObject == null)
            {
                return null;
            }

            List<ISystemObject> systemObjects = GetRelatedObjects(systemObject, typeof(T));
            if(systemObjects == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach(T systemObject_Temp in systemObjects)
            {
                result.Add(systemObject_Temp);
            }

            return result;
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return false;
            }

            if(jObject.ContainsKey("RelationCluster"))
            {
                relationCluster = new RelationCluster(jObject.Value<JObject>("RelationCluster"));
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if (result == null)
            {
                return result;
            }

            if(relationCluster != null)
            {
                result.Add("RelationCluster", relationCluster.ToJObject());
            }

            return result;
        }
    }
}
