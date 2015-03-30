using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace GetNews.Domain.Tests
{
    public class NewsTest
    {
        [Fact]
        public void DeveriaUnificarParagrafosQueForamSeparadosPorLink()
        {
            var news = new NewsTestBuilder()
                .WithParagraph("As inscrições para o evento são realizadas no site")                
                .WithParagraph("http://www.serrastartec.com.br")
                .WithParagraph("até esta quarta-feira, dia 02 de outubro. A programação detalhada, com todos os palestrantes, informações sobre o Serra Startec, patrocinadores também são encontradas no site. O evento acontece nos dias 03 e 04, a partir das 18 horas e no sábado dia 05 a partir das 8 horas, no salão de Atos da UNPLAC, no bairro Universitário em Lages. Já a programação de domingo, dia 06, acontece no Adventure Park.")             
                .Build();

            news.FixParagraphs();

            news.Paragraphs.Should().HaveCount(1);
        }

        [Fact]
        public void DeveriaUnificarParagrafosDePublicacaoQueForamSeparadosPorLink()
        {
            var news = new NewsTestBuilder()
                .WithParagraph("Publicado em 30/09/2013 por")
                .WithParagraph("Assessoria de Comunicação Social.")
                .Build();

            news.FixParagraphs();

            news.Paragraphs.Should().HaveCount(1);
        }    
    }
}
