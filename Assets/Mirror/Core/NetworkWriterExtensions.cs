using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Mirror
{
    // Mirror's Weaver automatically detects all NetworkWriter function types,
    // but they do all need to be extensions.
    public static class NetworkWriterExtensions
    {
        public static void WriteByte(this NetworkWriter writer, byte value) => writer.WriteBlittable(value);
        public static void WriteByteNullable(this NetworkWriter writer, byte? value) => writer.WriteBlittableNullable(value);

        public static void WriteSByte(this NetworkWriter writer, sbyte value) => writer.WriteBlittable(value);
        public static void WriteSByteNullable(this NetworkWriter writer, sbyte? value) => writer.WriteBlittableNullable(value);

        // char is not blittable. convert to ushort.
        public static void WriteChar(this NetworkWriter writer, char value) => writer.WriteBlittable((ushort)value);
        public static void WriteCharNullable(this NetworkWriter writer, char? value) => writer.WriteBlittableNullable((ushort?)value);

        // bool is not blittable. convert to byte.
        public static void WriteBool(this NetworkWriter writer, bool value) => writer.WriteBlittable((byte)(value ? 1 : 0));
        public static void WriteBoolNullable(this NetworkWriter writer, bool? value) => writer.WriteBlittableNullable(value.HasValue ? ((byte)(value.Value ? 1 : 0)) : new byte?());

        public static void WriteShort(this NetworkWriter writer, short value) => writer.WriteBlittable(value);
        public static void WriteShortNullable(this NetworkWriter writer, short? value) => writer.WriteBlittableNullable(value);

        public static void WriteUShort(this NetworkWriter writer, ushort value) => writer.WriteBlittable(value);
        public static void WriteUShortNullable(this NetworkWriter writer, ushort? value) => writer.WriteBlittableNullable(value);

        public static void WriteInt(this NetworkWriter writer, int value) => writer.WriteBlittable(value);
        public static void WriteIntNullable(this NetworkWriter writer, int? value) => writer.WriteBlittableNullable(value);

        public static void WriteUInt(this NetworkWriter writer, uint value) => writer.WriteBlittable(value);
        public static void WriteUIntNullable(this NetworkWriter writer, uint? value) => writer.WriteBlittableNullable(value);

        public static void WriteLong(this NetworkWriter writer, long value)  => writer.WriteBlittable(value);
        public static void WriteLongNullable(this NetworkWriter writer, long? value) => writer.WriteBlittableNullable(value);

        public static void WriteULong(this NetworkWriter writer, ulong value) => writer.WriteBlittable(value);
        public static void WriteULongNullable(this NetworkWriter writer, ulong? value) => writer.WriteBlittableNullable(value);

        public static void WriteFloat(this NetworkWriter writer, float value) => writer.WriteBlittable(value);
        public static void WriteFloatNullable(this NetworkWriter writer, float? value) => writer.WriteBlittableNullable(value);

        [StructLayout(LayoutKind.Explicit)]
        internal struct UIntDouble
        {
            [FieldOffset(0)]
            public double doubleValue;

            [FieldOffset(0)]
            public ulong longValue;
        }

        public static void WriteDouble(this NetworkWriter writer, double value)
        {
            // DEBUG: try to find the exact value that fails.
            //UIntDouble convert = new UIntDouble{doubleValue = value};
            //Debug.Log($"=> NetworkWriter.WriteDouble: {value} => 0x{convert.longValue:X8}");


            writer.WriteBlittable(value);
        }
        public static void WriteDoubleNullable(this NetworkWriter writer, double? value) => writer.WriteBlittableNullable(value);

        public static void WriteDecimal(this NetworkWriter writer, decimal value) => writer.WriteBlittable(value);
        public static void WriteDecimalNullable(this NetworkWriter writer, decimal? value) => writer.WriteBlittableNullable(value);

        public static void WriteString(this NetworkWriter writer, string value)
        {
            // write 0 for null support, increment real size by 1
            // (note: original HLAPI would write "" for null strings, but if a
            //        string is null on the server then it should also be null
            //        on the client)
            if (value == null)
            {
                writer.WriteUShort(0);
                return;
            }

            // WriteString copies into the buffer manually.
            // need to ensure capacity here first, manually.
            int maxSize = writer.encoding.GetMaxByteCount(value.Length);
            writer.EnsureCapacity(writer.Position + 2 + maxSize); // 2 bytes position + N bytes encoding

            // encode it into the buffer first.
            // reserve 2 bytes for header after we know how much was written.
            int written = writer.encoding.GetBytes(value, 0, value.Length, writer.buffer, writer.Position + 2);

            // check if within max size
            if (written >= NetworkWriter.MaxStringLength)
                throw new IndexOutOfRangeException($"NetworkWriter.Write(string) too long: {written}. Limit: {NetworkWriter.MaxStringLength}");

            // .Position is unchanged, so fill in the size header now.
            // we already ensured that max size fits into ushort.max-1.
            writer.WriteUShort(checked((ushort)(written + 1))); // Position += 2

            // now update position by what was written above
            writer.Position += written;
        }

        public static void WriteBytesAndSizeSegment(this NetworkWriter writer, ArraySegment<byte> buffer)
        {
            writer.WriteBytesAndSize(buffer.Array, buffer.Offset, buffer.Count);
        }

        // Weaver needs a write function with just one byte[] parameter
        // (we don't name it .Write(byte[]) because it's really a WriteBytesAndSize since we write size / null info too)

        public static void WriteBytesAndSize(this NetworkWriter writer, byte[] buffer)
        {
            // buffer might be null, so we can't use .Length in that case
            writer.WriteBytesAndSize(buffer, 0, buffer != null ? buffer.Length : 0);
        }

        // for byte arrays with dynamic size, where the reader doesn't know how many will come
        // (like an inventory with different items etc.)

        public static void WriteBytesAndSize(this NetworkWriter writer, byte[] buffer, int offset, int count)
        {
            // null is supported because [SyncVar]s might be structs with null byte[] arrays
            // write 0 for null array, increment normal size by 1 to save bandwidth
            // (using size=-1 for null would limit max size to 32kb instead of 64kb)
            if (buffer == null)
            {
                writer.WriteUInt(0u);
                return;
            }
            writer.WriteUInt(checked((uint)count) + 1u);
            writer.WriteBytes(buffer, offset, count);
        }

        public static void WriteArraySegment<T>(this NetworkWriter writer, ArraySegment<T> segment)
        {
            int length = segment.Count;
            writer.WriteInt(length);
            for (int i = 0; i < length; i++)
            {
                writer.Write(segment.Array[segment.Offset + i]);
            }
        }

        public static void WriteVector2(this NetworkWriter writer, Vector2 value) => writer.WriteBlittable(value);
        public static void WriteVector2Nullable(this NetworkWriter writer, Vector2? value) => writer.WriteBlittableNullable(value);

        public static void WriteVector3(this NetworkWriter writer, Vector3 value) => writer.WriteBlittable(value);
        public static void WriteVector3Nullable(this NetworkWriter writer, Vector3? value) => writer.WriteBlittableNullable(value);

        public static void WriteVector4(this NetworkWriter writer, Vector4 value) => writer.WriteBlittable(value);
        public static void WriteVector4Nullable(this NetworkWriter writer, Vector4? value) => writer.WriteBlittableNullable(value);

        public static void WriteVector2Int(this NetworkWriter writer, Vector2Int value) => writer.WriteBlittable(value);
        public static void WriteVector2IntNullable(this NetworkWriter writer, Vector2Int? value) => writer.WriteBlittableNullable(value);

        public static void WriteVector3Int(this NetworkWriter writer, Vector3Int value) => writer.WriteBlittable(value);
        public static void WriteVector3IntNullable(this NetworkWriter writer, Vector3Int? value) => writer.WriteBlittableNullable(value);

        public static void WriteColor(this NetworkWriter writer, Color value) => writer.WriteBlittable(value);
        public static void WriteColorNullable(this NetworkWriter writer, Color? value) => writer.WriteBlittableNullable(value);

        public static void WriteColor32(this NetworkWriter writer, Color32 value) => writer.WriteBlittable(value);
        public static void WriteColor32Nullable(this NetworkWriter writer, Color32? value) => writer.WriteBlittableNullable(value);

        public static void WriteQuaternion(this NetworkWriter writer, Quaternion value) => writer.WriteBlittable(value);
        public static void WriteQuaternionNullable(this NetworkWriter writer, Quaternion? value) => writer.WriteBlittableNullable(value);

        public static void WriteRect(this NetworkWriter writer, Rect value) => writer.WriteBlittable(value);
        public static void WriteRectNullable(this NetworkWriter writer, Rect? value) => writer.WriteBlittableNullable(value);

        public static void WritePlane(this NetworkWriter writer, Plane value) => writer.WriteBlittable(value);
        public static void WritePlaneNullable(this NetworkWriter writer, Plane? value) => writer.WriteBlittableNullable(value);

        public static void WriteRay(this NetworkWriter writer, Ray value) => writer.WriteBlittable(value);
        public static void WriteRayNullable(this NetworkWriter writer, Ray? value) => writer.WriteBlittableNullable(value);

        public static void WriteMatrix4x4(this NetworkWriter writer, Matrix4x4 value) => writer.WriteBlittable(value);
        public static void WriteMatrix4x4Nullable(this NetworkWriter writer, Matrix4x4? value) => writer.WriteBlittableNullable(value);

        public static void WriteGuid(this NetworkWriter writer, Guid value)
        {
#if !UNITY_2021_3_OR_NEWER
            // Unity 2019 doesn't have Span yet
            byte[] data = value.ToByteArray();
            writer.WriteBytes(data, 0, data.Length);
#else
            // WriteBlittable(Guid) isn't safe. see WriteBlittable comments.
            // Guid is Sequential, but we can't guarantee packing.
            // TryWriteBytes is safe and allocation free.
            writer.EnsureCapacity(writer.Position + 16);
            value.TryWriteBytes(new Span<byte>(writer.buffer, writer.Position, 16));
            writer.Position += 16;
#endif
        }
        public static void WriteGuidNullable(this NetworkWriter writer, Guid? value)
        {
            writer.WriteBool(value.HasValue);
            if (value.HasValue)
                writer.WriteGuid(value.Value);
        }

        public static void WriteNetworkIdentity(this NetworkWriter writer, NetworkIdentity value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            // users might try to use unspawned / prefab GameObjects in
            // rpcs/cmds/syncvars/messages. they would be null on the other
            // end, and it might not be obvious why. let's make it obvious.
            // https://github.com/vis2k/Mirror/issues/2060
            //
            // => warning (instead of exception) because we also use a warning
            //    if a GameObject doesn't have a NetworkIdentity component etc.
            if (value.netId == 0)
                Debug.LogWarning($"Attempted to serialize unspawned GameObject: {value.name}. Prefabs and unspawned GameObjects would always be null on the other side. Please spawn it before using it in [SyncVar]s/Rpcs/Cmds/NetworkMessages etc.");

            writer.WriteUInt(value.netId);
        }

        public static void WriteNetworkBehaviour(this NetworkWriter writer, NetworkBehaviour value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            writer.WriteUInt(value.netId);
            writer.WriteByte(value.ComponentIndex);
        }

        public static void WriteTransform(this NetworkWriter writer, Transform value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }
            if (value.TryGetComponent(out NetworkIdentity identity))
            {
                writer.WriteUInt(identity.netId);
            }
            else
            {
                Debug.LogWarning($"NetworkWriter {value} has no NetworkIdentity");
                writer.WriteUInt(0);
            }
        }

        public static void WriteGameObject(this NetworkWriter writer, GameObject value)
        {
            if (value == null)
            {
                writer.WriteUInt(0);
                return;
            }

            // warn if the GameObject doesn't have a NetworkIdentity,
            if (!value.TryGetComponent(out NetworkIdentity identity))
                Debug.LogWarning($"NetworkWriter {value} has no NetworkIdentity");

            // serialize the correct amount of data in any case to make sure
            // that the other end can read the expected amount of data too.
            writer.WriteNetworkIdentity(identity);
        }

        // while SyncList<T> is recommended for NetworkBehaviours,
        // structs may have .List<T> members which weaver needs to be able to
        // fully serialize for NetworkMessages etc.
        // note that Weaver/Writers/GenerateWriter() handles this manually.
        public static void WriteList<T>(this NetworkWriter writer, List<T> list)
        {
            if (list is null)
            {
                writer.WriteInt(-1);
                return;
            }
            writer.WriteInt(list.Count);
            for (int i = 0; i < list.Count; i++)
                writer.Write(list[i]);
        }

        // while SyncSet<T> is recommended for NetworkBehaviours,
        // structs may have .Set<T> members which weaver needs to be able to
        // fully serialize for NetworkMessages etc.
        // note that Weaver/Writers/GenerateWriter() handles this manually.
        // TODO writer not found. need to adjust weaver first. see tests.
        /*
        public static void WriteHashSet<T>(this NetworkWriter writer, HashSet<T> hashSet)
        {
            if (hashSet is null)
            {
                writer.WriteInt(-1);
                return;
            }
            writer.WriteInt(hashSet.Count);
            foreach (T item in hashSet)
                writer.Write(item);
        }
        */

        public static void WriteArray<T>(this NetworkWriter writer, T[] array)
        {
            if (array is null)
            {
                writer.WriteInt(-1);
                return;
            }
            writer.WriteInt(array.Length);
            for (int i = 0; i < array.Length; i++)
                writer.Write(array[i]);
        }

        public static void WriteUri(this NetworkWriter writer, Uri uri)
        {
            writer.WriteString(uri?.ToString());
        }

        public static void WriteTexture2D(this NetworkWriter writer, Texture2D texture2D)
        {
            // TODO allocation protection when sending textures to server.
            //      currently can allocate 32k x 32k x 4 byte = 3.8 GB

            // support 'null' textures for [SyncVar]s etc.
            // https://github.com/vis2k/Mirror/issues/3144
            // simply send -1 for width.
            if (texture2D == null)
            {
                writer.WriteShort(-1);
                return;
            }

            // write dimensions first so reader can create the texture with size
            // 32k x 32k short is more than enough
            writer.WriteShort((short)texture2D.width);
            writer.WriteShort((short)texture2D.height);
            writer.WriteArray(texture2D.GetPixels32());
        }

        public static void WriteSprite(this NetworkWriter writer, Sprite sprite)
        {
            // support 'null' textures for [SyncVar]s etc.
            // https://github.com/vis2k/Mirror/issues/3144
            // simply send a 'null' for texture content.
            if (sprite == null)
            {
                writer.WriteTexture2D(null);
                return;
            }

            writer.WriteTexture2D(sprite.texture);
            writer.WriteRect(sprite.rect);
            writer.WriteVector2(sprite.pivot);
        }
    }
}
