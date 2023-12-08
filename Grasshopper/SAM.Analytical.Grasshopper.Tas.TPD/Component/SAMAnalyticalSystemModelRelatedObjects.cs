using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Tas.TPD;
using SAM.Analytical.Grasshopper.Tas.TPD.Properties;
using SAM.Analytical.Tas.TPD;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public class SAMAnalyticalSystemModelRelatedObjects : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("45fc07cc-37fb-47a0-9bf8-0524248e6875");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small3;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalSystemModelRelatedObjects()
          : base("SystemModel.RelatedObjects", "SystemModel.RelatedObjects",
              "Related Objects in SystemModel",
              "SAM WIP", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooSystemModelParam() { Name = "_systemModel", NickName = "_systemModel", Description = "SAM Analytical SystemModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSystemObjectParam() { Name = "_systemObject", NickName = "_systemObject", Description = "SystemObject", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "type_", NickName = "type_", Description = "Type", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooSystemObjectParam() { Name = "systemObjects", NickName = "systemObjects", Description = "Related SystemObjects", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index = -1;

            SystemModel systemModel = null;
            index = Params.IndexOfInputParam("_systemModel");
            if (index == -1 || !dataAccess.GetData(index, ref systemModel) || systemModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ISystemObject systemObject = null;
            index = Params.IndexOfInputParam("_systemObject");
            if (index == -1 || !dataAccess.GetData(index, ref systemObject) || systemObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Type type = null;
            index = Params.IndexOfInputParam("type_");
            if (index != -1)
            {
                string fullTypeName = null;
                if (dataAccess.GetData(index, ref fullTypeName))
                {
                    try
                    {
                        type = Type.GetType(fullTypeName);
                    }
                    catch
                    {
                        type = null;
                    }
                }
            }

            List<ISystemObject> result = null;
            if (type == null)
                result = systemModel.GetRelatedObjects(systemObject);
            else
                result = systemModel.GetRelatedObjects(systemObject, type);


            index = Params.IndexOfOutputParam("systemObjects");
            if (index != -1)
                dataAccess.SetDataList(index, result);



            //HashSet<string> names = new HashSet<string>();
            //foreach (object @object in result)
            //{
            //    object value = @object;
            //    if (@object is IGH_Goo)
            //    {
            //        value = (@object as dynamic).Value;
            //    }

            //    string name = value?.GetType()?.Name;
            //    if (string.IsNullOrWhiteSpace(name))
            //    {
            //        continue;
            //    }

            //    names.Add(name);
            //}

            //List<string> names_Temp = names.ToList();
            //names_Temp.Sort();

            //IEnumerable<GooObjectParam> gooParameterParams = names_Temp.ConvertAll(x => new GooObjectParam(x));


            //Dictionary<string, IList<IGH_Param>> dictionary = new Dictionary<string, IList<IGH_Param>>();
            //foreach (IGH_Param param in Params.Output)
            //{
            //    if (param.Recipients == null && param.Recipients.Count == 0)
            //        continue;

            //    GooObjectParam gooParameterParam = param as GooObjectParam;
            //    if (gooParameterParam == null)
            //        continue;

            //    dictionary.Add(gooParameterParam.Name, new List<IGH_Param>(gooParameterParam.Recipients));
            //}

            //while (Params.Output != null && Params.Output.Count() > 0)
            //    Params.UnregisterOutputParameter(Params.Output[0]);

            //if (gooParameterParams != null)
            //{
            //    foreach (GooObjectParam gooParameterParam in gooParameterParams)
            //    {
            //        if (gooParameterParam == null)
            //            continue;

            //        if (gooParameterParam.Attributes is null)
            //            gooParameterParam.Attributes = new GH_LinkedParamAttributes(gooParameterParam, Attributes);

            //        gooParameterParam.Access = GH_ParamAccess.list;
            //        Params.RegisterOutputParam(gooParameterParam);

            //        IList<IGH_Param> @params = null;

            //        if (!dictionary.TryGetValue(gooParameterParam.Name, out @params))
            //            continue;

            //        foreach (IGH_Param param in @params)
            //            param.AddSource(gooParameterParam);
            //    }
            //}

            //List<Tuple<int, string, List<GooObject>>> tuples = new List<Tuple<int, string, List<GooObject>>>();
            //for (int i = 0; i < Params.Output.Count; ++i)
            //{
            //    GooObjectParam gooParameterParam = Params.Output[i] as GooObjectParam;
            //    if (gooParameterParam == null || string.IsNullOrWhiteSpace(gooParameterParam.Name))
            //    {
            //        continue;
            //    }

            //    tuples.Add(new Tuple<int, string, List<GooObject>>(i, gooParameterParam.Name, new List<GooObject>()));
            //}

            //for (int i = 0; i < result.Count; i++)
            //{
            //    object @object = result[i];

            //    if (@object is IGH_Goo)
            //    {
            //        @object = (@object as dynamic).Value;
            //    }

            //    if (@object is Rhino.Geometry.GeometryBase)
            //    {
            //        if (((Rhino.Geometry.GeometryBase)@object).Disposed)
            //        {
            //            return;
            //        }

            //        @object = ((Rhino.Geometry.GeometryBase)@object).Duplicate();
            //    }

            //    if (@object is IJSAMObject)
            //    {
            //        IJSAMObject jSAMObject = Core.Query.Clone((IJSAMObject)@object);
            //        if (jSAMObject != null)
            //            @object = jSAMObject;
            //    }

            //    string name = @object?.GetType()?.Name;
            //    if (string.IsNullOrWhiteSpace(name))
            //    {
            //        continue;
            //    }

            //    List<GooObject> gooObjects = tuples.Find(x => name.Equals(x.Item2))?.Item3;
            //    if (gooObjects == null)
            //    {
            //        continue;
            //    }

            //    gooObjects.Add(new GooObject(@object));

            //}

            //for (int i = 0; i < Params.Output.Count; ++i)
            //{
            //    List<GooObject> gooObjects = tuples.Find(x => x.Item1 == i)?.Item3;
            //    dataAccess.SetDataList(i, gooObjects);
            //}

            //Ex
        }
    }
}