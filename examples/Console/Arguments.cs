/*
  █▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀  ▀  ▀      ▀▀
  █  The MIT License (MIT)
  █
  █  Copyright (c) 2017 JP Dillingham (jp@dillingham.ws)
  █
  █  Permission is hereby granted, free of charge, to any person obtaining a copy
  █  of this software and associated documentation files (the "Software"), to deal
  █  in the Software without restriction, including without limitation the rights
  █  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  █  copies of the Software, and to permit persons to whom the Software is
  █  furnished to do so, subject to the following conditions:
  █
  █  The above copyright notice and this permission notice shall be included in all
  █  copies or substantial portions of the Software.
  █
  █  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  █  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  █  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  █  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  █  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  █  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  █  SOFTWARE.
  █
  ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀  ▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀██
                                                                                               ██
                                                                                           ▀█▄ ██ ▄█▀
                                                                                             ▀████▀
                                                                                               ▀▀                            */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]

namespace Utility.CommandLine
{
    /// <summary>
    ///     Provides extension method(s) for the Argument namespace.
    /// </summary>
    public static class ArgumentsExtensions
    {
        /// <summary>
        ///     Adds the specified key to the specified dictionary with the specified value, but only if the specified key is not
        ///     already present in the dictionary. If it is present, a list is created and the new value is added to the list,
        ///     along with all subsequent values.
        /// </summary>
        /// <param name="dictionary">The dictionary to which they specified key and value are to be added.</param>
        /// <param name="key">The key to add to the dictionary.</param>
        /// <param name="value">The value corresponding to the specified key.</param>
        internal static void ExclusiveAdd(this Dictionary<string, object> dictionary, string key, object value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
            else
            {
                var type = dictionary[key].GetType();

                if (dictionary[key].GetType() == typeof(List<object>))
                {
                    ((List<object>)dictionary[key]).Add(value);
                }
                else
                {
                    object existingValue = dictionary[key];

                    dictionary[key] = new List<object>(new object[] { existingValue, value });
                }
            }
        }
    }

    /// <summary>
    ///     Indicates that the property is to be used as a target for automatic population of values from command line arguments
    ///     when invoking the <see cref="Arguments.Populate(string, bool, string)"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ArgumentAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ArgumentAttribute"/> class.
        /// </summary>
        /// <param name="shortName">The short name of the argument, represented as a single character.</param>
        /// <param name="longName">The long name of the argument.</param>
        /// <param name="helpText">The help text of the argument.</param>
        public ArgumentAttribute(char shortName, string longName, string helpText = null)
        {
            ShortName = shortName;
            LongName = longName;
            HelpText = helpText;
        }

        /// <summary>
        ///     Gets or sets the help text of the argument.
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        ///     Gets or sets the long name of the argument.
        /// </summary>
        public string LongName { get; set; }

        /// <summary>
        ///     Gets or sets the short name of the argument.
        /// </summary>
        public char ShortName { get; set; }
    }

    /// <summary>
    ///     Provides static methods used to retrieve the command line arguments and operands with which the application was
    ///     started, as well as a Type to contain them.
    /// </summary>
    public class Arguments
    {
        /// <summary>
        ///     The regular expression with which to parse the command line string.
        /// </summary>
        private const string ArgumentRegEx = "(?:[-]{1,2}|\\/)([^=: ]+)[=: ]?(\\/?\\w\\S*|\\\"[^\"]*\\\"|\\\'[^']*\\\')?|([^ ([^'\\\"]+|\"[^\\\"]+\"|\\\'[^']+\\\')";

        /// <summary>
        ///     The regular expression with which to parse argument-value groups.
        /// </summary>
        private const string GroupRegEx = "^-[^-]+";

        /// <summary>
        ///     The regular expression with which to parse strings strictly containing operands.
        /// </summary>
        private const string OperandRegEx = "([^ ([^'\\\"]+|\\\"[^\\\"]+\\\"|\\\'[^']+\\\')";

        /// <summary>
        ///     The regular expression with which to split the command line string explicitly among argument/value pairs and
        ///     operands, and strictly operands.
        /// </summary>
        /// <remarks>
        ///     This regular expression effectively splits a string into two parts; the part before the first "--", and the part
        ///     after. Instances of "--" not surrounded by a word boundary and those enclosed in quotes are ignored.
        /// </remarks>
        private const string StrictOperandSplitRegEx = "(.*?[^\\\"\\\'])?(\\B-{2}\\B)[^\\\"\\\']?(.*)";

        /// <summary>
        ///     Initializes a new instance of the <see cref="Arguments"/> class with the specified argument dictionary and operand list.
        /// </summary>
        /// <param name="commandLineString">The command line string from which the arguments were parsed.</param>
        /// <param name="argumentDictionary">
        ///     The dictionary containing the arguments and values specified in the command line arguments with which the
        ///     application was started.
        /// </param>
        /// <param name="operandList">
        ///     The list containing the operands specified in the command line arguments with which the application was started.
        /// </param>
        private Arguments(string commandLineString, Dictionary<string, object> argumentDictionary, List<string> operandList)
        {
            CommandLineString = commandLineString;
            ArgumentDictionary = argumentDictionary;
            OperandList = operandList;
        }

        /// <summary>
        ///     Gets a dictionary containing the arguments and values specified in the command line arguments with which the
        ///     application was started.
        /// </summary>
        public Dictionary<string, object> ArgumentDictionary { get; private set; }

        /// <summary>
        ///     Gets the command line string from which the arguments were parsed.
        /// </summary>
        public string CommandLineString { get; private set; }

        /// <summary>
        ///     Gets a list containing the operands specified in the command line arguments with which the application was started.
        /// </summary>
        public List<string> OperandList { get; private set; }

        /// <summary>
        ///     Gets the argument value corresponding to the specified key from the <see cref="ArgumentDictionary"/> property.
        /// </summary>
        /// <param name="index">The key for which the value is to be retrieved.</param>
        /// <returns>The argument value corresponding to the specified key.</returns>
        public object this[string index]
        {
            get
            {
                return ArgumentDictionary[index];
            }
        }

        /// <summary>
        ///     Retrieves a collection of <see cref="ArgumentHelp"/> gathered from properties in the target <paramref name="type"/>
        ///     marked with the <see cref="ArgumentAttribute"/><see cref="Attribute"/> along with the short and long names and help text.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the matching properties are to be retrieived.</param>
        /// <returns>The retrieved collection of <see cref="ArgumentHelp"/>.</returns>
        public static IEnumerable<ArgumentHelp> GetArgumentHelp(Type type = null)
        {
            type = type ?? new StackFrame(1).GetMethod().DeclaringType;
            var retVal = new List<ArgumentHelp>();

            foreach (PropertyInfo property in GetArgumentProperties(type).Values.Distinct())
            {
                CustomAttributeData attribute = property.CustomAttributes.Where(a => a.AttributeType.Name == typeof(ArgumentAttribute).Name).FirstOrDefault();

                if (attribute != default(CustomAttributeData))
                {
                    retVal.Add(new ArgumentHelp()
                    {
                        ShortName = (char)attribute.ConstructorArguments[0].Value,
                        LongName = (string)attribute.ConstructorArguments[1].Value,
                        HelpText = (string)attribute.ConstructorArguments[2].Value,
                    });
                }
            }

            return retVal;
        }

        /// <summary>
        ///     Returns a dictionary containing the values specified in the command line arguments with which the application was
        ///     started, keyed by argument name.
        /// </summary>
        /// <param name="commandLineString">The command line arguments with which the application was started.</param>
        /// <returns>
        ///     The dictionary containing the arguments and values specified in the command line arguments with which the
        ///     application was started.
        /// </returns>
        public static Arguments Parse(string commandLineString = default(string))
        {
            commandLineString = commandLineString == default(string) || commandLineString == string.Empty ? Environment.CommandLine : commandLineString;

            Dictionary<string, object> argumentDictionary;
            List<string> operandList;

            // use the strict operand regular expression to test for/extract the two halves of the string, if the operator is used.
            MatchCollection matches = Regex.Matches(commandLineString, StrictOperandSplitRegEx);

            // if there is a match, the string contains the strict operand delimiter. parse the first and second matches accordingly.
            if (matches.Count > 0)
            {
                // the first group of the first match will contain everything in the string prior to the strict operand delimiter,
                // so extract the argument key/value pairs and list of operands from that string.
                argumentDictionary = GetArgumentDictionary(matches[0].Groups[1].Value);
                operandList = GetOperandList(matches[0].Groups[1].Value);

                // the first group of the second match will contain everything in the string after the strict operand delimiter, so
                // extract the operands from that string using the strict method.
                if (matches[0].Groups[3].Value != string.Empty)
                {
                    List<string> operandListStrict = GetOperandListStrict(matches[0].Groups[3].Value);

                    // join the operand lists.
                    operandList.AddRange(operandListStrict);
                }
            }
            else
            {
                argumentDictionary = GetArgumentDictionary(commandLineString);
                operandList = GetOperandList(commandLineString);
            }

            return new Arguments(commandLineString, argumentDictionary, operandList);
        }

        /// <summary>
        ///     Populates the properties in the invoking class marked with the
        ///     <see cref="ArgumentAttribute"/><see cref="Attribute"/> with the values specified in the list of command line
        ///     arguments, if present.
        /// </summary>
        /// <param name="commandLineString">The command line arguments with which the application was started.</param>
        /// <param name="clearExistingValues">Whether to clear the properties before populating them. Defaults to true.</param>
        /// <param name="caller">Internal parameter used to identify the calling method.</param>
        public static void Populate(string commandLineString = default(string), bool clearExistingValues = true, [CallerMemberName] string caller = default(string))
        {
            var callingMethod = new StackTrace().GetFrames()
                .Select(f => f.GetMethod())
                .Where(m => m.Name == caller).FirstOrDefault();

            if (callingMethod == default(MethodBase))
            {
                throw new InvalidOperationException("Error populating arguments; Unable to determine the containing type of Main().  Use Populate(typeof(<class containing main>))");
            }

            Populate(callingMethod.DeclaringType, Parse(commandLineString), clearExistingValues);
        }

        /// <summary>
        ///     Populates the properties in the specified Type marked with the
        ///     <see cref="ArgumentAttribute"/><see cref="Attribute"/> with the values specified in the list of command line
        ///     arguments, if present.
        /// </summary>
        /// <param name="type">
        ///     The Type for which the static properties matching the list of command line arguments are to be populated.
        /// </param>
        /// <param name="commandLineString">The command line arguments with which the application was started.</param>
        /// <param name="clearExistingValues">Whether to clear the properties before populating them. Defaults to true.</param>
        public static void Populate(Type type, string commandLineString = default(string), bool clearExistingValues = true)
        {
            Populate(type, Parse(commandLineString), clearExistingValues);
        }

        /// <summary>
        ///     Populates the properties in the specified Type marked with the
        ///     <see cref="ArgumentAttribute"/><see cref="Attribute"/> with the values specified in the specified argument
        ///     dictionary, if present. All property values are set to null at the start of the routine.
        /// </summary>
        /// <param name="type">
        ///     The Type for which the static properties matching the list of command line arguments are to be populated.
        /// </param>
        /// <param name="arguments">
        ///     The Arguments object containing the dictionary containing the argument-value pairs with which the destination
        ///     properties should be populated and the list of operands.
        /// </param>
        /// <param name="clearExistingValues">Whether to clear the properties before populating them. Defaults to true.</param>
        public static void Populate(Type type, Arguments arguments, bool clearExistingValues = true)
        {
            // fetch any properties in the specified type marked with the ArgumentAttribute attribute and clear them
            Dictionary<string, PropertyInfo> properties = GetArgumentProperties(type);

            if (clearExistingValues)
            {
                ClearProperties(properties);
            }

            foreach (string propertyName in properties.Keys)
            {
                // if the argument dictionary contains a matching argument
                if (arguments.ArgumentDictionary.ContainsKey(propertyName))
                {
                    // retrieve the property and type
                    PropertyInfo property = properties[propertyName];
                    Type propertyType = property.PropertyType;

                    // retrieve the value from the argument dictionary
                    object value = arguments.ArgumentDictionary[propertyName];

                    bool valueIsList = value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(List<>);

                    object convertedValue;

                    // if the type of the property is bool and the argument value is empty set the property value to true,
                    // indicating the argument is present
                    if (propertyType == typeof(bool))
                    {
                        convertedValue = true;

                        // if a value is specified, a bool flag was followed by an operand and the parser interpreted this as key
                        // value pair because it wasn't aware the flag was backed by a bool. remove the argument from the original
                        // string and re-parse operands from it to preserve order.
                        if (value.ToString() != string.Empty)
                        {
                            var arg = Regex.Matches(arguments.CommandLineString, "(?:[-]{1,2}|\\/)" + propertyName)[0].Value;
                            arguments.OperandList = GetOperandList(arguments.CommandLineString.Replace(arg, string.Empty));
                        }
                    }
                    else if (propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>)))
                    {
                        // if the property is an array or list, convert the value to an array or list of the matching type. start
                        // by converting atomic values to a list containing a single value, just to simplify processing.
                        if (valueIsList)
                        {
                            convertedValue = value;
                        }
                        else
                        {
                            convertedValue = new List<object>(new object[] { value });
                        }

                        // next, create a list with the same type as the target property
                        Type valueType;

                        if (propertyType.IsArray)
                        {
                            valueType = propertyType.GetElementType();
                        }
                        else
                        {
                            valueType = propertyType.GetGenericArguments()[0];
                        }

                        // create a list to store converted values
                        Type valueListType = typeof(List<>).MakeGenericType(valueType);
                        var valueList = (IList)Activator.CreateInstance(valueListType);

                        // populate the list
                        foreach (object v in (List<object>)convertedValue)
                        {
                            valueList.Add(ChangeType(v, propertyName, valueType));
                        }

                        // if the target property is an array, create one and populate it from the list this is surprisingly
                        // difficult here because we created the source list with the Activator and ToArray() won't work easily.
                        if (propertyType.IsArray)
                        {
                            var valueArray = Array.CreateInstance(propertyType.GetElementType(), valueList.Count);

                            for (int i = 0; i < valueArray.Length; i++)
                            {
                                valueArray.SetValue(valueList[i], i);
                            }

                            convertedValue = valueArray;
                        }
                        else
                        {
                            convertedValue = valueList;
                        }
                    }
                    else
                    {
                        // if the target property Type is an atomic (non-array or list) Type, convert the value and populate it,
                        // but not if the value is an array or list.
                        if (valueIsList)
                        {
                            throw new InvalidCastException($"Multiple values were specified for argument '{propertyName}', however it is not backed by an array or List<T>.  Specify only one value.");
                        }

                        convertedValue = ChangeType(value, propertyName, propertyType);
                    }

                    // set the target properties' value to the converted value from the argument string
                    property.SetValue(null, convertedValue);
                }
            }

            PropertyInfo operandsProperty = GetOperandsProperty(type);

            // check to ensure the target class has a property marked with the Operands attribute; if not GetOperandsProperty()
            // will return null.
            if (operandsProperty != default(PropertyInfo))
            {
                if (operandsProperty.PropertyType.IsAssignableFrom(typeof(List<string>)))
                {
                    operandsProperty.SetValue(null, arguments.OperandList);
                }
                else
                {
                    operandsProperty.SetValue(null, arguments.OperandList.ToArray());
                }
            }
        }

        /// <summary>
        ///     Converts the specified value for the specified argument to the specified Type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="argument">The argument for which the value is being converted.</param>
        /// <param name="toType">The Type to which the value is being converted.</param>
        /// <returns>The converted value.</returns>
        private static object ChangeType(object value, string argument, Type toType)
        {
            try
            {
                if (toType.IsEnum)
                {
                    return Enum.Parse(toType, (string)value, true);
                }

                return Convert.ChangeType(value, toType);
            }
            catch (Exception ex)
            {
                // if the cast fails, throw an exception
                string message = $"Specified value '{value}' for argument '{argument}' (expected type: {toType}).  ";
                message += "See inner exception for details.";

                throw new ArgumentException(message, ex);
            }
        }

        /// <summary>
        ///     Sets the value of each property in the specified dictionary to null.
        /// </summary>
        /// <param name="properties">The dictionary containing the properties to clear.</param>
        private static void ClearProperties(Dictionary<string, PropertyInfo> properties)
        {
            foreach (string key in properties.Keys)
            {
                properties[key].SetValue(null, null);
            }
        }

        /// <summary>
        ///     Populates and returns a dictionary containing the values specified in the command line arguments with which the
        ///     application was started, keyed by argument name.
        /// </summary>
        /// <param name="commandLineString">The command line arguments with which the application was started.</param>
        /// <returns>
        ///     The dictionary containing the arguments and values specified in the command line arguments with which the
        ///     application was started.
        /// </returns>
        private static Dictionary<string, object> GetArgumentDictionary(string commandLineString)
        {
            Dictionary<string, object> argumentDictionary = new Dictionary<string, object>();

            foreach (Match match in Regex.Matches(commandLineString, ArgumentRegEx))
            {
                // the first match of the regular expression used to parse the string will contain the argument name, if one was matched.
                if (match.Groups[1].Value == default(string) || match.Groups[1].Value == string.Empty)
                {
                    continue;
                }

                string fullMatch = match.Groups[0].Value;
                string argument = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                value = TrimOuterQuotes(value);

                // check to see if the argument uses a single dash. if so, split the argument name into a char array and add each
                // to the dictionary. if a value is specified, it belongs to the final character.
                if (Regex.IsMatch(fullMatch, GroupRegEx))
                {
                    char[] charArray = argument.ToCharArray();

                    // iterate over the characters backwards to more easily assign the value
                    for (int i = 0; i < charArray.Length; i++)
                    {
                        argumentDictionary.ExclusiveAdd(charArray[i].ToString(), i == charArray.Length - 1 ? value : string.Empty);
                    }
                }
                else
                {
                    // add the argument and value to the dictionary if it doesn't already exist.
                    argumentDictionary.ExclusiveAdd(argument, value);
                }
            }

            return argumentDictionary;
        }

        /// <summary>
        ///     Retrieves a dictionary containing properties in the target <see cref="Type"/> marked with the
        ///     <see cref="ArgumentAttribute"/><see cref="Attribute"/>, keyed on the string specified in the 'Name' field of the <see cref="Attribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the matching properties are to be retrieved.</param>
        /// <returns>
        ///     A dictionary containing matching properties, keyed on the 'Name' field of the
        ///     <see cref="ArgumentAttribute"/><see cref="Attribute"/> used to mark the property.
        /// </returns>
        private static Dictionary<string, PropertyInfo> GetArgumentProperties(Type type)
        {
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                // attempt to fetch the ArgumentAttribute of the property
                CustomAttributeData attribute = property.CustomAttributes.Where(a => a.AttributeType.Name == typeof(ArgumentAttribute).Name).FirstOrDefault();

                // if found, extract the Name property and add it to the dictionary
                if (attribute != default(CustomAttributeData))
                {
                    char shortName = (char)attribute.ConstructorArguments[0].Value;
                    string longName = (string)attribute.ConstructorArguments[1].Value;

                    if (!properties.ContainsKey(shortName.ToString()) && !properties.ContainsKey(longName))
                    {
                        properties.Add(shortName.ToString(), property);
                        properties.Add(longName, property);
                    }
                }
            }

            return properties;
        }

        /// <summary>
        ///     Populates and returns a list containing the operands specified in the command line arguments with which the
        ///     application was started.
        /// </summary>
        /// <param name="commandLineString">The command line arguments with which the application was started.</param>
        /// <returns>
        ///     A list containing the operands specified in the command line arguments with which the application was started.
        /// </returns>
        private static List<string> GetOperandList(string commandLineString)
        {
            List<string> operands = new List<string>();

            foreach (Match match in Regex.Matches(commandLineString, ArgumentRegEx))
            {
                // the 3rd match of the regular expression used to parse the string will contain the operand, if one was matched.
                if (match.Groups[3].Value == default(string) || match.Groups[3].Value == string.Empty)
                {
                    continue;
                }

                string fullMatch = match.Groups[0].Value;
                string operand = match.Groups[3].Value;

                operands.Add(TrimOuterQuotes(operand));
            }

            return operands;
        }

        /// <summary>
        ///     Populates and returns a list containing the operands within the specified string grouped by whole words and groups
        ///     of words contained within single or double quotes, treating strings that would otherwise be treated as argument
        ///     keys as operands.
        /// </summary>
        /// <param name="operandListString">The string from which the list of operands is to be parsed.</param>
        /// <returns>
        ///     A list containing the operands within the specified string grouped by whole words and groups of words contained
        ///     within single or double quotes, treating strings that would otherwise be treated as argument keys as operands.
        /// </returns>
        private static List<string> GetOperandListStrict(string operandListString)
        {
            List<string> operands = new List<string>();

            foreach (Match match in Regex.Matches(operandListString, OperandRegEx))
            {
                operands.Add(match.Groups[0].Value);
            }

            return operands;
        }

        /// <summary>
        ///     Retrieves the property in the target <see cref="Type"/> marked with the
        ///     <see cref="OperandsAttribute"/><see cref="Attribute"/>, if one exists.
        /// </summary>
        /// <remarks>The target property <see cref="Type"/> of the designated property must be of type string[] or List{string}.</remarks>
        /// <param name="type">The Type for which the matching property is to be retrieved.</param>
        /// <returns>The matching property, if one exists.</returns>
        /// <exception cref="InvalidCastException">
        ///     Thrown when the Type of the retrieved property is not string[] or List{string}.
        /// </exception>
        private static PropertyInfo GetOperandsProperty(Type type)
        {
            PropertyInfo property = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CustomAttributes
                    .Any(a => a.AttributeType.Name == typeof(OperandsAttribute).Name))
                        .FirstOrDefault();

            if (property != default(PropertyInfo) && property.PropertyType != typeof(string[]) && property.PropertyType != typeof(List<string>))
            {
                throw new InvalidCastException("The target for the Operands attribute must be of string[] or List<string>.");
            }

            return property;
        }

        /// <summary>
        ///     Returns the specified string with outer single or double quotes trimmed, if the string starts and ends with them.
        /// </summary>
        /// <param name="value">The string from which to trim outer single or double quotes.</param>
        /// <returns>The string with outer single or double quotes trimmed.</returns>
        private static string TrimOuterQuotes(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Trim('"');
            }
            else if (value.StartsWith("'") && value.EndsWith("'"))
            {
                value = value.Trim('\'');
            }

            return value;
        }
    }

    /// <summary>
    ///     Encapsulates argument names and help text.
    /// </summary>
    public class ArgumentHelp
    {
        /// <summary>
        ///     Gets or sets the help text for the argument.
        /// </summary>
        public string HelpText { get; set; }

        /// <summary>
        ///     Gets or sets the long name of the argument.
        /// </summary>
        public string LongName { get; set; }

        /// <summary>
        ///     Gets or sets the short name of the argument.
        /// </summary>
        public char ShortName { get; set; }
    }

    /// <summary>
    ///     Indicates that the property is to be used as the target for automatic population of command line operands when invoking
    ///     the <see cref="Arguments.Populate(string, bool, string)"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OperandsAttribute : Attribute
    {
    }
}