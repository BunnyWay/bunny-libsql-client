using System.Reflection;

namespace Bunny.LibSql.Client.Types;

public class F32Blob : ILibSqlType
{
    public float[] values { get; set; }

    public F32Blob(float[] values)
    {
        this.values = values;
    }

    public float VectorDistanceCos(F32Blob other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (values.Length != other.values.Length)
            throw new ArgumentException("Vectors must be the same length", nameof(other));

        float dot = 0f;
        float magA = 0f;
        float magB = 0f;

        for (int i = 0; i < values.Length; i++)
        {
            float a = values[i];
            float b = other.values[i];
            dot += a * b;
            magA += a * a;
            magB += b * b;
        }

        // avoid division by zero
        if (magA == 0f || magB == 0f)
            throw new InvalidOperationException("Cannot compute cosine distance for zero‐magnitude vector");

        float cosine = dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
        return 1f - cosine;
    }

    public F32Blob(byte[] bytes)
    {
        if (bytes.Length % sizeof(float) != 0)
        {
            throw new ArgumentException("Byte array length must be a multiple of 4 (size of float).");
        }

        int count = bytes.Length / sizeof(float);
        values = new float[count];
        Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
    }
    
    public static int GetSize(PropertyInfo property)
    {
        // Check if the property is of type F32Blob
        if (property.PropertyType != typeof(F32Blob))
        {
            return sizeof(float) * 4;
        }
        
        // Get BlobSizeAttribute from the property
        var blobSizeAttribute = property.GetCustomAttribute<BlobSizeAttribute>();
        if (blobSizeAttribute != null)
        {
            // Return the size specified in the BlobSizeAttribute
            return blobSizeAttribute.Size;
        }
        
        
        return sizeof(float) * 4; // 4 floats, each 4 bytes
    }

    public object? GetLibSqlJsonValue()
    {
        if (values == null)
            return null;
        
        // Convert the float array to a byte array
        byte[] bytes = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}