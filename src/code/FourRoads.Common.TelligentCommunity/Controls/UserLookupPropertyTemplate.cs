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
    public class UserLookupPropertyTemplate : IPropertyTemplate, IPropertyTemplateAdjustment
    {
        public string[] DataTypes => new string[] { "custom", "string" };

        public string TemplateName => "custom_usersLookup";

        public bool SupportsReadOnly => true;

        public PropertyTemplateOption[] Options
        {
            get
            {
                return new PropertyTemplateOption[2]
                {
                    new PropertyTemplateOption("maxCount", "10")
                    {
                        Description = "Maximum number of users which can be selected."
                    },
                    new PropertyTemplateOption("includeHidden", "false")
                    {
                        Description = "Include hidden or system users"
                    }
                };
            }
        }

        public string Name => "4 Roads - Users Lookup Property Template";

        public string Description => "Allows the selection of users via a glow lookup box";

        public void Initialize()
        {
        }

        public void Render(TextWriter writer, IPropertyTemplateOptions options)
        {
            string value = options.Value == null ? string.Empty : options.Value.ToString();
            string maxvalues = options.Property.Options["maxCount"] ?? "10";
            string includeHidden = options.Property.Options["includeHidden"] ?? "false";

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

	$.fourroads.extensions.usersLookup = {

		register : function (textBoxId, allowDuplicates, maxValues , initialValues, includeHidden) {
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

            $.map( initialValues , function (item, i) {
                var lookup = textBox.glowLookUpTextBox('createLookUp', item.id, item.name, item.name, true);
                textBox.glowLookUpTextBox('add', lookup);
			});
		}
	};
    </script>
 ");
                writer.Write(
                    $"\r\n<script type=\"text/javascript\">\r\n$(document).ready(function() {{\r\n " +
                    $"$.fourroads.extensions.usersLookup.register('#{options.UniqueId}' , false , {maxvalues} , {GetCurrentSelectionsArray(value)} , {includeHidden}); \r\n" +
                    $"var api = {(object)options.JsonApi};\r\n    var i = $('#{(object)options.UniqueId}');\r\n       api.register({{\r\n        val: function(val) {{ return (typeof val == 'undefined') ? i.val() : i.val(val); }},\r\n        hasValue: function() {{ return i.val() != null; }}\r\n    }});\r\n    i.on('change', function() {{ api.changed(i.val()); }});\r\n}});\r\n</script>\r\n");

            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                writer.Write($"<p> {GetCurrentSelectionsDisplay(value)} </p>");
            }
        }

        public void AdjustPropertyTemplate(ref string template, NameValueCollection options, PropertyValue[] values)
        {
            if (!(template == "FourRoads.Common.TelligentCommunity.Controls.UserLookupPropertyTemplate, FourRoads.Common.TelligentCommunity"))
                return;
            template = this.TemplateName;
            int maxCount;
            bool includeHidden;

            if (options["maxCount"] == null || !int.TryParse(options["maxCount"], out maxCount) || maxCount == 0)
            {
                options["maxCount"] = "10";
            }

            if (options["includeHidden"] == null || !bool.TryParse(options["includeHidden"], out includeHidden))
            {
                options["includeHidden"] = "false";
            }

        }

        private string GetCurrentSelectionsArray(string value)
        {
            var users = GetSelectedUsers(value);
            List<string> items = new List<string>();

            foreach (var user in users)
            {
                items.Add($"{{id:\'{user.Id}\',name:\'{user.DisplayName}\'}}");
            }

            return $"[{string.Join(",", items)}]";
        }

        private string GetCurrentSelectionsDisplay(string value)
        {
            return string.Join(", ", GetSelectedUsers(value).Select(s => s.DisplayName));
        }

        private IEnumerable<User> GetSelectedUsers(string value)
        {
            int[] userIds = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(u => int.Parse(u)).ToArray();

            return userIds.Select(g => Apis.Get<IUsers>().Get(new UsersGetOptions() {Id = g}));
        }
    }
}
