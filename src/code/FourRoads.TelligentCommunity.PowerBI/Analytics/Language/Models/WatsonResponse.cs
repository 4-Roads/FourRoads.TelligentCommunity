namespace FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Models
{
    public class WatsonResponse
    {
        public Usage usage { get; set; }
        public string language { get; set; }
        public Keyword[] keywords { get; set; }
        public Emotion emotion { get; set; }

        public class Usage
        {
            public int text_units { get; set; }
            public int text_characters { get; set; }
            public int features { get; set; }
        }

        public class Emotion
        {
            public Document document { get; set; }
        }

        public class Document
        {
            public Emotions emotion { get; set; }
        }

        public class Emotions
        {
            public float sadness { get; set; }
            public float joy { get; set; }
            public float fear { get; set; }
            public float disgust { get; set; }
            public float anger { get; set; }
        }

        public class Keyword
        {
            public string text { get; set; }
            public Sentiment sentiment { get; set; }
            public float relevance { get; set; }
            public Emotions emotion { get; set; }
        }

        public class Sentiment
        {
            public float score { get; set; }
            public string label { get; set; }
        }
    }
}
