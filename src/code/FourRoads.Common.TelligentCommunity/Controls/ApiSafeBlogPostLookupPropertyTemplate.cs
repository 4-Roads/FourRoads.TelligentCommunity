using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Configuration.Version1;

namespace FourRoads.Common.TelligentCommunity.Controls
{
    public class ApiSafeBlogPostLookupPropertyTemplate : IPropertyTemplate, IPropertyTemplateAdjustment
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "custom_apiSafeBlogPostLookup";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return new PropertyTemplateOption[1]
                {
                    new PropertyTemplateOption("maxCount", "10")
                    {
                        Description = "Maximum number of blog posts which can be selected."
                    }
                };
            }
        }

        public string Name => "4 Roads - API Safe Blog Post Lookup Property Template";

        public string Description => "Allows the selection of blog posts fields via a glow lookup box";

        public void Initialize()
        {
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            var value = options.Value == null ? string.Empty : options.Value.ToString();
            var maxvalues = options.Property.Options["maxCount"] ?? "10";

            if (options.Property.Editable)
            {
                writer.Write($"<input style='width:100%' id='{options.UniqueId}'>");

                writer.Write(
                    @"
    <script type='text/javascript'>

    if (typeof $.fourroads === 'undefined')
		$.fourroads = {};
	if (typeof $.fourroads.extensions === 'undefined')
		$.fourroads.extensions = {};

	$.fourroads.extensions.blogpostsLookup = {

		register : function (textBoxId, allowDuplicates, maxValues , initialValues) {
			var spinner = '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'utility/spinner.gif"" /></div>';
            var textBox = $(textBoxId);

			textBox.glowLookUpTextBox({
				allowDuplicates : allowDuplicates,
				maxValues : maxValues,
				selectedLookUpsHtml : [],
				emptyHtml : '',
				onGetLookUps : function (tb, searchText) {
					if (searchText && searchText.length >= 2) {

						tb.glowLookUpTextBox('updateSuggestions', [
								tb.glowLookUpTextBox('createLookUp', '', spinner, spinner, false)
					    ]);

					    var results = [];

						$.telligent.evolution.get({
							url : $.telligent.evolution.site.getBaseUrl() + 'api.ashx/v2/search.json',
							data : {
								Query : searchText,
								Filters : 'type::blog',
								PageSize : 20
							},
                            async : false,
							success : function (response) {
								if (response && response.SearchResults.length >= 1) {

									$.map(response.SearchResults, function (sr, i) {
                                        results.push(tb.glowLookUpTextBox('createLookUp', sr.Id, sr.Title, sr.Title, true));
									});
                                    
								} 
							}
						});

                        if (results.length == 0){
                            	results.push(tb.glowLookUpTextBox('createLookUp', '', 'No Blog Posts Found', 'No Blogs Found', false));
                        }

                        
						tb.glowLookUpTextBox('updateSuggestions', results);
					}
				}
			});

            console.log(initialValues);

            $.map( initialValues , function (item, i) {
                var lookup = textBox.glowLookUpTextBox('createLookUp', item.id, item.title, item.title, true);
                textBox.glowLookUpTextBox('add', lookup);
			});
		}
	};
    </script>
 ");
                writer.Write(
                    $"\r\n<script type=\"text/javascript\">\r\n$(document).ready(function() {{\r\n " +
                    $"$.fourroads.extensions.blogpostsLookup.register('#{options.UniqueId}', false, {maxvalues}, {GetCurrentSelectionsArray(value)}); \r\n" +
                    $"var api = {(object)options.JsonApi};\r\n    var i = $('#{(object)options.UniqueId}');\r\n       api.register({{\r\n        val: function(val) {{ return (typeof val == 'undefined') ? i.val() : i.val(val); }},\r\n        hasValue: function() {{ return i.val() != null; }}\r\n    }});\r\n    i.on('change', function() {{ api.changed(i.val()); }});\r\n}});\r\n</script>\r\n");

            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                writer.Write($"<p> {GetSelectedBlogPosts(value)} </p>");
            }
        }

        public void AdjustPropertyTemplate(ref string template, NameValueCollection options, PropertyValue[] values)
        {
            if (!(template == "FourRoads.Common.TelligentCommunity.Controls.ApiSafeBlogPostLookupPropertyTemplate, FourRoads.Common.TelligentCommunity"))
                return;
            template = this.TemplateName;
            int result;
            if (options["maxCount"] != null && int.TryParse(options["maxCount"], out result) && result >= 1)
                return;
            options["maxCount"] = "10";
        }

        private string GetCurrentSelectionsArray(string source)
        {
            var blogPosts = GetSelectedBlogPosts(source);
            List<string> items = new List<string>();

            foreach (var blog in blogPosts)
            {
                items.Add($"{{id:\'{blog.ContentId}\',title:\'{blog.Title}\'}}");
            }

            return $"[{string.Join(",", items)}]";
        }

        public static IEnumerable<BlogPost> GetSelectedBlogPosts(string source)
        {
            string[] blogPostsIds = source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return blogPostsIds.Select(u => Apis.Get<IBlogPosts>().Get(Guid.Parse(u)));
        }
    }
}