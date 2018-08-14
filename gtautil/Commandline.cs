using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace GTAUtil
{
    public class CommandLine
    {
        /// <summary>
        /// Parse a parsing target T
        /// </summary>
        public static void Parse<T>(string[] args, Action<T> callback) where T : class, new()
        {
            T parsingTarget = null;
            var verbAttribute = GetVerbAttribute<T>();

            if (args.Length > 0 && args[0] == verbAttribute.Name)
            {
                parsingTarget = new T();
                AssignOptions(parsingTarget, args);

                if (args.Length == 0 || (args.Length > 1 && args[1] == "help"))
                {
                    Console.Error.Write(GenHelp<T>());
                    return;
                }
            }

            if (parsingTarget != null)
            {
                callback(parsingTarget);
            }
        }

        /// <summary>
        /// Get verb attribute from parsing target T
        /// </summary>
        private static VerbAttribute GetVerbAttribute<T>() where T : class
        {
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            var attrs = typeInfo.GetCustomAttributes();

            foreach (var attr in attrs)
            {
                if (attr.GetType().Name == "VerbAttribute")
                {
                    return attr as VerbAttribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Get pairs of PropertyInfo / OptionAttribute from parsing target T
        /// </summary>
        private static Tuple<PropertyInfo, OptionAttribute>[] GetOptionAttributes<T>() where T : class
        {
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            var props = typeInfo.GetProperties();
            var optionAttrs = new List<Tuple<PropertyInfo, OptionAttribute>>();

            foreach (var prop in props)
            {
                var attrs = prop.GetCustomAttributes();

                foreach (var attr in attrs)
                {
                    if (attr.GetType().Name == "OptionAttribute")
                    {
                        optionAttrs.Add(new Tuple<PropertyInfo, OptionAttribute>(prop, attr as OptionAttribute));
                        break;
                    }
                }

            }

            return optionAttrs.ToArray();
        }

        /// <summary>
        /// Assign values to parsing target T from arg array
        /// </summary>
        private static void AssignOptions<T>(T obj, string[] args) where T : class
        {
            var optionAttrs = GetOptionAttributes<T>();

            foreach (var attr in optionAttrs)
            {
                bool found = false;

                for (int i = 0; i < args.Length; i++)
                {
                    var type = attr.Item1.PropertyType;

                    if (args[i] == "--" + attr.Item2.Name || args[i] == "-" + attr.Item2.ShortName)
                    {
                        found = true;

                        if (type == typeof(bool)) // Parse bool (switch)
                        {
                            attr.Item1.SetValue(obj, !(bool)attr.Item2.Default);

                        }
                        else if (type == typeof(string)) // Parse string
                        {
                            if (args.Length > i + 1 && !args[i + 1].StartsWith("-"))
                            {
                                attr.Item1.SetValue(obj, args[i + 1]);
                            }
                            else
                            {
                                attr.Item1.SetValue(obj, (string)attr.Item2.Default);
                            }
                        }
                        else if (type == typeof(int)) // Parse int
                        {
                            if (args.Length > i + 1)
                            {
                                attr.Item1.SetValue(obj, Convert.ToInt32(args[i + 1]));
                            }
                            else
                            {
                                attr.Item1.SetValue(obj, (int)attr.Item2.Default);
                            }
                        }
                        else if (type == typeof(float)) // Parse float
                        {
                            if (args.Length > i + 1)
                            {
                                attr.Item1.SetValue(obj, Convert.ToSingle(args[i + 1].Replace('.', ',')));
                            }
                            else
                            {
                                attr.Item1.SetValue(obj, (float)attr.Item2.Default);
                            }
                        }
                        else if (type == typeof(List<string>)) // Parse array of string
                        {
                            attr.Item1.SetValue(obj, args[i + 1].Split(',').ToList());
                        }
                        else if (type == typeof(List<int>)) // Parse array of int
                        {
                            var list    = new List<int>();
                            var listStr = args[i + 1].Split(',').ToList();

                            for (int j = 0; j < listStr.Count; j++)
                                list.Add(Convert.ToInt32(listStr[j]));

                            attr.Item1.SetValue(obj, list);
                        }
                        else if (type == typeof(List<float>)) // Parse array of float
                        {
                            var list = new List<float>();
                            var listStr = args[i + 1].Split(',');

                            for (int j = 0; j < listStr.Length; j++)
                                list.Add(Convert.ToSingle(listStr[j].Replace('.', ',')));

                            attr.Item1.SetValue(obj, list);
                        }

                        break;
                    }

                }

                if (!found)
                {
                    attr.Item1.SetValue(obj, attr.Item2.Default);
                }
            }
        }

        /// <summary>
        /// Generic help - Show which verbs can be used
        /// </summary>
        public static string GenHelp()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var sb = new StringBuilder();

            sb.AppendLine(assembly.Name + " " + assembly.Version + "\n");

            int longerVerbLength = 0;

            foreach (var verb in Verbs.Registered)
            {
                if (verb.Name.Length > longerVerbLength)
                {
                    longerVerbLength = verb.Name.Length;
                }
            }

            foreach (var verb in Verbs.Registered)
            {
                int lengthDiff = longerVerbLength - verb.Name.Length;
                sb.AppendLine("  " + verb.Name + new string(' ', 4 + lengthDiff) + verb.HelpText);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Specific help - Show what args are declared in parsing target T
        /// </summary>
        private static string GenHelp<T>() where T : class
        {
            var optionAttrs = GetOptionAttributes<T>();
            var assembly = Assembly.GetExecutingAssembly().GetName();
            var sb = new StringBuilder();
            var attrInfos = new List<string>();
            var attrHelpTexts = new List<string>();
            int longerAttrInfoLength = 0;

            foreach (var attr in optionAttrs)
            {
                var type = attr.Item1.PropertyType;
                var shortName = attr.Item2.ShortName;
                var name = attr.Item2.Name;

                sb.Append("--" + name);

                if (shortName != null)
                {
                    sb.Append(", -" + shortName);
                }

                var attrInfo = sb.ToString();

                if (attrInfo.Length > longerAttrInfoLength)
                {
                    longerAttrInfoLength = attrInfo.Length;
                }

                attrInfos.Add(attrInfo);
                attrHelpTexts.Add(attr.Item2.HelpText);

                sb.Clear();
            }

            sb.AppendLine(assembly.Name + " " + assembly.Version + "\n");

            for (int i = 0; i < attrInfos.Count; i++)
            {
                int lengthDiff = longerAttrInfoLength - attrInfos[i].Length;
                sb.AppendLine("  " + attrInfos[i] + new string(' ', 4 + lengthDiff) + attrHelpTexts[i]);
            }

            sb.AppendLine();

            return sb.ToString();
        }
    }

}