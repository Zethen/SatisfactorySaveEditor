﻿using SatisfactorySaveParser.PropertyTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SatisfactorySaveParser
{
    public class SerializedFields : List<SerializedProperty>
    {
        public byte[] TrailingData { get; private set; }

        public void Serialize(BinaryWriter writer)
        {
            foreach (var field in this)
            {
                field.Serialize(writer);
            }

            writer.WriteLengthPrefixedString("None");

            writer.Write(0);
            if (TrailingData != null)
                writer.Write(TrailingData);
        }

        public static SerializedFields Parse(int length, BinaryReader reader)
        {
            var start = reader.BaseStream.Position;
            var result = new SerializedFields();

            while (true)
            {
                var propertyName = reader.ReadLengthPrefixedString();
                if (propertyName == "None")
                {
                    break;
                }

                var fieldType = reader.ReadLengthPrefixedString();
                var size = reader.ReadInt32();

                var unk = reader.ReadInt32();
                Trace.Assert(unk == 0);

                int overhead;
                var before = reader.BaseStream.Position;
                switch (fieldType)
                {
                    case ArrayProperty.TypeName:
                        result.Add(ArrayProperty.Parse(propertyName, reader, size, out overhead));
                        break;
                    case FloatProperty.TypeName:
                        overhead = 1;
                        result.Add(FloatProperty.Parse(propertyName, reader));
                        break;
                    case IntProperty.TypeName:
                        overhead = 1;
                        result.Add(IntProperty.Parse(propertyName, reader));
                        break;
                    case ByteProperty.TypeName:
                        result.Add(ByteProperty.Parse(propertyName, reader, out overhead));
                        break;
                    case EnumProperty.TypeName:
                        result.Add(EnumProperty.Parse(propertyName, reader, out overhead));
                        break;
                    case BoolProperty.TypeName:
                        overhead = 2;
                        result.Add(BoolProperty.Parse(propertyName, reader));
                        break;
                    case StrProperty.TypeName:
                        overhead = 1;
                        result.Add(StrProperty.Parse(propertyName, reader));
                        break;
                    case NameProperty.TypeName:
                        overhead = 1;
                        result.Add(NameProperty.Parse(propertyName, reader));
                        break;
                    case ObjectProperty.TypeName:
                        overhead = 1;
                        result.Add(ObjectProperty.Parse(propertyName, reader));
                        break;
                    case StructProperty.TypeName:
                        result.Add(StructProperty.Parse(propertyName, reader, size, out overhead));
                        break;
                    case MapProperty.TypeName:
                        result.Add(MapProperty.Parse(propertyName, reader, size, out overhead));
                        break;
                    case TextProperty.TypeName:
                        overhead = 1;
                        result.Add(TextProperty.Parse(propertyName, reader));
                        break;
                    default:
                        throw new NotImplementedException(fieldType);
                }
                var after = reader.BaseStream.Position;

                if (before + size + overhead != after)
                {
                    throw new InvalidOperationException($"Expected {size} bytes read but got {after - before - overhead}");
                }
            }

            var int1 = reader.ReadInt32();
            Trace.Assert(int1 == 0);

            var remainingBytes = start + length - reader.BaseStream.Position;
            if (remainingBytes > 0)
            {
                result.TrailingData = reader.ReadBytes((int)remainingBytes);
            }

            //if (remainingBytes == 4)
            ////if(result.Fields.Count > 0)
            //{
            //    var int2 = reader.ReadInt32();
            //}
            //else if (remainingBytes > 0 && result.Any(f => f is ArrayProperty && ((ArrayProperty)f).Type == StructProperty.TypeName))
            //{
            //    var unk = reader.ReadBytes((int)remainingBytes);
            //}
            //else if (remainingBytes > 4)
            //{
            //    var int2 = reader.ReadInt32();
            //    var str2 = reader.ReadLengthPrefixedString();
            //    var str3 = reader.ReadLengthPrefixedString();
            //}


            return result;
        }
    }
}