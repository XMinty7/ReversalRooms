using Semver;
using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;

namespace ReversalRooms.Engine.Utils
{
    /// <summary>
    /// Contains utility extension methods
    /// </summary>
    public static class Extensions
    {
        private static Deserializer Deserializer = new Deserializer();
        private static Serializer Serializer = new Serializer();

        /// <summary>
        /// Reads a stream from start to end
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        /// <returns>The bytes read from the stream</returns>
        public static byte[] ReadAll(this Stream stream, bool fromStart = false)
        {
            if (fromStart) stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Reads a stream from start to end and then decodes it into a string
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="encoding">The encoding to use, UTF-8 by default</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        /// <returns>The string read from the stream</returns>
        public static string ReadAllString(this Stream stream, bool fromStart = false, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            return encoding.GetString(stream.ReadAll(fromStart));
        }

        /// <summary>
        /// Writes a bytes array onto a stream
        /// </summary>
        /// <param name="stream">The stream to write onto</param>
        /// <param name="bytes">The bytes to write</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        public static void WriteAll(this Stream stream, byte[] bytes, bool fromStart = false)
        {
            if (fromStart) stream.Seek(0, SeekOrigin.Begin);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Writes a string onto a stream
        /// </summary>
        /// <param name="stream">The stream to write onto</param>
        /// <param name="str">The string to write</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        /// <param name="encoding">The encoding to use, UTF-8 by default</param>
        public static void WriteAll(this Stream stream, string str, bool fromStart = false, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            WriteAll(stream, encoding.GetBytes(str), fromStart);
        }

        /// <summary>
        /// Reads the stream as a string and deserializes the result as YAML into the specified type
        /// </summary>
        /// <typeparam name="T">The deserialization type</typeparam>
        /// <param name="stream">The stream to read</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        /// <param name="encoding">The encoding to use, UTF-8 by default</param>
        /// <returns>The result of the deserialization</returns>
        public static T DeserializeYaml<T>(this Stream stream, bool fromStart = false, Encoding encoding = null)
        {
            return Deserializer.Deserialize<T>(stream.ReadAllString(fromStart, encoding));
        }

        /// <summary>
        /// Serializes a value into a YAML string and writes it onto the stream
        /// </summary>
        /// <typeparam name="T">The deserialization type</typeparam>
        /// <param name="stream">The stream to read</param>
        /// <param name="value">The value to serialize</param>
        /// <param name="fromStart">Whether to reset to starting position or not</param>
        /// <param name="encoding">The encoding to use, UTF-8 by default</param>
        /// <returns>The result of the deserialization</returns>
        public static void SerializeYaml<T>(this Stream stream, T value, bool fromStart = false, Encoding encoding = null)
        {
            stream.WriteAll(Serializer.Serialize(value), fromStart, encoding);
        }

        /// <summary>
        /// Checks if the version meets a minimum
        /// </summary>
        /// <param name="version">The version to check</param>
        /// <param name="requirement">The minimum required version</param>
        /// <returns>True if the version meets the minimum; otherwise false</returns>
        public static bool MeetsVersion(this SemVersion version, SemVersion requirement)
        {
            return version.ComparePrecedenceTo(requirement) >= 0;
        }

        /// <summary>
        /// Checks if the version outdates another
        /// </summary>
        /// <param name="version">The version to check for</param>
        /// <param name="requirement">The version to check against</param>
        /// <returns>True if the version outdates the other; otherwise false</returns>
        public static bool Outdates(this SemVersion version, SemVersion other)
        {
            return version.ComparePrecedenceTo(other) > 0;
        }
    }
}
