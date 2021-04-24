using Tesseract;

namespace Lunar.OCR.Models
{
    public class RecognisedText
    {
        public string Text { get; set; }

        public Rect Boundary { get; set; }
    }
}
