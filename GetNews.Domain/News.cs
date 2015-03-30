using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GetNews.Domain
{
    public class News
    {
        private bool _paragraphsFixed;

        public News()
        {
            Paragraphs = new List<string>();
        }

        public string Image { get; set; }
        public string ImageLink { get; set; }
        public string Title { get; set; }
        public List<string> Paragraphs { get; set; }
        public string Tag { get; set; }

        public DateTime Date
        {
            get
            {
                if (_paragraphsFixed == false)
                    throw new InvalidOperationException("Please invoke FixParagraphs!");

                DateTime date = DateTime.MinValue;

                string lastParagraph = Paragraphs[Paragraphs.Count - 1];

                string[] words = lastParagraph.Split(' ');

                if (words.Count() >= 3)
                {
                    string strDate = words[2];

                    DateTime.TryParse(strDate, out date);
                }

                return date;
            }
        }

        public override string ToString()
        {
            if (_paragraphsFixed == false)
                throw new InvalidOperationException("Please invoke FixParagraphs!");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Title);
            sb.AppendLine(ImageLink);
            sb.AppendLine(Tag);
            sb.AppendLine(Date.ToString());

            foreach (var paragraph in Paragraphs)
            {
                sb.AppendLine(paragraph);
            }

            return sb.ToString();
        }

        public void FixParagraphs()
        {
            List<Line> paragraphsFixed = new List<Line>();

            foreach (var text in Paragraphs)
            {
                var line = new Line(text);

                if (line.IsNew())
                {
                    paragraphsFixed.Add(line);
                }
                else
                {
                    var lastParagraph = paragraphsFixed.Last();

                    lastParagraph.Concat(line);
                }
            }

            if (ExistsParagraphPublishDate())
                FixParagraphPublishDate(ref paragraphsFixed);

            UpdateParagraphs(paragraphsFixed);

            _paragraphsFixed = true;
        }

        private bool ExistsParagraphPublishDate()
        {
            bool result = false;

            foreach (var item in Paragraphs)
            {
                if (item.StartsWith("Publicado"))
                    result = true;
            }

            return result;
        }

        private void UpdateParagraphs(List<Line> paragraphsFixed)
        {
            Paragraphs = paragraphsFixed.Select(x => x.Text).ToList();
        }

        private void FixParagraphPublishDate(ref List<Line> paragraphsFixed)
        {
            Line lastLine = paragraphsFixed[paragraphsFixed.Count -1];

            Line penultimateLine = paragraphsFixed[paragraphsFixed.Count - 2];

            penultimateLine.Concat(lastLine);

            paragraphsFixed.Remove(lastLine);           
        }
    }

   
}
