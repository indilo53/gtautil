using System;
using System.Collections.Generic;

namespace GTAUtil
{
    public static class Verbs
    {
        public static List<VerbAttribute> Registered = new List<VerbAttribute>();
    }

    /// <summary>
    /// Verb attribute : example mycommand.exe verb --option
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class VerbAttribute : Attribute
    {
        public string Name;
        public string HelpText;

        public VerbAttribute(string name)
        {
            Name = name;
            Verbs.Registered.Add(this);
        }
    }

    /// <summary>
    /// Option attribute : example mycommand.exe verb --option
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptionAttribute : Attribute
    {
        public string ShortName;
        public string Name;
        public object Default;
        public string HelpText;

        public OptionAttribute(string name)
        {
            Name = name;
        }

        public OptionAttribute(string shortName, string name)
        {
            ShortName = shortName;
            Name      = name;
        }

        public OptionAttribute(char shortName, string name)
        {
            ShortName = shortName.ToString();
            Name = name;
        }
    }
}
