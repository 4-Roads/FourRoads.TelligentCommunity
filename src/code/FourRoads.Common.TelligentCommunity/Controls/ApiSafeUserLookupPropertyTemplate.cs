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
    public class ApiSafeUserLookupPropertyTemplate : IPropertyTemplate, IPropertyTemplateAdjustment
	{
		public string[] DataTypes => new string[] { "custom", "string" };

		public string TemplateName => "custom_apiSafeUserLookup";

		public bool SupportsReadOnly => true;

        private string InitialValue { get; set; }

        public PropertyTemplateOption[] Options
		{
			get
			{
				return new PropertyTemplateOption[2]
				{
					new PropertyTemplateOption("maxCount", "10")
					{
						Description = "Maximum number of user which can be selected."
					},
					new PropertyTemplateOption("includeHidden", "false")
					{
						Description = "Include system accounts from selection."
					}
				};
			}
		}

		public string Name => "4 Roads - API Safe User Lookup Property Template";

		public string Description => "Allows the selection of users via a glow lookup box";

		public void Initialize()
		{
		}

		public void Render(TextWriter writer, IPropertyTemplateOptions options)
		{
			var value = options.Value == null ? string.Empty : options.Value.ToString();
			var maxvalues = options.Property.Options["maxCount"] ?? "10";
			var includeHidden = options.Property.Options["includeHidden"] ?? "false";

			InitialValue = value;

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

	$.fourroads.extensions.userLookup = {

		register : function (textBoxId, allowDuplicates, maxValues, includeHidden) {
			var spinner = '<div style=""text-align: center;""><img src=""' + $.telligent.evolution.site.getBaseUrl() + 'utility/spinner.gif"" /></div>';

			$(textBoxId).glowLookUpTextBox({
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
								Query : searchText + '*',
								Filters : 'type::user',
								PageSize : 20
							},
                            async : false,
							success : function (response) {
								if (response && response.SearchResults.length >= 1) {
									var userNames = $.map(response.SearchResults, function (sr, i) {
											return sr.Users[0].Username;
										});

                                    if (userNames.length > 0){
									    $.telligent.evolution.get({
										    url : $.telligent.evolution.site.getBaseUrl() + 'api.ashx/v2/users.json',
										    data : {
											    Usernames : userNames.join(),
											    IncludeHidden : includeHidden
										    },
										    async : false,
										    success : function (response) {
											    results = $.map(response.Users, function (user, i) {
													    return tb.glowLookUpTextBox('createLookUp', user.Id, user.Username, user.DisplayName, true);
										        });
										    }
									    });
                                    }
								} 
							}
						});

                        if (results.length == 0){
                            	results.push(tb.glowLookUpTextBox('createLookUp', '', 'No Users Found', 'No Users Found', false));
                        }

						tb.glowLookUpTextBox('updateSuggestions', results);
					}
				}
			});
		}
	};
    </script>
 ");
				writer.Write(
					$"\r\n<script type=\"text/javascript\">\r\n$(document).ready(function() {{\r\n " +
					$"$.fourroads.extensions.userLookup.register('#{options.UniqueId}', false, {maxvalues}, {includeHidden}); \r\n" +
					$"var api = {(object)options.JsonApi};\r\n    var i = $('#{(object)options.UniqueId}');\r\n       api.register({{\r\n        val: function(val) {{ return (typeof val == 'undefined') ? i.val() : i.val(val); }},\r\n        hasValue: function() {{ return i.val() != null; }}\r\n    }});\r\n    i.on('change', function() {{ api.changed(i.val()); }});\r\n}});\r\n</script>\r\n");

			}
		}
		public void AdjustPropertyTemplate(ref string template, NameValueCollection options, PropertyValue[] values)
		{
			if (!(template == "FourRoads.Common.TelligentCommunity.Controls.ApiSafeUserLookupPropertyTemplate, FourRoads.Common.TelligentCommunity"))
				return;
			template = this.TemplateName;
			int result;

			if (options["maxCount"] == null || !int.TryParse(options["maxCount"], out result) || result < 1)
			{
				options["maxCount"] = "10";
			}

			if (options["includeHidden"] == null || !bool.TryParse(options["includeHidden"], out bool inclHidden))
			{
				options["includeHidden"] = "false";
			}
		}

        public IEnumerable<User> SelectedUsers
        {
            get
            {
                string[] userIds = InitialValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                return userIds.Select(u => Apis.Get<IUsers>().Get(new UsersGetOptions{Id=Convert.ToInt32(u)}));
            }
        }
    }
}
