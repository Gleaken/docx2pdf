namespace Docx2Pdf.Execptions;

public class FileSizeException: Exception
{
    public FileSizeException(long fileSize, long limit) : base($"File size is {fileSize} and it exceeds maximum allowed size: {limit} bytes")
    {
    }
}