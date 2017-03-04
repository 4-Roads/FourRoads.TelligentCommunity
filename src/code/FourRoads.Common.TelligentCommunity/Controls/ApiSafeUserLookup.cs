using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.Common.TelligentCommunity.Controls
{
    public class ApiSafeUserLookup : TextBox
    {
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            Page.ClientScript.RegisterClientScriptBlock(GetType(), "initialize", @"

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
 ", true);

            Page.ClientScript.RegisterClientScriptBlock(GetType(), ClientID, string.Format("$(function () {{ $.fourroads.extensions.userLookup.register('#{0}' , {1} , {2} , {3}); }});", ClientID, EnableDuplicates.ToString().ToLower(), MaximumCount, IncludeSystemAccounts.ToString().ToLower()), true);
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


        public bool IncludeSystemAccounts
        {
            get;
            set;
        }

        public IEnumerable<User> SelectedUsers
        {
            get
            {
                string[] userIds = Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                return userIds.Select(u => PublicApi.Users.Get(new UsersGetOptions{Id=Convert.ToInt32(u)}));
            }
        }
    }
}
