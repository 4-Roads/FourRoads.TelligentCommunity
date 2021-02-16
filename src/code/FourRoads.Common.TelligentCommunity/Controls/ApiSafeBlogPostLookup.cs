using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telligent.DynamicConfiguration.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.Common.TelligentCommunity.Controls
{
    // todo - reqwite as lookup property or use the ootb one
    public class ApiSafeBlogPostLookup : TextBox//, IPropertyControl
    {
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            Page.ClientScript.RegisterClientScriptBlock(GetType(), "initialize", @"

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
 ", true);

            Page.ClientScript.RegisterClientScriptBlock(GetType(), ClientID, string.Format("$(function () {{ $.fourroads.extensions.blogpostsLookup.register('#{0}' , {1} , {2} , {3}); }});", ClientID, EnableDuplicates.ToString().ToLower(), MaximumCount , GetCurrentSelectionsArray()), true);
        }

        private string GetCurrentSelectionsArray()
        {
            var blogPosts = GetSelectedBlogPosts(Text);
            List<string> items = new List<string>();

            foreach (var blog in blogPosts)
            {
                items.Add($"{{id:\'{blog.ContentId}\',title:\'{blog.Title}\'}}");
            }

            return $"[{string.Join(",", items)}]";
        }

        public int MaximumCount
        {
            get;
            set;
        }

        public bool EnableDuplicates
        {
            get;
            set;
        }


        public static IEnumerable<BlogPost> GetSelectedBlogPosts(string source)
        {
            string[] blogPostsIds = source.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return blogPostsIds.Select(u => Apis.Get<IBlogPosts>().Get(Guid.Parse(u)));
        }

        public void SetConfigurationPropertyValue(object value)
        {
            Text = Convert.ToString(value);
        }

        public object GetConfigurationPropertyValue()
        {
            return Text;
        }

        public Property ConfigurationProperty { get; set; }
        public ConfigurationDataBase ConfigurationData { get; set; }
        //public event ConfigurationPropertyChanged ConfigurationValueChanged;
        public Control Control => this;
    }
}