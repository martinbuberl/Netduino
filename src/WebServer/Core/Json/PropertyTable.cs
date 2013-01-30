// Based on Mike Jones's JSON Serialization and Deserialization library
// http://www.tinyclr.com/codeshare/entry/357

using System;
using System.Collections;
using System.Reflection;
using Netduino.WebServer.Core.Abstraction;
using Netduino.WebServer.Core.Extensions;
using Netduino.WebServer.Core.Utilities;

namespace Netduino.WebServer.Core.Json
{
    /// <summary>
    /// Contains a snapshot of all of the classes currently loaded by the Assembly
    /// and enumerates their class names, properties and property types into a Hashtable.
    /// The table is then referenced against a decoded Json string to determine what
    /// data types a given chunk of Json string represents.  When a match is found,
    /// that class is instantiated and its properties populated with the Json data.
    /// </summary>
    public class PropertyTable
    {
        private ArrayList _properties;

        public PropertyTable()
        {
            Snapshot();
        }

        public ArrayList Properties
        {
            get { return _properties; }
        }

        /// <summary>
        /// Takes a snapshot of the classes and their property names and types
        /// </summary>
        public void Snapshot()
        {
            if (_properties != null)
            {
                foreach (Hashtable h in _properties)
                {
                    h.Clear();
                }
                _properties = null;
            }

            // Read in all Assemblies except for the .NET MF DLLs
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _properties = new ArrayList();
            foreach (Assembly a in assemblies)
            {
                if ((a.FullName.StartsWith("System")) || (a.FullName.StartsWith("mscorlib")) || (a.FullName.StartsWith("Microsoft.SPOT")))
                    continue;

                DebugWrapper.Print("Adding " + a.FullName + " to the list");
                Hashtable hash = GetProperties(a.GetTypes());
                DumpObjects(hash, 0);
                _properties.Add(hash);
            }

            //_properties = DumpObjects(_assembly.GetTypes());

        }

        /// <summary>
        /// Searches the Hashtable for an object that contains a property name specified by key.
        /// The key may be composed of colon-delimited object names representing a class heirarchy.
        /// </summary>
        public object FindObject(Hashtable jsonTable)
        {
            // Find the Hashtable that matches the specified Hashtable
            DictionaryEntry classDefinition = FindMatchingDictionary(jsonTable);

            // Now we have two hashtables:
            //  1) The jsonTable hashtable, which has all the property names and values
            //     but contains no class names.
            //  2) The class hashtable, which contains the actual class names
            //     and their property names but no property values.

            // Create an instance of the highest-level class
            Type theClassType = Type.GetType(classDefinition.Key.ToString());
            ConstructorInfo ctor = theClassType.GetConstructor(new Type[] {});
            object theClass = ctor.Invoke(null);

            // Iterate through all of its properties, setting its values
            foreach (DictionaryEntry e in classDefinition.Value as Hashtable)
            {
                SetProperty(theClass, theClassType, classDefinition.Value as Hashtable, e, jsonTable);
            }

            return theClass;
        }

        /// <summary>
        /// Searches the ArrayList of Hashtables generated at startup time, and returns
        /// the Hashtable that matches the specified Hashtable.
        /// </summary>
        /// <param name="jsonTable"></param>
        /// <returns></returns>
        private DictionaryEntry FindMatchingDictionary(Hashtable jsonTable)
        {
            DictionaryEntry tableFound = null;

            foreach (DictionaryEntry entry in jsonTable)
            {
                // Each "Value" in the DictionaryEntry should be a Class object, which is itself a Hashtable
                // They Key inside each Value is the actual Property name we're checking against
                foreach (Hashtable curTable in _properties)
                {
                    foreach (DictionaryEntry e in curTable)
                    {
                        Hashtable hashtable = e.Value as Hashtable;

                        if (hashtable != null)
                        {
                            foreach (DictionaryEntry p in hashtable)
                            {
                                DebugWrapper.Print("Comparing " + entry.Key + " to " + p.Key + " (" + e.Key + ")");

                                if (entry.Key.ToString() == p.Key.ToString())
                                {
                                    // We could easily have matches across multiple Classes, because classes can often
                                    // contain properties with the same names (e.g. "Name", "Id", etc).  However, the last
                                    // match should always contain the Class whose properties MOST matched the table,
                                    // so last guy wins.
                                    tableFound = new DictionaryEntry(e.Key.ToString(), e.Value);
                                }
                            }
                        }
                    }
                }
            }

            return tableFound;
        }

        /// <summary>
        /// Sets a single Property on an object.  That single Property can be a Value Type, and ArrayList, Hashtable,
        /// essentially anything that's legal to deserialize.  This method will recurse into itself to set all of
        /// this Property's values, children objects and values, etc.
        /// </summary>
        /// <param name="parent">The owner of this Property</param>
        /// <param name="parentType">The Type of the owner object</param>
        /// <param name="classDefinition">The Hashtable that contains this Property's (heirarchical) information</param>
        /// <param name="entry">The current Property Name and Type</param>
        /// <param name="jsonTable">The entire JSON string, converted to a Hasthable</param>
        private void SetProperty(object parent, Type parentType, Hashtable classDefinition, DictionaryEntry entry,Hashtable jsonTable)
        {
            string name = entry.Key.ToString(); // name of property
            string type = entry.Value.ToString(); // Type of property, as a string

            object value = GetValueFromJsonHashtable(jsonTable, entry.Key.ToString());

            // If it's not a value type, recurse
            if (value is Hashtable)
            {
                if (type.Contains("DictionaryEntry"))
                {
                    foreach (DictionaryEntry d in value as Hashtable)
                    {
                        MethodInfo method = parentType.GetMethod("set_" + name);
                        method.Invoke(parent, new object[] {d});

                        break;
                    }
                }
                else if (type.Contains("Hashtable"))
                {
                }
                else
                {
                    // Find the top-level Hashtable entry for this property in properties and Json
                    DictionaryEntry newClassDefinition = FindMatchingDictionary(value as Hashtable);
                    //DumpObjects(newClassDefinition.Value as Hashtable, 8);

                    // Instantiate the class, whose Type is entry.Value
                    Type newParentType = Type.GetType(type);
                    ConstructorInfo ctor = newParentType.GetConstructor(new Type[] {});
                    object newParent = ctor.Invoke(null);

                    // Fill in all the properties for this class
                    foreach (DictionaryEntry e in newClassDefinition.Value as Hashtable)
                    {
                        SetProperty(newParent, newParentType, newClassDefinition.Value as Hashtable, e, value as Hashtable);
                    }

                    // Assign the newly-instanced class to the parent's property of the class
                    MethodInfo method = parentType.GetMethod("set_" + name);
                    method.Invoke(parent, new[] {newParent});
                }
            }
            else if (type == "System.Collections.ArrayList")
            {
                MethodInfo method = parentType.GetMethod("set_" + name);
                method.Invoke(parent, new[] {value});
            }
            else if (type == "System.DateTime")
            {
                // Determine if DATE format is the ASP.NET Ajax form, or not.
                // "Not" means it assumes the de factor ISO 8601 standard format.
                DateTime dt = value.ToString().Contains("Date(") ? Converter.FromAspNetAjax(value.ToString()) : Converter.FromIso8601(value.ToString());

                MethodInfo method = parentType.GetMethod("set_" + name);
                method.Invoke(parent, new object[] {dt});
            }
            else if (type == "System.Guid")
            {
                Guid g = Converter.ToGuid(value.ToString());
                MethodInfo method = parentType.GetMethod("set_" + name);
                method.Invoke(parent, new object[] {g});
            }
            else if (type == "System.Boolean")
            {
                bool b = (bool) value;
                MethodInfo method = parentType.GetMethod("set_" + name);
                method.Invoke(parent, new object[] {b});
            }
            else
            {
                // We have a value type to set, so set it
                SetTypedValue(parent, parentType, name, type, value);
            }
        }

        /// <summary>
        /// Performs the type conversion that is normally standard stuff for .NET, but missing in .NET MF.
        /// Basically, the unTypedValue object cannot be converted or cast using boxing or a simple cast,
        /// except for the actual Type defined inside the object (the object's Parse method ReturnType).
        /// So this function discovers what the unTypedValue's inner Type really is, compares that with
        /// the destination Property's real Type, and performs a cast specific to those two types.
        /// </summary>
        /// <param name="parent">The Property's parent object</param>
        /// <param name="parentType">Type of the parent object</param>
        /// <param name="name">Property Name</param>
        /// <param name="type">Property's real type</param>
        /// <param name="unTypedValue">The object containing the Property's value, extracted from a JSON string</param>
        private void SetTypedValue(object parent, Type parentType, string name, string type, object unTypedValue)
        {
            // Find out what Type the object was set to, using the Parse Method's ReturnType
            MethodInfo parse = unTypedValue.GetType().GetMethod("Parse");
            Type typeToUnbox = typeof (Int64);

            // The JSON parser assigned the type to one of three possible Types:
            //   1) Double
            //   2) UInt64
            //   3) Int64
            // From these three values, any form of casting is possible into the Value Types.
            // Exceptions are specifically handled by the destionation Type.
            UInt64 ui64Value = 0;
            Int64 i64Value = 0;
            Double dblValue = 0;

            if (parse != null)
            {
                if (parse.ReturnType == typeof (UInt64))
                {
                    ui64Value = (UInt64) unTypedValue;
                    typeToUnbox = typeof (UInt64);
                }
                else if (parse.ReturnType == typeof (Int64))
                {
                    i64Value = (Int64) unTypedValue;
                    typeToUnbox = typeof (Int64);
                }
                else if (parse.ReturnType == typeof (Double))
                {
                    dblValue = (Double) unTypedValue;
                    typeToUnbox = typeof (Double);
                }
            }

            // Perform the actual conversion, based on the real destination Type
            // and the value's inner object Type.
            MethodInfo method = parentType.GetMethod("set_" + name);

            switch (type)
            {
                case "System.SByte":
                    {
                        SByte value = (SByte) (typeToUnbox == typeof (UInt64) ? (SByte) ui64Value : (typeToUnbox == typeof (Int64) ? (SByte) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Byte":
                    {
                        Byte value = (Byte) (typeToUnbox == typeof (UInt64) ? (Byte) ui64Value : (typeToUnbox == typeof (Int64) ? (Byte) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Int16":
                    {
                        Int16 value = (Int16) (typeToUnbox == typeof (UInt64) ? (Int16) ui64Value : (typeToUnbox == typeof (Int64) ? (Int16) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.UInt16":
                    {
                        UInt16 value = (UInt16) (typeToUnbox == typeof (UInt64) ? (UInt16) ui64Value : (typeToUnbox == typeof (Int64) ? (UInt16) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Int32":
                    {
                        Int32 value = (Int32) (typeToUnbox == typeof (UInt64) ? (Int32) ui64Value : (typeToUnbox == typeof (Int64) ? (Int32) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.UInt32":
                    {
                        UInt32 value = (UInt32) (typeToUnbox == typeof (UInt64) ? (UInt32) ui64Value : (typeToUnbox == typeof (Int64) ? (UInt32) i64Value : unTypedValue));
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Int64":
                    {
// ReSharper disable RedundantCast
                        Int64 value = (Int64) (typeToUnbox == typeof (UInt64) ? (Int64) ui64Value : (typeToUnbox == typeof (Int64) ? (Int64) i64Value : unTypedValue));
// ReSharper restore RedundantCast
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.UInt64":
                    {
// ReSharper disable RedundantCast
                        UInt64 value = (UInt64) (typeToUnbox == typeof (UInt64) ? (UInt64) ui64Value : (typeToUnbox == typeof (Int64) ? (UInt64) i64Value : unTypedValue));
// ReSharper restore RedundantCast
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Single":
                    {
                        Single value = (Single) unTypedValue;
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                case "System.Double":
                    {
                        Double value = (Double) unTypedValue;
                        method.Invoke(parent, new object[] {value});
                    }
                    break;
                default:
                    method.Invoke(parent, new[] {unTypedValue});
                    break;
            }
        }

        /// <summary>
        /// Returns the Value that matches the specified name.  The Value can be anything:
        /// a value type (integer, string, etc), or another class, or a Hasthtable,
        /// DictionaryEntry, or ArrayList.
        /// </summary>
        /// <param name="jsonTable"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static object GetValueFromJsonHashtable(Hashtable jsonTable, string name)
        {
            object value = null;

            foreach (DictionaryEntry entry in jsonTable)
            {
                if (entry.Key.ToString() == name)
                {
                    return entry.Value;
                }
            }

            return value;
        }

        /// <summary>
        /// Debugging/Diagnostic tool that dumps objects and their children.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="level"></param>
        public static void DumpObjects(Hashtable hash, int level)
        {
            foreach (DictionaryEntry d in hash)
            {
                string name = d.Key.ToString();
                string tabs = string.Empty;

                for (int i = 0; i < level; i++)
                {
                    tabs = tabs + " ";
                }

                DebugWrapper.Print(tabs + name + " : ");

                if (d.Value is Hashtable)
                    DumpObjects(d.Value as Hashtable, level + 4);

                if (d.Value is ArrayList)
                {
                    DumpObjectArray(d.Value as ArrayList, level + 4);
                }
                else
                {
                    DebugWrapper.Print(d.Value.ToString());
                }
            }

        }

        private static void DumpObjectArray(ArrayList array, int level)
        {
            foreach (object o in array)
            {
                if (o is Hashtable)
                {
                    DumpObjects(o as Hashtable, level + 4);
                }
                else if (o is ArrayList)
                {
                    DumpObjectArray(o as ArrayList, level + 4);
                }
                else
                {
                    DebugWrapper.Print(o.ToString());
                }
            }
        }


        /// <summary>
        /// High-level method that enumerates all Types in the loaded assembly,
        /// and places their class names and properties in a hashtable.  At deserialization
        /// time, this Hashtable (containing Property Names and Types) is compared to
        /// the Hashtable containing the JSON Property Names and Values, and the result
        /// of the two Hashtables is the desired instantiated object.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        private Hashtable GetProperties(Type[] types)
        {
            Hashtable h = new Hashtable();

            foreach (Type type in types)
            {
                Hashtable entry = GetProperty(type);
                if (entry != null)
                {
                    h.Add(type.FullName, entry);
                }
            }

            return h;
        }

        /*
         * The format of the object list is as follows:
         * Dictionary:
         * Class Name: Dictionary
         *		Property Name: property return type
         *		Property Name: property return type
         * Class Name:Dictionary
         *		Property Name: property return type
         *		Property Name: property return type
         *		Property Name: property return type
         *		Property Name: property return type
         *		Property Name: property return type
         *		Property Name: property return type
         * ....
         * 
         * Note that the returned object is NOT hierarchical, unlike the objects
         * that it represents.  This is because we do not need to know the
         * heirarchical relationships between objects, we only need to know what
         * properties a given object contains, which is the purpose of this table.
         * 
        */

        private Hashtable GetProperty(Type type)
        {

            //DebugWrapper.Print("----------------------------------------------");
            //// Type dump
            //DebugWrapper.Print("Name: " + t.Name);
            //DebugWrapper.Print("    IsClass: " + t.IsClass);
            //DebugWrapper.Print("    IsArray: " + t.IsArray);
            //DebugWrapper.Print("    IsEnum: " + t.IsEnum);
            //DebugWrapper.Print("    IsAbstract: " + t.IsAbstract);
            //DebugWrapper.Print("");

            // If it's a class, then it's something we care about
            if (type.IsClass)
            {
                Hashtable properties = new Hashtable();

                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    //DebugWrapper.Print("        Name: " + method.Name);
                    //DebugWrapper.Print("            IsVirtual: " + method.IsVirtual);
                    //DebugWrapper.Print("            IsStatic: " + method.IsStatic);
                    //DebugWrapper.Print("            IsPublic: " + method.IsPublic);
                    //DebugWrapper.Print("            IsFinal: " + method.IsFinal);
                    //DebugWrapper.Print("            IsAbstract: " + method.IsAbstract);
                    //DebugWrapper.Print("            MemberType: " + method.MemberType);
                    //DebugWrapper.Print("            DeclaringType: " + method.DeclaringType);
                    //DebugWrapper.Print("            ReturnType: " + method.ReturnType);

                    // If the Name.StartsWith "get_" and/or "set_",
                    // and it's not Abstract && not Virtual
                    // then it's a Property to save
                    if ((method.Name.StartsWith("get_")) &&
                        (method.IsAbstract == false) &&
                        (method.IsVirtual == false))
                    {
                        // Ignore abstract and virtual objects
                        if ((method.IsAbstract ||
                             (method.IsVirtual) ||
                             (method.ReturnType.IsAbstract)))
                        {
                            continue;
                        }

                        // Ignore delegates and MethodInfos
                        if ((method.ReturnType == typeof (Delegate)) ||
                            (method.ReturnType == typeof (MulticastDelegate)) ||
                            (method.ReturnType == typeof (MethodInfo)))
                        {
                            continue;
                        }

                        // Ditto for DeclaringType
                        if ((method.DeclaringType == typeof (Delegate)) ||
                            (method.DeclaringType == typeof (MulticastDelegate)))
                        {
                            continue;
                        }

                        // Don't need these types either
                        if ((method.Name.StartsWith("System.Globalization")))
                        {
                            continue;
                        }

                        // If the property returns a Hashtable
                        //if (method.ReturnType == typeof(System.Collections.Hashtable))
                        //{
                        //    return GetProperty(method.ReturnType);
                        //}

                        // If the property is another Class, return the Class name as the type,
                        // then we'll look elsewhere in the PropertyTable for that class
                        //if ((method.ReturnType.IsClass) &&
                        //    (method.ReturnType.IsArray == false) &&
                        //    (method.ReturnType.ToString().StartsWith("System.Collections") == false) &&
                        //    (method.ReturnType.ToString().StartsWith("System.String") == false))
                        //{
                        //    properties.Add(method.Name.Substring(4), method.Name.Substring(4));
                        //    continue;
                        //}

                        // If the property returns an array
                        //if (method.ReturnType == typeof(System.Collections.ArrayList))
                        //{
                        //    return DumpObject(method.ReturnType);
                        //}

                        //foreach (object s in properties.Keys)
                        //{
                        //    DebugWrapper.Print(s.ToString());
                        //}

                        DebugWrapper.Print("****************** " + method.ReturnType);
                        properties.Add(method.Name.Substring(4), method.ReturnType);
                    }
                }

                return properties;
            }

            return null;
        }
    }
}
