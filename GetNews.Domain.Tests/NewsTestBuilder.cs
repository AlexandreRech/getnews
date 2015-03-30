using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetNews.Domain.Tests
{
    public class NewsTestBuilder
    {
        private News _news;

        public NewsTestBuilder()
        {
            _news = new News();
        }

        public NewsTestBuilder WithParagraph(string p)
        {
            _news.Paragraphs.Add(p);

            return this;
        }

        public News Build()
        {
            return _news;
        }
    }
}
