using Blogs = Telligent.Evolution.Blogs.Components;
using Dicussions = Telligent.Evolution.Discussions.Components;
using MediaGallery = Telligent.Evolution.MediaGalleries.Components;
using Wikis = Telligent.Evolution.Wikis.Components;

namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataDefaultData
    {
        public static MicroDataEntry[] Entries = new[]
                {
                    //Bogs
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/BlogPosting"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".blog-details .content-author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".blog-details .content-date" , Type = MicroDataType.itemprop , Value = "datePublished"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".blog-post .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".blog-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                   
                    //Comments
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = Blogs.ContentTypes.BlogPost, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Wikis.ContentTypes.WikiPage, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Wikis.ContentTypes.WikiPage, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = Wikis.ContentTypes.WikiPage, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},

                    //Users
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author, .author" , Type = MicroDataType.itemscope , Value = "http://schema.org/Person"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.rel , Value = "author"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .avatar, .author .avatar img" , Type = MicroDataType.itemprop , Value = "image"},
                    
                    //FOrums
                    new MicroDataEntry(){ContentType = Dicussions.ContentTypes.ForumThread, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/Article"},
                    new MicroDataEntry(){ContentType = Dicussions.ContentTypes.ForumThread, Selector = ".thread .full" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Dicussions.ContentTypes.ForumThread, Selector = ".thread .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Dicussions.ContentTypes.ForumThread, Selector = ".thread .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = Dicussions.ContentTypes.ForumThread, Selector = ".thread .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                    
                    
                    //Media
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/MediaObject"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".media-gallery-post .full .name a" , Type = MicroDataType.itemprop , Value = "contentUrl"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".media-gallery-post  .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".media-gallery-post .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = MediaGallery.ContentTypes.Media, Selector = ".media-gallery-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                };

    }
}
