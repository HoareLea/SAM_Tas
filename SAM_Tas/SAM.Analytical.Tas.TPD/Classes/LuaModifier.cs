using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class LuaModifier : SimpleModifier
    {
        public string Code { get; set; }

        public LuaModifier(ArithmeticOperator arithmeticOperator, string code)
        {
            ArithmeticOperator = arithmeticOperator;
            Code = code;
        }

        public LuaModifier(LuaModifier luaModifier)
            : base(luaModifier)
        {
            if(luaModifier != null)
            {
                Code = luaModifier.Code;
            }
        }

        public LuaModifier(JObject jObject)
            : base(jObject)
        {

        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if (jObject.ContainsKey("Code"))
            {
                Code = jObject.Value<string>("Code");
            }

            return result;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if (result == null)
            {
                return null;
            }

            if (Code != null)
            {
                result.Add("Code", Code);
            }

            return result;
        }
    }
}
