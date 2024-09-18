using System.ComponentModel;

namespace SAM.Core.Tas.TPD
{
    [Description("Result Period.")]
    public enum ResultPeriod
    {
        [Description("Undefined")] Undefined,
        [Description("Hourly")] Hourly,
        [Description("Daily")] Daily,
        [Description("Weekly")] Weekly,
        [Description("Monthly")] Monthly,
        [Description("Annual")] Annual,
    }
}
