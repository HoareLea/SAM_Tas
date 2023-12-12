using Newtonsoft.Json.Linq;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public bool Add(ISystemEquipmentResult systemEquipmentResult, ISystemEquipment systemEquipment = null)
        {
            ISystemEquipmentResult systemEquipmentResult_Temp = systemEquipmentResult?.Clone();
            if (systemEquipmentResult_Temp == null)
            {
                return false;
            }

            if (relationCluster == null)
            {
                relationCluster = new RelationCluster();
            }

            bool result = relationCluster.AddObject(systemEquipmentResult_Temp);
            if (!result)
            {
                return result;
            }

            if (!relationCluster.Contains(systemEquipment))
            {
                Add(systemEquipment);
            }

            relationCluster.AddRelation(systemEquipment, systemEquipmentResult_Temp);
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

        public bool Add(SystemPlantRoom systemPlantRoom, IEnumerable<ISystemEquipment> systemEquipments)
        {
            SystemPlantRoom systemPlantRoom_Temp = systemPlantRoom?.Clone();
            if (systemPlantRoom_Temp == null)
            {
                return false;
            }

            if (relationCluster == null)
            {
                relationCluster = new RelationCluster();
            }

            bool result = relationCluster.AddObject(systemPlantRoom_Temp);
            if (!result)
            {
                return result;
            }

            if (systemEquipments == null || systemEquipments.Count() == 0)
            {
                return result;
            }

            foreach(ISystemEquipment systemEquipment in systemEquipments)
            {
                if (!relationCluster.Contains(systemEquipment))
                {
                    Add(systemEquipment);
                }

                relationCluster.AddRelation(systemPlantRoom, systemEquipment);
            }

            return true;
        }

        public List<SystemPlantRoom> GetSystemPlantRooms()
        {
            return relationCluster?.GetObjects<SystemPlantRoom>()?.ConvertAll(x => new SystemPlantRoom(x));
        }

        public List<SystemSpace> GetSystemSpaces()
        {
            return relationCluster?.GetObjects<SystemSpace>()?.ConvertAll(x => new SystemSpace(x));
        }

        public List<SystemSpace> GetSystemSpaces(SystemPlantRoom systemPlantRoom)
        {
            if(systemPlantRoom == null)
            {
                return null;
            }

            List<ISystemEquipment> systemEquipments = relationCluster.GetRelatedObjects<ISystemEquipment>(systemPlantRoom);
            if(systemEquipments == null || systemEquipments.Count == 0)
            {
                return null;
            }

            List<SystemSpace> result = new List<SystemSpace>();
            foreach(ISystemEquipment systemEquipment in systemEquipments)
            {
                List<SystemSpace> systemSpaces = relationCluster.GetRelatedObjects<SystemSpace>(systemEquipment);
                if(systemSpaces == null)
                {
                    continue;
                }

                foreach(SystemSpace systemSpace in systemSpaces)
                {
                    if(result.Find(x => x.Guid == systemSpace.Guid) != null)
                    {
                        continue;
                    }

                    result.Add(systemSpace);
                }
            }

            return result.ConvertAll(x => x.Clone());
        }

        public List<T> GetSystemEquipments<T>() where T : ISystemEquipment
        {
            return relationCluster?.GetObjects<ISystemEquipment>()?.ConvertAll(x => Core.Query.Clone(x)).FindAll(x => x is T).ConvertAll(x => (T)(object)x);
        }

        public List<ISystemEquipment> GetSystemEquipments()
        {
            return GetSystemEquipments<ISystemEquipment>();
        }

        public List<T> GetSystemEquipments<T>(SystemSpace systemSpace) where T : ISystemEquipment
        {
            return relationCluster.GetRelatedObjects<T>(systemSpace).ConvertAll(x => x.Clone());
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

        public T Find<T>(Func<T, bool> func) where T : ISystemObject, IJSAMObject
        {
            T t = relationCluster.GetObjects<T>(func).FirstOrDefault();
            if(t == null)
            {
                return t;
            }

            return t.Clone();
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
