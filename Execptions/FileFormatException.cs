namespace Docx2Pdf.Execptions;

public class FileFormatException : Exception
{
    public FileFormatException() : base("Converter accepts only *.docx files")
    {
    }
}