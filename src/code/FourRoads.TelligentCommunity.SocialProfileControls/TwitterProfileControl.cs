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
    public class TwitterProfileControl : TextProfileControl
    {
        public override string Name
        {
            get { return "4 Roads - Twitter"; }
        }

        public override string Description
        {
            get { return "This plugin extends Telligent Community to include additional profile field to support twitter"; }
        }

        public override string FieldName
        {
            get { return "Twitter"; }
        }

        public override string Template
        {
            get { return TwitterProfileControlPropertyTemplate.GetTemplateName(); }
        }
    }
}