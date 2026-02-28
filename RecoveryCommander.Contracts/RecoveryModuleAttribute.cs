using System;

namespace RecoveryCommander.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RecoveryModuleAttribute : Attribute
    {
        public string Name { get; }
        public string Version { get; }

        public RecoveryModuleAttribute(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
