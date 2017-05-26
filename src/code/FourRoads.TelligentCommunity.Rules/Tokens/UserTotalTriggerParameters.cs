namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    /// <summary>
    /// Used to pass custom values to the rule config UI (conditions etc) and rule actions
    /// </summary>
    /// 
    public class UserTotalTriggerParameters
    {
        public int UpVoteCount { get; set; }
        public int DownVoteCount { get; set; }
        public int VoteCount { get; set; }
        public int TagCount { get; set; }
    }
}
