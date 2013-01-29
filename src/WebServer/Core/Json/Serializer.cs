// Based on Mike Jones's JSON Serialization and Deserialization library
// http://www.tinyclr.com/codeshare/entry/357

using System.Collections;
using Netduino.WebServer.Core.Enums;

namespace Netduino.WebServer.Core.Json
{
    /// <summary>
    /// .NET Micro Framework JSON Serializer and Deserializer.
    /// Mimics, as closely as possible, the excellent JSON (de)serializer at http://www.json.org.
    /// You can (de)serialize just about any object that contains real property values:
    /// Value Types (int, bool, string, etc), Classes, Arrays, Dictionaries, Hashtables, etc.
    /// Caveats:
    ///   1) Each property to be (de)serialized must be public, and contain BOTH a property getter and setter.
    ///   2) You can't (de)serialize interfaces, virtual or abstract properties, private properties.
    ///      Your class can contain these objects, but their values are not (de)serialized.
    ///   3) DateTime objects can be (de)serialized, and their format in JSON will be ISO 8601 format.
    ///   4) Guids can be (de)serialized.
    ///   3) You can't use Array or IList because they are abstract (or an interface).  Use ArrayList instead.
    ///   4) You can't use IDictionaryEntry, use DictionaryEntry instead.
    ///   5) .NET MF floating point seems to have very little precision, at least on my GHI USBizi hardware.
    ///      I get only about 3 or 4 decimal places of accuracy.
    /// 
    /// How does this class work, given the extremely limited Reflection capabilities?
    /// Serialization is easy: you take the specified object, enumerate its Methods (yes, Methods, there
    /// is no Property enumeration), and find the getters and setters, filtering out the unusable items.
    /// Then you just JSON-format the Property names and their values.
    /// 
    /// Deserialization is much more difficult, as JSON contains no type definitions, and .NET MF contains
    /// next-to-zero assistance.  We instantiate a PropertyTable class to enumerate every public class
    /// that's loaded by this process, and create a non-heirarchical Hashtable list of all public classes,
    /// their property names, and each Property's Type.  When the JSON string is presented for deserialization,
    /// the JSON string is parsed (using this class) into a heirarchical Hashtable, containing all of the properties
    /// and their values.  At that point, you have (a) a Hastable containing Property Names with their Types,
    /// and (b) a Hastable containing the Property Names and their actual Values.  Deserialization at that point
    /// is a matter of matching the Properties in the two lists, instantiating them by their Type, and setting
    /// their Values.  Hoping of course that you contain no classes with identical property names.
    /// 
    /// This is more difficult than it sounds.  In normal .NET, you have boxing/unboxing to convert any object
    /// to any defined Type.  In .NET MF, you do not.  The only Type that an object will unbox into is the
    /// type exposed by the ReturnType property of the Parse method on the object.  So a LOT of wrangling is
    /// needed to properly unbox types from JSON into C#.  But it (mostly) works.
    /// </summary>
    public class Serializer
    {
        private readonly PropertyTable _propertyTable;

        public Serializer()
        {
            DateTimeFormat = DateTimeFormat.Iso8601;
            _propertyTable = new PropertyTable();
        }

        /// <summary>
        /// Gets/Sets the format that will be used to display and parse dates in the Json data.
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; }

        /// <summary>
        /// Serializes an object into a Json string.
        /// </summary>
        public string Serialize(object o)
        {
            return JsonPrimitives.Serialize(o);
        }

        /// <summary>
        /// Desrializes a Json string into an object.
        /// </summary>
        public object Deserialize(string json)
        {
            Hashtable table = JsonParser.JsonDecode(json) as Hashtable;
            JsonPrimitives.DumpObjects(table, 0);

            return _propertyTable.FindObject(table);
        }

        /// <summary>
        /// Resets the contents of the Property Table with current classes and property definitions.
        /// </summary>
        /// <remarks>This does NOT need to be called when values change, only when new classes are loaded dynamically at runtime.</remarks>
        public void Snapshot()
        {
            _propertyTable.Snapshot();
        }
    }
}
