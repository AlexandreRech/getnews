using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GetNews.WebApi.Models
{
    public class Post
    {
        public int Id { get; set; }

        public string Description { get; set; }

        public DateTime PublishDate { get; set; }

    }
}