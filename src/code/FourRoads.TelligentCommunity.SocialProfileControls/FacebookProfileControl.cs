// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2014.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using Telligent.Common;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility.Version1;
using FourRoads.TelligentCommunity.SocialProfileControls.Controls;

namespace FourRoads.TelligentCommunity.SocialProfileControls
{
    public class FacebookProfileControl : TextProfileControl
    {
        public override string Name
        {
            get { return "4 Roads - Facebook"; }
        }

        public override string Description
        {
            get { return "This plugin extends Telligent Community to include additional profile field to support Facebook"; }
        }

        public override string FieldName
        {
            get { return "Facebook"; }
        }

        public override string Template
        {
            get { return FacebookProfileControlPropertyTemplate.GetTemplateName(); }
        }
    }
}