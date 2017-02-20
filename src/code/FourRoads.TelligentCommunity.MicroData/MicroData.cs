using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Version1;


namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataDefaultData
    {
        public static MicroDataEntry[] Entries = new[]
                {
                    //Bogs
                    new MicroDataEntry(){ContentType = Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/BlogPosting"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".blog-details .content-author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".blog-details .content-date" , Type = MicroDataType.itemprop , Value = "datePublished"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".blog-post .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".blog-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                   
                    //Comments
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType =Apis.Get<IBlogPosts>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Apis.Get<IWikiPages>().ContentTypeId, Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Apis.Get<IWikiPages>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = Apis.Get<IWikiPages>().ContentTypeId, Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},

                    //Users
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author, .author" , Type = MicroDataType.itemscope , Value = "http://schema.org/Person"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.rel , Value = "author"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .avatar, .author .avatar img" , Type = MicroDataType.itemprop , Value = "image"},
                    
                    //FOrums
                    new MicroDataEntry(){ContentType = Apis.Get<IForumThreads>().ContentTypeId, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/Article"},
                    new MicroDataEntry(){ContentType = Apis.Get<IForumThreads>().ContentTypeId, Selector = ".thread .full" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = Apis.Get<IForumThreads>().ContentTypeId, Selector = ".thread .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Apis.Get<IForumThreads>().ContentTypeId, Selector = ".thread .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = Apis.Get<IForumThreads>().ContentTypeId, Selector = ".thread .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                    
                    
                    //Media
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/MediaObject"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".media-gallery-post .full .name a" , Type = MicroDataType.itemprop , Value = "contentUrl"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".media-gallery-post  .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".media-gallery-post .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = Apis.Get<IMedia>().ContentTypeId, Selector = ".media-gallery-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                };

    }
}
