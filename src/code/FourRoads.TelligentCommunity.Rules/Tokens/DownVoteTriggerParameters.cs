namespace FourRoads.TelligentCommunity.Rules.Tokens
{
    /// <summary>
    /// Used to pass custom values to the rule config UI (conditions etc) and rule actions
    /// </summary>
    /// 
    public class DownVoteTriggerParameters
    {
        public int ReplyDownVotes { get; set; }
        public int ThreadDownVotes { get; set; }
    }
}
