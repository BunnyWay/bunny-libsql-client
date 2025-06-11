namespace Bunny.LibSql.Client.Types;

public class BlobSizeAttribute : Attribute
{
    public BlobSizeAttribute(int size)
    {
        Size = size;
    }

    public int Size { get; }
}