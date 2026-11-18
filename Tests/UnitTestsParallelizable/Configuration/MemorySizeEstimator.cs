#nullable enable

namespace ConfigurationTests;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public static class MemorySizeEstimator
{
    public static long EstimateSize<T> (T? source)
    {
        if (source is null)
        {
            return 0;
        }

        ConcurrentDictionary<object, long> visited = new (ReferenceEqualityComparer.Instance);
        return EstimateSizeInternal (source, visited);
    }

    private const int POINTER_SIZE = 8; // 64-bit system
    private const int OBJECT_HEADER_SIZE = 16; // 2 pointers for GC

    private static long EstimateSizeInternal (object? source, ConcurrentDictionary<object, long> visited)
    {
        if (source is null)
        {
            return 0;
        }

        // Handle already visited objects to avoid circular references
        if (visited.TryGetValue (source, out long existingSize))
        {
            // // Log revisited object (enable for debugging)
            // Console.WriteLine($"Revisited {source.GetType().FullName}: {existingSize} bytes");
            return existingSize;
        }

        Type type = source.GetType ();
        long size = 0;

        // Handle simple types
        if (IsSimpleType (type))
        {
            size = EstimateSimpleTypeSize (source, type);
            visited.TryAdd (source, size);
            // // Log simple type (enable for debugging)
            // Console.WriteLine($"{type.FullName}: {size} bytes");
            return size;
        }

        // Handle arrays
        if (type.IsArray)
        {
            size = EstimateArraySize (source, visited);
        }
        // Handle dictionaries
        else if (source is IDictionary)
        {
            size = EstimateDictionarySize (source, visited);
        }
        // Handle collections
        else if (typeof (ICollection).IsAssignableFrom (type))
        {
            size = EstimateCollectionSize (source, visited);
        }
        // Handle structs and classes
        else
        {
            size = EstimateObjectSize (source, type, visited);
        }

        visited.TryAdd (source, size);
        // // Log object size (enable for debugging)
        // if (size == 0)
        // {
        //     Console.WriteLine($"Zero size for {type.FullName}");
        // }
        // else
        // {
        //     Console.WriteLine($"{type.FullName}: {size} bytes");
        // }

        return size;
    }

    private static bool IsSimpleType (Type type)
    {
        if (type.IsPrimitive
            || type.IsEnum
            || type == typeof (decimal)
            || type == typeof (DateTime)
            || type == typeof (DateTimeOffset)
            || type == typeof (TimeSpan)
            || type == typeof (Guid)
            || type == typeof (Rune)
            || type == typeof (string))
        {
            return true;
        }

        // Treat structs with no writable public properties as simple types
        if (type.IsValueType)
        {
            PropertyInfo [] writableProperties = type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
                .Where (p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters ().Length == 0)
                .ToArray ();
            return writableProperties.Length == 0;
        }

        // Treat Property翰Info as simple (metadata, not cloned)
        if (typeof (PropertyInfo).IsAssignableFrom (type))
        {
            return true;
        }

        return false;
    }

    private static long EstimateSimpleTypeSize (object? source, Type type)
    {
        if (type == typeof (string))
        {
            string str = (string)source!;
            // Header + length (4) + char array ref + chars (2 bytes each)
            return OBJECT_HEADER_SIZE + 4 + POINTER_SIZE + (str.Length * 2);
        }

        try
        {
            return Marshal.SizeOf (type);
        }
        catch (ArgumentException)
        {
            // Fallback for enums or other simple types
            return 4; // Conservative estimate
        }
    }

    private static long EstimateArraySize (object? source, ConcurrentDictionary<object, long> visited)
    {
        Array array = (Array)source!;
        long size = OBJECT_HEADER_SIZE + 4 + POINTER_SIZE; // Header + length + padding

        foreach (object? element in array)
        {
            size += EstimateSizeInternal (element, visited);
        }

        return size;
    }

    private static long EstimateDictionarySize (object? source, ConcurrentDictionary<object, long> visited)
    {
        IDictionary dict = (IDictionary)source!;
        long size = OBJECT_HEADER_SIZE + (POINTER_SIZE * 5); // Header + buckets, entries, comparer, fields
        size += dict.Count * 4; // Bucket array (~4 bytes per entry)
        size += dict.Count * (4 + 4 + POINTER_SIZE * 2); // Entry array: hashcode, next, key, value

        foreach (object? key in dict.Keys)
        {
            size += EstimateSizeInternal (key, visited);
            size += EstimateSizeInternal (dict [key], visited);
        }

        return size;
    }

    private static long EstimateCollectionSize (object? source, ConcurrentDictionary<object, long> visited)
    {
        Type type = source!.GetType ();
        long size = OBJECT_HEADER_SIZE + (POINTER_SIZE * 3); // Header + internal array + fields

        if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (Dictionary<,>))
        {
            return EstimateDictionarySize (source, visited);
        }

        if (source is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                size += EstimateSizeInternal (item, visited);
            }
        }

        return size;
    }

    private static long EstimateObjectSize (object? source, Type type, ConcurrentDictionary<object, long> visited)
    {
        long size = OBJECT_HEADER_SIZE;

        // Size public writable properties
        foreach (PropertyInfo prop in type.GetProperties (BindingFlags.Instance | BindingFlags.Public)
            .Where (p => p is { CanRead: true, CanWrite: true } && p.GetIndexParameters ().Length == 0))
        {
            try
            {
                object? value = prop.GetValue (source);
                size += EstimateSizeInternal (value, visited);
            }
            catch (Exception)
            {
                // // Log exception (enable for debugging)
                // Console.WriteLine($"Error processing property {prop.Name} of {type.FullName}: {ex.Message}");
                // Continue to avoid crashing
            }
        }

        // For structs, also size fields (to handle generic structs)
        if (type.IsValueType)
        {
            FieldInfo [] fields = type.GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                try
                {
                    object? fieldValue = field.GetValue (source);
                    size += EstimateSizeInternal (fieldValue, visited);
                }
                catch (Exception)
                {
                    // // Log exception (enable for debugging)
                    // Console.WriteLine($"Error processing field {field.Name} of {type.FullName}: {ex.Message}");
                    // Continue to avoid crashing
                }
            }
        }

        return size;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new ();

        public new bool Equals (object? x, object? y)
        {
            return ReferenceEquals (x, y);
        }

        public int GetHashCode (object obj)
        {
            return RuntimeHelpers.GetHashCode (obj);
        }
    }
}