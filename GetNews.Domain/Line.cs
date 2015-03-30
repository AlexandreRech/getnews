namespace GetNews.Domain
{
    public class Line
    {
        private string _text;

        public Line(string text)
        {
            _text = text;
        }

        public string Text { get { return _text; } }

        public bool IsNew()
        {
            char firstLetter = _text.Substring(0, 1).ToCharArray()[0];

            return char.IsUpper(firstLetter);
        }

        public void Concat(Line line)
        {
            if (_text.EndsWith(" "))
                _text = _text + line;
            else
                _text = _text + " " + line.Text;
        }
    }
}
