using System;


namespace FourRoads.TelligentCommunity.MicroData
{
    public class MicroDataDefaultData
    {
        public static MicroDataEntry[] Entries = new[]
                {
                    //Bogs
                    new MicroDataEntry(){ContentType = new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/BlogPosting"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".blog-details .content-author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".blog-details .content-date" , Type = MicroDataType.itemprop , Value = "datePublished"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".blog-post .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".blog-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                   
                    //Comments
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType =new Guid("ca0e7c80-8686-4d2f-a5a8-63b9e212e922"), Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = new Guid("6b577b8c-0470-4e20-9d29-b6772bf67243"), Selector = ".content-fragment.comments" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = new Guid("6b577b8c-0470-4e20-9d29-b6772bf67243"), Selector = ".content-fragment.comments .content.comment .content" , Type = MicroDataType.itemprop , Value = "text"},
                    new MicroDataEntry(){ContentType = new Guid("6b577b8c-0470-4e20-9d29-b6772bf67243"), Selector = ".content-fragment.comments .content.comment .author" , Type = MicroDataType.itemprop , Value = "creator"},

                    //Users
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author, .author" , Type = MicroDataType.itemscope , Value = "http://schema.org/Person"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .user-name a, .author .user-name a" , Type = MicroDataType.rel , Value = "author"},
                    new MicroDataEntry(){ContentType = null, Selector = ".content-author .avatar, .author .avatar img" , Type = MicroDataType.itemprop , Value = "image"},
                    
                    //FOrums
                    new MicroDataEntry(){ContentType = new Guid("46448885-d0e6-4133-bbfb-f0cd7b0fd6f7"), Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/Article"},
                    new MicroDataEntry(){ContentType = new Guid("46448885-d0e6-4133-bbfb-f0cd7b0fd6f7"), Selector = ".thread .full" , Type = MicroDataType.itemscope , Value = "http://schema.org/Comment"},
                    new MicroDataEntry(){ContentType = new Guid("46448885-d0e6-4133-bbfb-f0cd7b0fd6f7"), Selector = ".thread .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = new Guid("46448885-d0e6-4133-bbfb-f0cd7b0fd6f7"), Selector = ".thread .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = new Guid("46448885-d0e6-4133-bbfb-f0cd7b0fd6f7"), Selector = ".thread .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                    
                    
                    //Media
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".layout" , Type = MicroDataType.itemscope , Value = "http://schema.org/MediaObject"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".media-gallery-post .full .name a" , Type = MicroDataType.itemprop , Value = "contentUrl"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".media-gallery-post  .full .name" , Type = MicroDataType.itemprop , Value = "name"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".media-gallery-post .author" , Type = MicroDataType.itemprop , Value = "creator"},
                    new MicroDataEntry(){ContentType = new Guid("08ca0da0-e017-4a80-9832-476c74d4f174"), Selector = ".media-gallery-post .full .content" , Type = MicroDataType.itemprop , Value = "text"},
                };

    }
}
