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
    public static class JsonPrimitives
    {
        /// <summary>
        /// Lookup table for hex values.
        /// </summary>
        public const string ContentType = "application/json";

        /// <summary>
        /// Deserializes the specified Json string into an object whose type matches
        /// what was discovered in the PropertyTable.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static object Deserialize(string json)
        {
            object result = null;

            Hashtable table = JsonParser.JsonDecode(json) as Hashtable;
            DumpObjects(table, 0);

            return result;
        }

        #region Serialize Methods

        /// <summary>
        /// Convert a value to JSON.
        /// </summary>
        /// <param name="o">The value to convert. Supported types are: Boolean, String, Byte, (U)Int16, (U)Int32, Float, Double, Decimal, JsonObject, JsonArray, Array, Object and null.</param>
        /// <returns>The JSON object as a string or null when the value type is not supported.</returns>
        /// <remarks>For objects, only public fields are converted.</remarks>
        public static string Serialize(object o)
        {
            if (o == null)
                return "null";

            Type type = o.GetType();

            // All ordinary value types and all objects that are classes that can
            // and shouold be ToString()'d are handled here.  Special objects like
            // arrays and classes that have properties to be enumerated are handled below.
            switch (type.Name)
            {
                case "Boolean":
                    {
                        return (bool)o ? "true" : "false";
                    }
                case "String":
                    {
                        // Encapsulate object in double-quotes if it's not already
                        char v = o.ToString()[0];

                        if (v == '"')
                            return o.ToString();

                        return "\"" + o + "\"";
                    }
                case "Single":
                case "Double":
                case "Decimal":
                case "Float":
                    {
                        return Converter.ToString((double)o);
                    }
                case "Byte":
                case "SByte":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                case "JsonObject":
                case "JsonArray":
                    {
                        return o.ToString();
                    }
                case "Char":
                case "Guid":
                    {
                        return "\"" + o + "\"";
                    }
                case "DateTime":
                    {
                        // This MSDN page describes the problem with JSON dates:
                        // http://msdn.microsoft.com/en-us/library/bb299886.aspx
                        return "\"" + Converter.ToIso8601((DateTime)o) + "\"";
                    }

            }

            if (type.IsArray)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (object i in (Array)o)
                {
                    // if the array object needs to be serialized first, do it
                    SerializeStatus serialize = GetSerializeState(i);
                    object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(i) : i;
                    jsonArray.Add(valueToAdd);
                }

                return jsonArray.ToString();
            }

            if (type == typeof(ArrayList))
            {
                JsonArray jsonArray = new JsonArray();
                ArrayList arrayList = o as ArrayList;

                if (arrayList != null)
                {
                    foreach (object i in arrayList)
                    {
                        // if the array object needs to be serialized first, do it
                        SerializeStatus serialize = GetSerializeState(i);
                        object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(i) : i;
                        jsonArray.Add(valueToAdd);
                    }
                }

                return jsonArray.ToString();
            }

            if (type == typeof(Hashtable))
            {
                Hashtable table = o as Hashtable;
                JsonObject to = new JsonObject();

                if (table != null)
                {
                    foreach (object key in table.Keys)
                    {
                        // If the array object needs to be serialized first, do it
                        SerializeStatus serialize = GetSerializeState(table[key]);
                        object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(table[key]) : table[key];

                        to.Add(key, valueToAdd);
                    }
                }

                return to.ToString();
            }

            if (type == typeof(DictionaryEntry))
            {
                DictionaryEntry dict = o as DictionaryEntry;
                JsonObject to = new JsonObject();

                // If the Value property of the DictionaryEntry is an object rather
                // than a string, then serialize it first.
                if (dict != null)
                {
                    SerializeStatus serialize = GetSerializeState(dict.Value);
                    object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(dict.Value) : dict.Value;

                    to.Add(dict.Key, valueToAdd);
                }

                return to.ToString();
            }

            if (type.IsClass)
            {
                JsonObject jsonObject = new JsonObject();

                // Iterate through all of the methods, looking for GET properties
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    // We care only about property getters when serializing
                    if (method.Name.StartsWith("get_"))
                    {
                        // Ignore abstract and virtual objects
                        if ((method.IsAbstract || (method.IsVirtual) || (method.ReturnType.IsAbstract)))
                            continue;

                        // Ignore delegates and MethodInfos
                        if ((method.ReturnType == typeof(Delegate)) || (method.ReturnType == typeof(MulticastDelegate)) || (method.ReturnType == typeof(MethodInfo)))
                            continue;

                        // Ditto for DeclaringType
                        if ((method.DeclaringType == typeof(Delegate)) || (method.DeclaringType == typeof(MulticastDelegate)))
                            continue;

                        // If the property returns a Hashtable
                        if (method.ReturnType == typeof(Hashtable))
                        {
                            Hashtable table = method.Invoke(o, null) as Hashtable;
                            JsonObject to = new JsonObject();

                            if (table != null)
                            {
                                foreach (object key in table.Keys)
                                {
                                    // If the array object needs to be serialized first, do it
                                    SerializeStatus serialize = GetSerializeState(table[key]);
                                    object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(table[key]) : table[key];
                                    to.Add(key, valueToAdd);
                                    //to.Add(key, table[key]);
                                }
                            }

                            jsonObject.Add(method.Name.Substring(4), to.ToString());

                            continue;
                        }

                        // If the property returns an array of objects
                        if (method.ReturnType == typeof(ArrayList))
                        {
                            ArrayList no = method.Invoke(o, null) as ArrayList;
                            JsonArray jsonArray = new JsonArray();

                            if (no != null)
                            {
                                foreach (object i in no)
                                {
                                    // If the array object needs to be serialized first, do it
                                    SerializeStatus serialize = GetSerializeState(i);
                                    object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(i) : i;
                                    jsonArray.Add(valueToAdd);
                                }
                            }

                            jsonObject.Add(method.Name.Substring(4), jsonArray.ToString());

                            continue;
                        }

                        // If the property returns a DictionaryEntry
                        if (method.ReturnType == typeof(DictionaryEntry))
                        {
                            DictionaryEntry dict = method.Invoke(o, null) as DictionaryEntry;

                            // If the Value property of the DictionaryEntry needs to be serialized first, do it
                            if (dict != null)
                            {
                                SerializeStatus serialize = GetSerializeState(dict.Value);
                                object valueToAdd = serialize == SerializeStatus.Serialize ? Serialize(dict.Value) : dict.Value;

                                // Wrap the DictionaryEntry in a JsonObject
                                JsonObject to = new JsonObject {{dict.Key, valueToAdd}};
                                jsonObject.Add(method.Name.Substring(4), to.ToString());
                            }

                            continue;
                        }

                        // If the property is a Class that should NOT be ToString()'d, because
                        // it has properties that must themselves be enumerated and serialized,
                        // then recursively call myself to serialize them.
                        if ((method.ReturnType.IsClass) &&
                            (method.ReturnType.IsArray == false) &&
                            (method.ReturnType.ToString().StartsWith("System.Collections") == false) &&
                            (method.ReturnType.ToString().StartsWith("System.String") == false))
                        {
                            object no = method.Invoke(o, null);
                            string value = Serialize(no);
                            jsonObject.Add(method.Name.Substring(4), value);
                            continue;
                        }

                        // All other properties are types that will be handled according to 
                        // their type.  That handler code is the switch statement at the top
                        // of this function.
                        object newo = method.Invoke(o, null);
                        jsonObject.Add(method.Name.Substring(4), newo);


                    }
                }
                return jsonObject.ToString();
            }

            return null;
        }

        public enum SerializeStatus
        {
            None = 0,
            Serialize = 1
        }

        /// <summary>
        /// Determines if the specified object needs to be serialized.  It needs to be serialized if it's a 
        /// class that contains properties that need enumeration.  All other objects that can be directly
        /// returned, such as ints, strings, etc, do not need to be serialized.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private static SerializeStatus GetSerializeState(object o)
        {
            Type type = o.GetType();

            // Ignore delegates and MethodInfos
            if ((type == typeof(Delegate)) ||
                (type == typeof(MulticastDelegate)) ||
                (type == typeof(MethodInfo)))
            {
                return SerializeStatus.None;
            }

            // If the property returns a Hashtable
            if (type == typeof(Hashtable))
            {
                return SerializeStatus.None;
            }

            // If the property returns an array of objects
            if (type == typeof(ArrayList))
            {
                return SerializeStatus.Serialize;
            }

            // If the property returns a DictionaryEntry
            if (type == typeof(DictionaryEntry))
            {
                return SerializeStatus.Serialize;
            }

            // If the property is a Class that should NOT be ToString()'d, because
            // it has properties that must themselves be enumerated and serialized,
            // then recursively call myself to serialize them.
            if ((type.IsClass) &&
                (type.IsArray == false) &&
                (type.ToString().StartsWith("System.Collections") == false) &&
                (type.ToString().StartsWith("System.String") == false))
            {
                return SerializeStatus.Serialize;
            }

            // All other properties are types that will be handled according to 
            // their type.  That handler code is the switch statement at the top
            // of this function.
            return SerializeStatus.None;
        }

        #endregion

        #region Deserialize Methods

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

        #endregion
    }
}
