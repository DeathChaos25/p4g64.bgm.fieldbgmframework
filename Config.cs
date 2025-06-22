using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using p4g64.bgm.fieldbgmframework.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace p4g64.bgm.fieldbgmframework.Configuration
{
    public class Config : Configurable<Config>
    {
        [DisplayName("Debug")]
        [Description("Display extra messages for debugging purposes")]
        [DefaultValue(false)]
        [Display(Order = 0)]
        public bool Debug { get; set; } = false;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
